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
//   Validates account state and available balance, reserves funds,
//   writes OrderPlaced domain event, creates Order projection atomically.
//
// MEMBER DOCUMENTATION:
// - _uow: Injected INexusUnitOfWork -- single unit of work for atomic persistence.
// - Handle: Loads account, validates business rules, computes reserved amount,
//   writes OrderPlaced event, creates Order projection, updates reserved_balance.
//   Throws KeyNotFoundException if account does not exist.
//   Throws InvalidOperationException if account is not Active.
//   Throws InvalidOperationException if available balance is insufficient.
// ============================================================================

namespace NexusEngine.Api.Application.Orders.Commands.PlaceOrder;

using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NexusEngine.Api.Application.Abstractions;
using NexusEngine.Api.Domain.Entities;

public class PlaceOrderHandler : IRequestHandler<PlaceOrderCommand, Guid>
{
    private readonly INexusUnitOfWork _uow;

    public PlaceOrderHandler(INexusUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Guid> Handle(
        PlaceOrderCommand command,
        CancellationToken cancellationToken)
    {
        var account = await _uow.Accounts
            .FirstOrDefaultAsync(
                a => a.Id == command.AccountId,
                cancellationToken
            );

        if (account is null)
            throw new KeyNotFoundException(
                $"Account {command.AccountId} not found."
            );

        if (account.Status != "Active")
            throw new InvalidOperationException(
                $"Account {command.AccountId} is not Active"
            );

        var reservationAmount = command.Side == "Buy"
            ? command.Quantity * command.Price
            : 0m;

        if (command.Side == "Buy")
        {
            var availableBalance = account.Balance - account.ReservedBalance;
            if (availableBalance  < reservationAmount)
                throw new InvalidOperationException(
                    $"Insufficient available balance. " + 
                    $"Available: {availableBalance}, Reuqired: {reservationAmount}."
                );
        }

        var orderId = Guid.NewGuid();

        var nextAccountVersion = account.LastEventVersion + 1;

        var domainEvent = new DomainEvent
        {
            AggregateId = command.AccountId,
            AggregateType = "Account",
            EventType = "OrderPlaced", 
            AggregateVersion = nextAccountVersion,
            Payload = JsonSerializer.Serialize(
                new
                {
                    OrderId = orderId,
                    command.Symbol,
                    command.Side,
                    command.Quantity,
                    command.Price,
                    ReservationAmount = reservationAmount
                }
            )
        };

        var order = new Order
        {
            Id               = orderId,
            AccountId        = command.AccountId,
            Symbol           = command.Symbol,
            Side             = command.Side,
            Quantity         = command.Quantity,
            RemainingQuantity = command.Quantity,
            Price            = command.Price,
            Status           = "Pending",
            CreatedAt        = DateTime.UtcNow
        };

        account.ReservedBalance += reservationAmount;
        account.LastEventVersion = nextAccountVersion;
        account.UpdatedAt = DateTime.UtcNow;

        _uow.DomainEvents.Add(domainEvent);
        _uow.Orders.Add(order);
        await _uow.SaveChangesAsync(cancellationToken);

        return orderId;
    }
}