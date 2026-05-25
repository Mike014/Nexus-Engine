// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Application.Accounts.Commands.DepositFunds
//
// FILE DESCRIPTION:
// Implements the MediatR handler for DepositFundsCommand.
//
// CLASS DOCUMENTATION:
// - DepositFundsHandler: Executes the deposit workflow against the Account aggregate.
//   Reads current state from the projection, validates business rules,
//   writes a new domain event, and updates the projection atomically.
//
// MEMBER DOCUMENTATION:
// - _uow: Injected INexusUnitOfWork -- decoupled from Infrastructure (ADR-006 fix).
// - Handle: Loads the account projection, validates existence and status,
//   computes the next aggregate version, writes FundsDeposited event,
//   updates account balance and last_event_version, persists atomically.
//   Throws KeyNotFoundException if account does not exist.
//   Throws InvalidOperationException if account is not Active.
// ============================================================================

namespace NexusEngine.Api.Application.Accounts.Commands.DepositFunds;

using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NexusEngine.Api.Application.Abstractions;
using NexusEngine.Api.Domain.Entities;

public class DepositFundsHandler : IRequestHandler<DepositFundsCommand, Unit>
{
    private readonly INexusUnitOfWork _uow;

    public DepositFundsHandler(INexusUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Unit> Handle(
        DepositFundsCommand command,
        CancellationToken cancellationToken)
    {
        var account = await _uow.Accounts
            .FirstOrDefaultAsync(
                a => a.Id == command.AccountId,
                cancellationToken
            );

        if (account is null)
            throw new KeyNotFoundException(
                $"Account {command.AccountId} not found.");

        if (account.Status != "Active")
            throw new InvalidOperationException(
                $"Account {command.AccountId} is not Active.");

        var nextVersion = account.LastEventVersion + 1;

        var domainEvent = new DomainEvent
        {
            AggregateId      = command.AccountId,
            AggregateType    = "Account",
            EventType        = "FundsDeposited",
            AggregateVersion = nextVersion,
            Payload          = JsonSerializer.Serialize(new
            {
                command.Amount,
                BalanceBefore = account.Balance,
                BalanceAfter  = account.Balance + command.Amount
            })
        };

        account.Balance          += command.Amount;
        account.LastEventVersion  = nextVersion;
        account.UpdatedAt         = DateTime.UtcNow;

        _uow.DomainEvents.Add(domainEvent);
        await _uow.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}