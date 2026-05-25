// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Domain.Entities
//
// FILE DESCRIPTION:
// Contains the Order read-side projection entity.
//
// CLASS DOCUMENTATION:
// - Order: Read-side projection representing the current state of a trading order.
//   Like the Account entity, this is a materialized view updated asynchronously
//   by processing stream events.
//
// MEMBER DOCUMENTATION:
// - Id: Unique identifier of the order.
// - AccountId: Foreign key linking this order to its originating Account projection.
// - Side: "Buy" or "Sell". Stored as a string instead of an enum to simplify
//   JSON serialization and enable direct readability in the database without lookup tables.
// - Price: The limit price execution boundary for the order. Stored as decimal.
// - Quantity: The total asset quantity requested in the order.
// - FilledQuantity: The asset quantity already matched/executed. Starts at 0.
//   The remaining open quantity is calculated as (Quantity - FilledQuantity).
// - Status: Operational life-cycle state ("Pending", "PartiallyFilled", "Filled", "Cancelled").
// - LastEventVersion: The highest aggregate version processed for this specific order.
// - CreatedAt / UpdatedAt: Audit timestamps stored strictly in UTC.
// - Account / Transactions: EF Core navigation properties for read-side queries.
// ============================================================================

namespace NexusEngine.Api.Domain.Entities;

using System;
using System.Collections.Generic;

public class Order
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    public string Symbol { get; set; } = string.Empty;

    public string Side { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public decimal Quantity { get; set; }

    public decimal RemainingQuantity { get; set; }

    public decimal FilledQuantity { get; set; }

    public string Status { get; set; } = "Pending";

    public int LastEventVersion { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Account Account { get; set; } = null!;

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}