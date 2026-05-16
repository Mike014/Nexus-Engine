// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Infrastructure.Persistence.Configurations
//
// FILE DESCRIPTION:
// Contains the Entity Framework Core configuration for the DomainEvent entity.
//
// CLASS DOCUMENTATION:
// - DomainEventConfiguration: Implements the IEntityTypeConfiguration<T> pattern,
//   which is Microsoft's recommended approach for fluent mapping. It strictly
//   isolates infrastructure-specific schema data from the domain layer, keeping
//   the domain entity entirely uncoupled from persistence implementation details.
//
// MEMBER DOCUMENTATION:
// - Configure(builder): Fluent API mapping pipeline that overrides database schemas:
//   - Explicit snake_case table and column mapping to align with PostgreSQL standards.
//   - Enforces specific string maximum lengths to optimize storage allocation.
//   - Maps the 'Payload' property directly onto a native PostgreSQL binary JSON (JSONB)
//     column type to enable sub-document indexing and performant runtime parsing.
//   - Configures a composite unique constraint index ('uq_aggregate_version') over 
//     (AggregateId, AggregateVersion) to enforce atomic optimistic locking at the database engine level.
//   - Registers an index ('idx_domain_events_occurred_at') on timestamps to optimize stream replay sort operations.
// ============================================================================

namespace NexusEngine.Api.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusEngine.Api.Domain.Entities;

public class DomainEventConfiguration : IEntityTypeConfiguration<DomainEvent>
{
    public void Configure(EntityTypeBuilder<DomainEvent> builder)
    {
        builder.ToTable("domain_events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.AggregateId)
            .HasColumnName("aggregate_id")
            .IsRequired();

        builder.Property(e => e.AggregateType)
            .HasColumnName("aggregate_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.AggregateVersion)
            .HasColumnName("aggregate_version")
            .IsRequired();

        builder.Property(e => e.OccurredAt)
            .HasColumnName("occurred_at")
            .IsRequired();

        builder.HasIndex(e => new { e.AggregateId, e.AggregateVersion })
            .IsUnique()
            .HasDatabaseName("uq_aggregate_version");

        builder.HasIndex(e => e.OccurredAt)
            .HasDatabaseName("idx_domain_events_occurred_at");
    }
}