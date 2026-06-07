// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Application.Orders.Commands.PlaceOrder
//
// FILE DESCRIPTION:
// Implements the MediatR handler for PlaceOrderCommand.
//
// CLASS DOCUMENTATION:
// - PlaceOrderHandler: Executes the place order workflow.
//   Delegates validation to IOrderValidationStrategy implementations.
//   Applies pessimistic locking via SELECT FOR UPDATE on the account row.
//   Writes OrderPlaced domain event and creates Order projection atomically.
//
// MEMBER DOCUMENTATION:
// - _uow: Injected INexusUnitOfWork -- single unit of work for atomic persistence.
// - _validations: Injected collection of validation strategies -- Open/Closed Principle.
// - Handle: Runs all validation strategies in sequence, then executes the order.
//   Loads account via SELECT FOR UPDATE to pessimistically lock the row.
//   Writes Transaction ledger record with the reservation amount.
//   Throws KeyNotFoundException if account does not exist.
//   Throws InvalidOperationException if any validation rule is violated.
// ============================================================================

namespace NexusEngine.Api.Application.Orders.Commands.PlaceOrder;

using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NexusEngine.Api.Application.Abstractions;
using NexusEngine.Api.Application.Orders.Validation;
using NexusEngine.Api.Domain.Entities;

public class PlaceOrderHandler : IRequestHandler<PlaceOrderCommand, Guid>
{
    private readonly INexusUnitOfWork _uow;
    private readonly IEnumerable<IOrderValidationStrategy> _validations;

    public PlaceOrderHandler(
        INexusUnitOfWork uow,
        IEnumerable<IOrderValidationStrategy> validations)
    {
        _uow = uow;
        _validations = validations;
    }

    public async Task<Guid> Handle(
        PlaceOrderCommand command,
        CancellationToken cancellationToken)
    {
        var account = await _uow.Accounts
            .FromSqlInterpolated($"SELECT * FROM accounts WHERE id = {command.AccountId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);

        foreach (var validation in _validations)
            validation.Validate(account, command);

        var reservationAmount = command.Side == "Buy"
            ? command.Quantity * command.Price
            : 0m;

        var orderId = Guid.NewGuid();

        var nextAccountVersion = account!.LastEventVersion + 1;

        var domainEvent = new DomainEvent
        {
            AggregateId      = command.AccountId,
            AggregateType    = "Account",
            EventType        = "OrderPlaced",
            AggregateVersion = nextAccountVersion,
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

        var order = new Order
        {
            Id                = orderId,
            AccountId         = command.AccountId,
            Symbol            = command.Symbol,
            Side              = command.Side,
            Quantity          = command.Quantity,
            RemainingQuantity = command.Quantity,
            Price             = command.Price,
            Status            = "Pending",
            CreatedAt         = DateTime.UtcNow
        };

        var transaction = new Transaction
        {
            Id        = Guid.NewGuid(),
            AccountId = command.AccountId,
            OrderId   = orderId,
            Type      = "OrderPlaced",
            Amount    = -reservationAmount,
            EventId   = domainEvent.Id,
            OccurredAt = DateTime.UtcNow
        };

        account.ReservedBalance  += reservationAmount;
        account.LastEventVersion  = nextAccountVersion;
        account.UpdatedAt         = DateTime.UtcNow;

        _uow.DomainEvents.Add(domainEvent);
        _uow.Orders.Add(order);
        _uow.Transactions.Add(transaction);
        await _uow.SaveChangesAsync(cancellationToken);

        return orderId;
    }
}