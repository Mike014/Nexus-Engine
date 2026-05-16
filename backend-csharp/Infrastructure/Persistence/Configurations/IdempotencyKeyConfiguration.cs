// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Infrastructure.Persistence.Configurations
//
// FILE DESCRIPTION:
// Contains the Entity Framework Core configuration for the IdempotencyKey entity.
//
// CLASS DOCUMENTATION:
// - IdempotencyKeyConfiguration: Implements the IEntityTypeConfiguration<IdempotencyKey> pattern.
//   It configures the relational schema for the distributed idempotency gateway mechanism,
//   ensuring that duplicate API tracking states are decoupled from domain core representations.
//
// MEMBER DOCUMENTATION:
// - Configure(builder): Fluent API mapping pipeline that overrides database schemas:
//   - Maps explicitly to snake_case table and column names to align with PostgreSQL standards.
//   - Configures the string 'Key' property as the Primary Key ('idempotency_key'), capped at 100 chars.
//   - Maps 'ResponseBody' onto a native PostgreSQL binary JSON (JSONB) column type to cache 
//     previously computed API response objects seamlessly regardless of payload structure.
//   - Exposes 'ResponseStatus' to cache HTTP status values alongside payloads.
//   - Registers an index ('idx_idempotency_expires') on 'expires_at' to facilitate fast 
//     database TTL pruning jobs and record cleanup scripts.
// ============================================================================

namespace NexusEngine.Api.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusEngine.Api.Domain.Entities;

public class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKey>
{
    public void Configure(EntityTypeBuilder<IdempotencyKey> builder)
    {
        builder.ToTable("idempotency_keys");

        builder.HasKey(i => i.Key);

        builder.Property(i => i.Key)
            .HasColumnName("idempotency_key")
            .HasMaxLength(100);

        builder.Property(i => i.ResponseBody)
            .HasColumnName("response_body")
            .HasColumnType("jsonb");

        builder.Property(i => i.ResponseStatus)
            .HasColumnName("response_status");

        builder.Property(i => i.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(i => i.ExpiresAt)
            .HasDatabaseName("idx_idempotency_expires");
    }
}