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
// - _db: Injected NexusDbContext for read and write operations.
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
using NexusEngine.Api.Domain.Entities;
using NexusEngine.Api.Infrastructure.Persistence;

public class DepositFundsHandler : IRequestHandler<DepositFundsCommand, Unit>
{
    private readonly NexusDbContext _db;

    public DepositFundsHandler(NexusDbContext db)
    {
        _db = db;
    }

    public async Task<Unit> Handle(
        DepositFundsCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Carica la proiezione corrente dell'account.
        //    Tracking abilitato -- dobbiamo modificare questa entity.
        var account = await _db.Accounts
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

        // 2. Calcola la prossima versione dell'aggregato.
        //    LastEventVersion e' la versione dell'ultimo evento
        //    processato da questa proiezione.
        //    Il nuovo evento avra' version + 1.
        var nextVersion = account.LastEventVersion + 1;

        // 3. Costruisce il domain event.
        //    FundsDeposited e' un fatto immutabile --
        //    registra esattamente quanto e' stato depositato
        //    e qual era il saldo precedente.
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

        // 4. Aggiorna la proiezione.
        account.Balance          += command.Amount;
        account.LastEventVersion  = nextVersion;
        account.UpdatedAt         = DateTime.UtcNow;

        // 5. Persiste evento e proiezione nella stessa transazione.
        _db.DomainEvents.Add(domainEvent);
        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}