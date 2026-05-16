// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Infrastructure.Persistence.Configurations
//
// FILE DESCRIPTION:
// Contains the Entity Framework Core configuration for the Order entity.
//
// CLASS DOCUMENTATION:
// - OrderConfiguration: Implements the IEntityTypeConfiguration<Order> pattern.
//   It isolates the database schema definition from the domain model, ensuring 
//   the domain layer remains completely decoupled from persistence implementation details.
//
// MEMBER DOCUMENTATION:
// - Configure(builder): Fluent API mapping pipeline that overrides database schemas:
//   - Maps explicitly to snake_case table and column names to align with PostgreSQL standards.
//   - Restricts string column maximum lengths ('side' capped at 4 characters for "Buy"/"Sell").
//   - Configures high-precision data types via 'numeric(18,2)' for prices and 
//     'numeric(18,8)' for quantities to handle fractional matching and crypto scaling safely.
//   - Establishes a One-to-Many relationship mapping between Accounts and Orders, 
//     explicitly defining 'AccountId' as the Foreign Key relational boundary.
//   - Registers database indexing over 'account_id' and 'status' columns to ensure 
//     highly optimized search executions during active read-side operations.
// ============================================================================

namespace NexusEngine.Api.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusEngine.Api.Domain.Entities;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasColumnName("id");

        builder.Property(o => o.AccountId)
            .HasColumnName("account_id")
            .IsRequired();

        builder.Property(o => o.Side)
            .HasColumnName("side")
            .HasMaxLength(4)
            .IsRequired();

        builder.Property(o => o.Price)
            .HasColumnName("price")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(o => o.Quantity)
            .HasColumnName("quantity")
            .HasColumnType("numeric(18,8)")
            .IsRequired();

        builder.Property(o => o.FilledQuantity)
            .HasColumnName("filled_quantity")
            .HasColumnType("numeric(18,8)")
            .IsRequired();

        builder.Property(o => o.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.LastEventVersion)
            .HasColumnName("last_event_version")
            .IsRequired();

        builder.Property(o => o.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(o => o.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasOne(o => o.Account)
            .WithMany(a => a.Orders)
            .HasForeignKey(o => o.AccountId);

        builder.HasIndex(o => o.AccountId)
            .HasDatabaseName("idx_orders_account");

        builder.HasIndex(o => o.Status)
            .HasDatabaseName("idx_orders_status");
    }
}