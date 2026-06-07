// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Domain.OrderBook
//
// FILE DESCRIPTION:
// Contains the Trade record, representing a single match execution between
// a maker order and a taker order in the order book.
//
// CLASS DOCUMENTATION:
// - Trade: Immutable record capturing the result of a successful match.
//   Stores the maker order identifier, the agreed execution price (maker's
//   limit price), and the quantity exchanged in this match.
//
// MEMBER DOCUMENTATION:
// - MakerOrderId: The unique identifier of the resting (maker) order.
// - Price: The execution price of the trade, defined by the maker's limit.
// - Quantity: The asset quantity filled in this trade execution.
// ============================================================================

namespace NexusEngine.Domain.OrderBook;

public record Trade(Guid MakerOrderId, decimal Price, decimal Quantity);
