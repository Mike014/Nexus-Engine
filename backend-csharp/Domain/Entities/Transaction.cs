// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Domain.Entities
//
// FILE DESCRIPTION:
// Contains the Transaction read-side projection entity.
//
// CLASS DOCUMENTATION:
// - Transaction: Read-side projection acting as an audit ledger for financial 
//   and systemic asset movements. It materializes ledger rows derived from 
//   domain events to provide a historical, cross-referenced audit trail.
//
// MEMBER DOCUMENTATION:
// - Id: Unique identifier for the ledger transaction record.
// - AccountId: Foreign key linking this transaction to the affected Account projection.
// - Type: Operational category ("Deposit", "Withdrawal", "ReserveFunds", "ReleaseFunds", "SettleTrade").
// - Amount: Signed monetary value. Positive for credits, negative for debits.
//   Using a signed field simplifies historical aggregate checksums via SUM() queries.
// - OrderId: Nullable foreign key linking to the originating Order projection. 
//   Null for direct ledger mutations like account deposits or manual adjustments.
// - EventId: The exact DomainEvent ID that produced this record, enabling
//   direct traceability between the materialized read model and the Event Store.
// - OccurredAt: Audit timestamp marking when the transaction took place in UTC.
// - Account / Order / Event: EF Core navigation properties for ledger inspection.
// ============================================================================

namespace NexusEngine.Api.Domain.Entities;

using System;

public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AccountId { get; set; }

    public string Type { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public Guid? OrderId { get; set; }

    public Guid EventId { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public Account Account { get; set; } = null!;

    public Order? Order { get; set; }

    public DomainEvent Event { get; set; } = null!;
}