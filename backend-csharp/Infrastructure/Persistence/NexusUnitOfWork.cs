// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Infrastructure.Persistence
//
// FILE DESCRIPTION:
// Concrete implementation of INexusUnitOfWork backed by NexusDbContext.
// Bridges the Application abstraction with the EF Core infrastructure.
//
// ARCHITECTURAL DOCUMENTATION:
// - Adapter Pattern:
//   Wraps NexusDbContext and exposes it through the INexusUnitOfWork interface.
//   The Application layer never sees NexusDbContext directly.
// - Lifetime:
//   Registered as Scoped in DI -- one instance per HTTP request,
//   same lifetime as NexusDbContext.
// ============================================================================

namespace NexusEngine.Api.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using NexusEngine.Api.Application.Abstractions;
using NexusEngine.Api.Domain.Entities;

public class NexusUnitOfWork : INexusUnitOfWork
{
    private readonly NexusDbContext _db;

    public NexusUnitOfWork(NexusDbContext db)
    {
        _db = db;
    }

    public DbSet<Account> Accounts => _db.Accounts;
    public DbSet<Order> Orders => _db.Orders;
    public DbSet<Transaction> Transactions => _db.Transactions;
    public DbSet<DomainEvent> DomainEvents => _db.DomainEvents;
    public DbSet<IdempotencyKey> IdempotencyKeys => _db.IdempotencyKeys;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);
}