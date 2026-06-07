// FILE DESCRIPTION: Contains the Trade record, representing a single match
// execution between a maker order and a taker order in the order book.

namespace NexusEngine.Domain.OrderBook;

// CLASS DOCUMENTATION: Trade captures the result of a successful match.
// Tracks the maker order, both buy and sell order identifiers, the
// execution price, quantity, and the timestamp of execution.
//
// MEMBER DOCUMENTATION:
// - MakerOrderId: The unique identifier of the resting (maker) order.
// - Price: The execution price of the trade, defined by the maker's limit.
// - Quantity: The asset quantity filled in this trade execution.
// - BuyOrderId: The buy-side order identifier involved in this trade.
// - SellOrderId: The sell-side order identifier involved in this trade.
// - ExecutedAt: UTC timestamp when the trade was executed.
public record Trade(
    Guid MakerOrderId,
    decimal Price,
    decimal Quantity,
    Guid BuyOrderId,
    Guid SellOrderId,
    DateTime ExecutedAt);
