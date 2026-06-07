// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Application.Orders.Queries.GetOrders
//
// FILE DESCRIPTION:
// Data Transfer Object for the Order read-side projection.
// Returned by the GetOrdersQuery handler.
//
// MEMBER DOCUMENTATION:
// - Id: Unique identifier of the order.
// - Symbol: Trading pair symbol (e.g. "BTCUSD").
// - Side: "Buy" or "Sell".
// - Quantity: Total asset quantity requested.
// - RemainingQuantity: Asset quantity still open.
// - Price: Limit price for the order.
// - Status: Current lifecycle state ("Pending", "PartiallyFilled", etc.).
// - CreatedAt: UTC timestamp when the order was placed.
// ============================================================================

namespace NexusEngine.Api.Application.Orders.Queries.GetOrders;

public record OrderDto(
    Guid Id,
    string Symbol,
    string Side,
    decimal Quantity,
    decimal RemainingQuantity,
    decimal Price,
    string Status,
    DateTime CreatedAt
);
