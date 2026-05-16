// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Infrastructure.Persistence.Configurations
//
// FILE DESCRIPTION:
// Contains the Entity Framework Core configuration for the Transaction entity.
//
// CLASS DOCUMENTATION:
// - TransactionConfiguration: Implements the IEntityTypeConfiguration<Transaction> pattern.
//   It orchestrates relational mapping rules, separating business entity definitions 
//   from infrastructure constraints, keeping the domain core free of persistent data dependencies.
//
// MEMBER DOCUMENTATION:
// - Configure(builder): Fluent API mapping pipeline that overrides database schemas:
//   - Formats explicitly to snake_case table and column names to align with PostgreSQL standards.
//   - Configures fixed precision 'numeric(18,2)' for accounting and monetary values.
//   - Establishes a strict One-to-Many relationship between Accounts and Transactions.
//   - Sets up an optional (nullable) One-to-Many mapping relationship between Orders and 
//     Transactions via '.IsRequired(false)' to accommodate non-order adjustments like direct deposits.
//   - Maps a unidirectional relationship to the DomainEvent table via 'EventId' to anchor 
//     unalterable data lineage back to the Event Store.
//   - Registers database indexing over 'account_id' to fast-track historical ledger aggregation.
// ============================================================================

namespace NexusEngine.Api.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusEngine.Api.Domain.Entities;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id");

        builder.Property(t => t.AccountId)
            .HasColumnName("account_id")
            .IsRequired();

        builder.Property(t => t.Type)
            .HasColumnName("type")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(t => t.OrderId)
            .HasColumnName("order_id");

        builder.Property(t => t.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(t => t.OccurredAt)
            .HasColumnName("occurred_at")
            .IsRequired();

        builder.HasOne(t => t.Account)
            .WithMany(a => a.Transactions)
            .HasForeignKey(t => t.AccountId);

        builder.HasOne(t => t.Order)
            .WithMany(o => o.Transactions)
            .HasForeignKey(t => t.OrderId)
            .IsRequired(false);

        builder.HasOne(t => t.Event)
            .WithMany()
            .HasForeignKey(t => t.EventId);

        builder.HasIndex(t => t.AccountId)
            .HasDatabaseName("idx_transactions_account");
    }
}