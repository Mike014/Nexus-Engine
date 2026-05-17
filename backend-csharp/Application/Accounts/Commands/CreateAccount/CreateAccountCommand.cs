// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Application.Accounts.Commands.CreateAccount
//
// FILE DESCRIPTION:
// Defines the Request/Command Data Transfer Object for creating a new Account.
//
// ARCHITECTURAL DOCUMENTATION:
// - Command Pattern (CQRS):
//   Represents an immutable intention to mutate system state within the application layer.
//   By implementing 'IRequest<Guid>', it registers into the MediatR pipeline as a dispatchable
//   unit of work that asynchronously yields the unique tracking identifier (Guid) of the created entity.
// - Record Semantics & Value Invariants:
//   Declared as a C# positional 'record' rather than a standard class. This natively enforces
//   compile-time immutability via init-only properties and provides structural value-based 
//   equality. These traits are highly optimized for message routing, thread safety, and cross-boundary 
//   data transmission without the risk of accidental side-effect mutations.
// ============================================================================

namespace NexusEngine.Api.Application.Accounts.Commands.CreateAccount;

using MediatR;

public record CreateAccountCommand(
    string OwnerName,
    string Currency
) : IRequest<Guid>;