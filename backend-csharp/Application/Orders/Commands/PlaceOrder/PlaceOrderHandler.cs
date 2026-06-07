namespace NexusEngine.Api.Application.Orders.Commands.PlaceOrder;

using System.Text.Json;
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

    private static void ApplyTradeToAccount(Account account, string side, decimal amount)
    {
        if (side == "Buy")
        {
            account.ReservedBalance -= amount;
            account.Balance -= amount;
        }
        else
        {
            account.Balance += amount;
        }

        account.UpdatedAt = DateTime.UtcNow;
    }

    public async Task<Guid> Handle(
        PlaceOrderCommand command,
        CancellationToken cancellationToken)
    {
        var orderId = Guid.NewGuid();

        var order = new Order
        {
            Id                = orderId,
            AccountId         = command.AccountId,
            Symbol            = command.Symbol,
            Side              = command.Side,
            Quantity          = command.Quantity,
            RemainingQuantity = command.Quantity,
            FilledQuantity    = 0,
            Price             = command.Price,
            Status            = "Pending",
            CreatedAt         = DateTime.UtcNow
        };

        var matchResult = _orderBookService.Match(order);

        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await PersistAndPublish(command, order, orderId, matchResult, cancellationToken);
            }
            catch (DbUpdateException ex) when (OptimisticConcurrencyHelper.IsUniqueConstraintViolation(ex))
            {
                if (attempt < maxAttempts)
                {
                    var delay = Random.Shared.Next(50, 300);
                    await Task.Delay(delay, cancellationToken);
                }
                else
                {
                    throw new InvalidOperationException(
                        "Order could not be placed due to concurrent activity. Please try again.");
                }
            }
        }

        throw new InvalidOperationException(
            "Order could not be placed due to concurrent activity. Please try again.");
    }

    private async Task<Guid> PersistAndPublish(
        PlaceOrderCommand command,
        Order order,
        Guid orderId,
        MatchResult matchResult,
        CancellationToken cancellationToken)
    {
        var freshAccount = await _uow.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == command.AccountId, cancellationToken);

        foreach (var validation in _validations)
            validation.Validate(freshAccount, command);

        var trackedAccount = _uow.Accounts.Local
            .FirstOrDefault(a => a.Id == command.AccountId);

        Account account;
        if (trackedAccount != null)
        {
            trackedAccount.Balance = freshAccount!.Balance;
            trackedAccount.ReservedBalance = freshAccount.ReservedBalance;
            trackedAccount.UpdatedAt = freshAccount.UpdatedAt;
            account = trackedAccount;
        }
        else
        {
            _uow.Accounts.Attach(freshAccount!);
            account = freshAccount!;
        }

        var reservationAmount = command.Side == "Buy"
            ? command.Quantity * command.Price
            : 0m;

        account.ReservedBalance += reservationAmount;

        var domainEvents = new List<DomainEvent>();
        var transactions = new List<Transaction>();
        var version = account.LastEventVersion;

        foreach (var trade in matchResult.Trades)
        {
            var makerOrder = await _uow.Orders.FindAsync(trade.MakerOrderId, cancellationToken)
                ?? throw new KeyNotFoundException($"Maker order {trade.MakerOrderId} not found.");

            var makerAccount = await _uow.Accounts.FindAsync(makerOrder.AccountId, cancellationToken)
                ?? throw new KeyNotFoundException($"Maker account {makerOrder.AccountId} not found.");

            makerOrder.RemainingQuantity -= trade.Quantity;
            makerOrder.FilledQuantity += trade.Quantity;
            makerOrder.Status = makerOrder.RemainingQuantity == 0 ? "Filled" : "PartiallyFilled";
            makerOrder.UpdatedAt = DateTime.UtcNow;

            ApplyTradeToAccount(makerAccount, makerOrder.Side, trade.Price * trade.Quantity);
            ApplyTradeToAccount(account, order.Side, trade.Price * trade.Quantity);

            version++;
            var matchedEvent = new DomainEvent
            {
                AggregateId      = command.AccountId,
                AggregateType    = "Account",
                EventType        = "OrderMatched",
                AggregateVersion = version,
                Payload          = JsonSerializer.Serialize(new
                {
                    OrderId       = orderId,
                    MakerOrderId  = trade.MakerOrderId,
                    trade.Price,
                    trade.Quantity
                })
            };
            domainEvents.Add(matchedEvent);

            transactions.Add(new Transaction
            {
                Id         = Guid.NewGuid(),
                AccountId  = command.AccountId,
                OrderId    = orderId,
                Type       = "OrderMatched",
                Amount     = trade.Price * trade.Quantity,
                EventId    = matchedEvent.Id,
                OccurredAt = DateTime.UtcNow
            });
        }

        var totalTraded = matchResult.Trades.Sum(t => t.Quantity);
        order.FilledQuantity = totalTraded;
        order.RemainingQuantity = command.Quantity - totalTraded;

        if (order.RemainingQuantity == 0)
            order.Status = "Filled";
        else if (order.FilledQuantity > 0)
            order.Status = "PartiallyFilled";

        if (order.RemainingQuantity > 0)
            _orderBookService.AddOrder(order);

        version++;
        var placedEvent = new DomainEvent
        {
            AggregateId      = command.AccountId,
            AggregateType    = "Account",
            EventType        = "OrderPlaced",
            AggregateVersion = version,
            Payload          = JsonSerializer.Serialize(new
            {
                OrderId           = orderId,
                command.Symbol,
                command.Side,
                command.Quantity,
                command.Price,
                ReservationAmount = reservationAmount
            })
        };
        domainEvents.Add(placedEvent);

        transactions.Add(new Transaction
        {
            Id         = Guid.NewGuid(),
            AccountId  = command.AccountId,
            OrderId    = orderId,
            Type       = "OrderPlaced",
            Amount     = -reservationAmount,
            EventId    = placedEvent.Id,
            OccurredAt = DateTime.UtcNow
        });

        account.LastEventVersion = version;
        account.UpdatedAt = DateTime.UtcNow;

        _uow.Orders.Add(order);

        foreach (var evt in domainEvents)
            _uow.DomainEvents.Add(evt);

        foreach (var txn in transactions)
            _uow.Transactions.Add(txn);

        await _uow.SaveChangesAsync(cancellationToken);

        if (matchResult.Trades.Count > 0)
        {
            await _mediator.Publish(
                new TradeExecutedNotification(matchResult.Trades), cancellationToken);
        }

        await _mediator.Publish(
            new OrderBookChangedNotification(command.Symbol), cancellationToken);

        await _mediator.Publish(
            new BalanceChangedNotification(
                command.AccountId, account.Balance, account.ReservedBalance),
            cancellationToken);

        return orderId;
    }
}
