// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Application.Abstractions
//
// FILE DESCRIPTION:
// Defines the Unit of Work contract for the Application layer.
// Abstracts the persistence infrastructure from business logic handlers.
//
// ARCHITECTURAL DOCUMENTATION:
// - Dependency Rule:
//   The Application layer depends on this interface, never on NexusDbContext directly.
//   The concrete implementation lives in Infrastructure.
// - Unit of Work Pattern:
//   Groups multiple repository operations into a single atomic transaction.
//   SaveChangesAsync commits all pending changes or rolls back on failure.
// ============================================================================

namespace NexusEngine.Api.Application.Abstractions;

using Microsoft.EntityFrameworkCore;
using NexusEngine.Api.Domain.Entities;

public interface INexusUnitOfWork
{
    DbSet<Account> Accounts { get; }
    DbSet<Order> Orders { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<DomainEvent> DomainEvents { get; }
    DbSet<IdempotencyKey> IdempotencyKeys { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}