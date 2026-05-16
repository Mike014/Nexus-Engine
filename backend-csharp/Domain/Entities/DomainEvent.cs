// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Domain.Entities
//
// FILE DESCRIPTION:
// Contains the DomainEvent domain entity.
//
// CLASS DOCUMENTATION:
// - DomainEvent: Represents an immutable domain event within the Event Store.
//   This entity is append-only: no UPDATE or DELETE operations are permitted.
//   It serves as the absolute source of truth for the entire system.
//
// MEMBER DOCUMENTATION:
// - Id: Unique identifier for the event, generated in-memory by the backend.
// - AggregateId: The ID of the aggregate boundary to which this event belongs.
// - AggregateType: The type of the aggregate (e.g., "Account", "Order") for fast filtering.
// - EventType: The specific event type (e.g., "AccountCreated") used for deserialization routing.
// - Payload: The event data serialized as JSON, mapped to a JSONB column in EF Core.
// - AggregateVersion: The aggregate version at the time of this event, enforcing optimistic locking.
// - OccurredAt: The creation timestamp of the event, strictly stored in UTC.
// ============================================================================

namespace NexusEngine.Api.Domain.Entities;

using System;

public class DomainEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid AggregateId { get; init; }

    public string AggregateType { get; init; } = string.Empty;

    public string EventType { get; init; } = string.Empty;

    public string Payload { get; init; } = string.Empty;

    public int AggregateVersion { get; init; }

    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
