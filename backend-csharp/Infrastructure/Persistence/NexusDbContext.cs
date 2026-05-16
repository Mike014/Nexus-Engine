// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Infrastructure.Persistence
//
// FILE DESCRIPTION:
// Contains the central Entity Framework Core database context.
//
// CLASS DOCUMENTATION:
// - NexusDbContext: Acts as the data gateway and bridge between Domain Entities 
//   and the physical relational database. It orchestrates tracking, reading, 
//   and writing operations, managing an active database session state.
//   Analogy: If Entities are blueprints, DbContext is the automated warehouse 
//   manager—it tracks object lifecycles, runs queries, and flushes unit-of-work changes.
//
// MEMBER DOCUMENTATION:
// - NexusDbContext(options): DI constructor passing operational configurations 
//   (connection strings, DB provider behaviors, logging) down to the framework core.
// - DbSets (DomainEvents, Accounts, Orders, Transactions, IdempotencyKeys):
//   Exposed transactional windows mapping directly to underlying database tables. 
//   They accept LINQ expressions to compile heavily optimized SQL abstractly.
// - OnModelCreating(modelBuilder): Configuration lifecycle pipeline hook used to override 
//   implicit EF Core naming conventions. Delegates relational schema building (indexes, 
//   composite unique constraints, foreign keys) to isolated configuration objects via reflection.
// ============================================================================

namespace NexusEngine.Api.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using NexusEngine.Api.Domain.Entities;

public class NexusDbContext : DbContext
{
    public NexusDbContext(DbContextOptions<NexusDbContext> options)
        : base(options)
    {
    }

    public DbSet<DomainEvent> DomainEvents => Set<DomainEvent>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(NexusDbContext).Assembly
        );
    }
}