// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Infrastructure.Persistence.Configurations
//
// FILE DESCRIPTION:
// Contains the Entity Framework Core configuration for the Account entity.
//
// CLASS DOCUMENTATION:
// - AccountConfiguration: Implements the IEntityTypeConfiguration<Account> pattern.
//   It isolates the schema definitions of the financial account projection from 
//   the core domain representation, enforcing persistence boundaries via the Fluent API.
//
// MEMBER DOCUMENTATION:
// - Configure(builder): Fluent API mapping pipeline that overrides database schemas:
//   - Maps explicitly to snake_case table and column names to align with PostgreSQL standards.
//   - Limits string field parameters to strict structural lengths ('owner_name' at 100, 
//     'currency' at 3 characters for ISO-4217 standard compliance).
//   - Enforces 'numeric(18,2)' data representations on monetary fields ('balance' and 
//     'reserved_balance') to eliminate floating-point precision truncation bugs in financial calculations.
//   - Tracks state concurrency invariants through the mandatory 'last_event_version' field.
// ============================================================================

namespace NexusEngine.Api.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusEngine.Api.Domain.Entities;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id");

        builder.Property(a => a.OwnerName)
            .HasColumnName("owner_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.Balance)
            .HasColumnName("balance")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(a => a.ReservedBalance)
            .HasColumnName("reserved_balance")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(a => a.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(a => a.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.LastEventVersion)
            .HasColumnName("last_event_version")
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_accounts_balance_non_negative",
                "balance >= 0");
            t.HasCheckConstraint(
                "ck_accounts_reserved_balance_non_negative",
                "reserved_balance >= 0");
        });
    }
}