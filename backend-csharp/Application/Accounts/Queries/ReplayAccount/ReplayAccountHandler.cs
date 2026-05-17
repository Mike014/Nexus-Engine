// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Application.Accounts.Queries.ReplayAccount
//
// FILE DESCRIPTION:
// Implements the MediatR handler for ReplayAccountQuery.
//
// CLASS DOCUMENTATION:
// - ReplayAccountHandler: Reconstructs Account state exclusively from domain_events.
//   Never reads from the accounts projection table.
//   Applies each event in aggregate_version order, mutating an in-memory state object.
//   This is the foundational Event Sourcing replay mechanism.
//
// MEMBER DOCUMENTATION:
// - _db: Injected NexusDbContext. Used only to read from domain_events table.
// - Handle: Loads all events for the given aggregate_id ordered by aggregate_version.
//   Returns null if no events exist for the given ID.
//   Applies each event by switching on EventType and mutating AccountState.
//   Supported events: AccountCreated, FundsDeposited.
// - AccountState: Private mutable in-memory state accumulator used during replay.
//   Not persisted -- exists only for the duration of the replay operation.
// ============================================================================

namespace NexusEngine.Api.Application.Accounts.Queries.ReplayAccount;

using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NexusEngine.Api.Infrastructure.Persistence;

public class ReplayAccountHandler : IRequestHandler<ReplayAccountQuery, ReplayAccountDto?>
{
    private readonly NexusDbContext _db;

    public ReplayAccountHandler(NexusDbContext db)
    {
        _db = db;
    }

    public async Task<ReplayAccountDto?> Handle(
        ReplayAccountQuery query,
        CancellationToken cancellationToken)
    {
        var events = await _db.DomainEvents
            .AsNoTracking()
            .Where(e => e.AggregateId == query.AccountId)
            .OrderBy(e => e.AggregateVersion)
            .ToListAsync(cancellationToken);

        if (events.Count == 0)
            return null;

        var state = new AccountState();

        foreach (var evt in events)
        {
            switch (evt.EventType)
            {
                case "AccountCreated":
                    var created = JsonSerializer.Deserialize<AccountCreatedPayload>(
                        evt.Payload,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    )!;
                    state.Id        = evt.AggregateId;
                    state.OwnerName = created.OwnerName;
                    state.Currency  = created.Currency;
                    state.Balance   = created.InitialBalance;
                    state.Status    = "Active";
                    break;

                case "FundsDeposited":
                    var deposited = JsonSerializer.Deserialize<FundsDepositedPayload>(
                        evt.Payload,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    )!;
                    state.Balance = deposited.BalanceAfter;
                    break;
            }

            state.LastEventVersion = evt.AggregateVersion;
        }

        return new ReplayAccountDto(
            state.Id,
            state.OwnerName,
            state.Balance,
            state.Currency,
            state.Status,
            state.LastEventVersion,
            EventsReplayed: events.Count
        );
    }

    private class AccountState
    {
        public Guid    Id               { get; set; }
        public string  OwnerName        { get; set; } = string.Empty;
        public decimal Balance           { get; set; }
        public string  Currency         { get; set; } = string.Empty;
        public string  Status           { get; set; } = string.Empty;
        public int     LastEventVersion { get; set; }
    }

    private record AccountCreatedPayload(
        string  OwnerName,
        string  Currency,
        decimal InitialBalance
    );

    private record FundsDepositedPayload(
        decimal Amount,
        decimal BalanceBefore,
        decimal BalanceAfter
    );
}