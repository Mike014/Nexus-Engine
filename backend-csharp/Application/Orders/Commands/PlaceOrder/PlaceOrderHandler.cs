// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Application.Orders.Commands.PlaceOrder
//
// FILE DESCRIPTION:
// Implements the MediatR handler for PlaceOrderCommand.
// Integrates with IOrderBookService to match incoming orders against
// resting liquidity using price-time priority FIFO matching.
//
// CLASS DOCUMENTATION:
// - PlaceOrderHandler: Executes the place order workflow with matching.
//   Delegates validation to IOrderValidationStrategy implementations.
//   Applies optimistic concurrency control via the unique constraint on
//   (aggregate_id, aggregate_version) in the domain_events table. Retries
//   up to 3 times with random jitter on unique constraint violations.
//   Routes the order through IOrderBookService.Match() for price-time
//   priority matching. Processes resulting trades by updating maker
//   orders and accounts atomically. Places unmatched quantity back
//   into the order book via AddOrder(). Writes OrderMatched and
//   OrderPlaced domain events with corresponding transaction records.
//
// MEMBER DOCUMENTATION:
// - _uow: Injected INexusUnitOfWork -- single unit of work for atomic persistence.
// - _validations: Injected collection of validation strategies -- Open/Closed Principle.
// - _orderBookService: Injected IOrderBookService singleton -- in-memory matching engine.
// - Handle: Validates, matches via OrderBook, processes trades, persists atomically.
// - ApplyTradeToAccount: Adjusts account balance and reserved balance based on trade side.
//   Throws KeyNotFoundException if account or maker order does not exist.
//   Throws InvalidOperationException if any validation rule is violated
//   or retries are exhausted due to concurrent activity.
// ============================================================================

namespace NexusEngine.Api.Application.Orders.Commands.PlaceOrder;

using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NexusEngine.Api.Application.Abstractions;
using NexusEngine.Api.Application.Common;
using NexusEngine.Api.Application.Orders.Validation;
using NexusEngine.Api.Domain.Entities;
using NexusEngine.Application.Abstractions;

public class PlaceOrderHandler : IRequestHandler<PlaceOrderCommand, Guid>
{
    private readonly INexusUnitOfWork _uow;
    private readonly IEnumerable<IOrderValidationStrategy> _validations;
    private readonly IOrderBookService _orderBookService;

    public PlaceOrderHandler(
        INexusUnitOfWork uow,
        IEnumerable<IOrderValidationStrategy> validations,
        IOrderBookService orderBookService)
    {
        _uow = uow;
        _validations = validations;
        _orderBookService = orderBookService;
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
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await ExecuteOnce(command, cancellationToken);
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

    private async Task<Guid> ExecuteOnce(
        PlaceOrderCommand command,
        CancellationToken cancellationToken)
    {
        var account = await _uow.Accounts
            .FirstOrDefaultAsync(a => a.Id == command.AccountId, cancellationToken);

        foreach (var validation in _validations)
            validation.Validate(account, command);

        var reservationAmount = command.Side == "Buy"
            ? command.Quantity * command.Price
            : 0m;

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

        account!.ReservedBalance += reservationAmount;

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

        return orderId;
    }
}
