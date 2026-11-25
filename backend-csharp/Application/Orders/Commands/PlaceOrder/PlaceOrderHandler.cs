// Places a limit order: validates, matches against the in-memory book, settles
// both sides of every trade and persists events + projections atomically.
//
// Consistency rules:
// - The whole command runs under the matching-engine lock (serialized writers).
// - The book is checkpointed before matching and restored if persistence fails,
//   so memory never diverges from the database.
// - Buyers reserve quantity * limit price; on each fill they pay quantity * trade
//   price and release quantity * their own limit price (price improvement is freed).
// - Every affected account (taker and makers) receives its own event and version.

namespace NexusEngine.Api.Application.Orders.Commands.PlaceOrder;

using MediatR;
using Microsoft.EntityFrameworkCore;
using NexusEngine.Api.Application.Abstractions;
using NexusEngine.Api.Application.Common;
using NexusEngine.Api.Application.Orders.Notifications;
using NexusEngine.Api.Application.Orders.Validation;
using NexusEngine.Api.Domain.Entities;
using NexusEngine.Application.Abstractions;
using NexusEngine.Domain.OrderBook;

public class PlaceOrderHandler : IRequestHandler<PlaceOrderCommand, Guid>
{
    private const int MaxAttempts = 3;

    private readonly INexusUnitOfWork _uow;
    private readonly IEnumerable<IOrderValidationStrategy> _validations;
    private readonly IOrderBookService _orderBookService;
    private readonly IMediator _mediator;

    public PlaceOrderHandler(
        INexusUnitOfWork uow,
        IEnumerable<IOrderValidationStrategy> validations,
        IOrderBookService orderBookService,
        IMediator mediator)
    {
        _uow = uow;
        _validations = validations;
        _orderBookService = orderBookService;
        _mediator = mediator;
    }

    public async Task<Guid> Handle(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        Outcome outcome;

        using (await _orderBookService.AcquireAsync(cancellationToken))
        {
            outcome = await ExecuteWithRetry(command, cancellationToken);
        }

        if (outcome.Trades.Count > 0)
            await _mediator.Publish(new TradeExecutedNotification(outcome.Trades), cancellationToken);

        await _mediator.Publish(new OrderBookChangedNotification(command.Symbol), cancellationToken);

        foreach (var account in outcome.TouchedAccounts)
        {
            await _mediator.Publish(
                new BalanceChangedNotification(account.Id, account.Balance, account.ReservedBalance),
                cancellationToken);
        }

        return outcome.OrderId;
    }

    private async Task<Outcome> ExecuteWithRetry(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var checkpoint = _orderBookService.Checkpoint(command.Symbol);
            try
            {
                return await ExecuteOnce(command, cancellationToken);
            }
            catch (Exception ex)
            {
                _orderBookService.Restore(checkpoint);
                _uow.DiscardChanges();

                var retryable = ex is DbUpdateException dbEx
                    && OptimisticConcurrencyHelper.IsUniqueConstraintViolation(dbEx);

                if (!retryable)
                    throw;

                if (attempt >= MaxAttempts)
                    throw new InvalidOperationException(
                        "Order could not be placed due to concurrent activity. Please try again.");

                await Task.Delay(Random.Shared.Next(50, 300), cancellationToken);
            }
        }
    }

    private async Task<Outcome> ExecuteOnce(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        var account = await _uow.Accounts
            .FirstOrDefaultAsync(a => a.Id == command.AccountId, cancellationToken);

        foreach (var validation in _validations)
            validation.Validate(account, command);

        var taker = account!;
        var isBuy = command.Side == "Buy";
        var reservation = isBuy ? command.Quantity * command.Price : 0m;
        var now = DateTime.UtcNow;

        var order = new Order
        {
            Id = Guid.NewGuid(),
            AccountId = command.AccountId,
            Symbol = command.Symbol,
            Side = command.Side,
            Quantity = command.Quantity,
            RemainingQuantity = command.Quantity,
            FilledQuantity = 0,
            Price = command.Price,
            Status = "Pending",
            CreatedAt = now,
            UpdatedAt = now
        };

        AccountLedger.Append(_uow, taker, "OrderPlaced", order.Id,
            balanceDelta: 0m, reservedDelta: reservation,
            new
            {
                OrderId = order.Id,
                command.Symbol,
                command.Side,
                command.Quantity,
                command.Price,
                ReservationAmount = reservation
            });

        var matchResult = _orderBookService.Match(order);
        var touched = new Dictionary<Guid, Account> { [taker.Id] = taker };

        foreach (var trade in matchResult.Trades)
        {
            var makerOrder = await _uow.Orders.FindAsync([trade.MakerOrderId], cancellationToken)
                ?? throw new KeyNotFoundException($"Maker order {trade.MakerOrderId} not found.");

            var makerAccount = await _uow.Accounts.FindAsync([makerOrder.AccountId], cancellationToken)
                ?? throw new KeyNotFoundException($"Maker account {makerOrder.AccountId} not found.");

            makerOrder.RemainingQuantity -= trade.Quantity;
            makerOrder.FilledQuantity += trade.Quantity;
            makerOrder.Status = makerOrder.RemainingQuantity == 0 ? "Filled" : "PartiallyFilled";
            makerOrder.UpdatedAt = DateTime.UtcNow;

            Settle(makerAccount, makerOrder, order.Id, trade, "Maker");
            Settle(taker, order, makerOrder.Id, trade, "Taker");

            touched[makerAccount.Id] = makerAccount;
        }

        var totalTraded = matchResult.Trades.Sum(t => t.Quantity);
        order.FilledQuantity = totalTraded;
        order.RemainingQuantity = command.Quantity - totalTraded;
        order.Status = order.RemainingQuantity == 0
            ? "Filled"
            : totalTraded > 0 ? "PartiallyFilled" : "Pending";

        _uow.Orders.Add(order);
        await _uow.SaveChangesAsync(cancellationToken);

        return new Outcome(order.Id, matchResult.Trades, touched.Values.ToList());
    }

    private void Settle(Account account, Order order, Guid counterOrderId, Trade trade, string role)
    {
        var notional = trade.Quantity * trade.Price;
        var isBuy = order.Side == "Buy";

        AccountLedger.Append(_uow, account, "OrderMatched", order.Id,
            balanceDelta: isBuy ? -notional : notional,
            reservedDelta: isBuy ? -(trade.Quantity * order.Price) : 0m,
            new
            {
                OrderId = order.Id,
                CounterOrderId = counterOrderId,
                Role = role,
                order.Side,
                trade.Price,
                trade.Quantity
            });
    }

    private sealed record Outcome(Guid OrderId, IReadOnlyList<Trade> Trades, IReadOnlyList<Account> TouchedAccounts);
}
