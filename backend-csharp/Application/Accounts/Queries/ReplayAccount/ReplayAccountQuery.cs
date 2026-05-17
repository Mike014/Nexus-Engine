// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Application.Accounts.Queries.ReplayAccount
//
// FILE DESCRIPTION:
// Defines the CQRS Query for reconstructing Account state by replaying domain events.
//
// CLASS DOCUMENTATION:
// - ReplayAccountQuery: Read-only query that bypasses the projection entirely.
//   Reconstructs the current state of an Account aggregate by applying all
//   domain events in sequence from the Event Store.
//   Demonstrates the core Event Sourcing principle: state is always derivable
//   from the event stream.
// - ReplayAccountDto: Result of the replay operation. Identical structure to
//   AccountDto but includes EventsReplayed to show how many events were applied.
//
// MEMBER DOCUMENTATION:
// ReplayAccountQuery:
// - AccountId: The unique identifier of the account aggregate to reconstruct.
//
// ReplayAccountDto:
// - Id: Reconstructed account identifier.
// - OwnerName: Reconstructed owner name from AccountCreated event.
// - Balance: Reconstructed balance after applying all FundsDeposited events.
// - Currency: Reconstructed currency from AccountCreated event.
// - Status: Reconstructed status from the event stream.
// - LastEventVersion: The version of the last event applied during replay.
// - EventsReplayed: Total number of events applied to reconstruct this state.
// ============================================================================

namespace NexusEngine.Api.Application.Accounts.Queries.ReplayAccount;

using MediatR;

public record ReplayAccountQuery(Guid AccountId) : IRequest<ReplayAccountDto?>;

public record ReplayAccountDto(
    Guid Id,
    string OwnerName,
    decimal Balance,
    string Currency,
    string Status,
    int LastEventVersion,
    int EventsReplayed
);

