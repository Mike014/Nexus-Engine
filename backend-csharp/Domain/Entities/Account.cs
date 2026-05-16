// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Domain.Entities
//
// FILE DESCRIPTION:
// Contains the Account read-side projection entity.
//
// CLASS DOCUMENTATION:
// - Account: Read-side projection of the current state of an account.
//   This is NOT the domain aggregate -- it is a materialized view built 
//   by applying domain events sequentially (CQRS Pattern).
//
// MEMBER DOCUMENTATION:
// - Id: Unique identifier of the account, matching the AggregateId of the events.
// - OwnerName: Name of the account holder.
// - Balance: Available balance for new operations. Mapped as NUMERIC(18,2) 
//   in the database. Uses C# 'decimal' to avoid floating-point rounding errors.
// - ReservedBalance: Funds temporarily locked by open/pending orders.
//   Total user balance is calculated as (Balance + ReservedBalance).
// - Currency: ISO currency code (defaults to "EUR").
// - Status: Operational status of the account (e.g., "Active", "Suspended").
// - LastEventVersion: The highest aggregate version processed by this projection.
//   Crucial for replayability and ensuring event processing order.
// - CreatedAt / UpdatedAt: Audit timestamps stored strictly in UTC.
// - Orders / Transactions: EF Core navigation properties for read-side queries.
// ============================================================================

namespace NexusEngine.Api.Domain.Entities;

public class Account
{
    public Guid Id { get; set; }

    public string OwnerName { get; set; } = string.Empty;

    public decimal Balance { get; set; }

    public decimal ReservedBalance { get; set; }

    public string Currency { get; set; } = "EUR";

    public string Status { get; set; } = "Active";

    public int LastEventVersion { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Order> Orders { get; set; } = new List<Order>();

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}