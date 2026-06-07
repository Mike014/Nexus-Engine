// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Infrastructure.OrderBook
//
// FILE DESCRIPTION:
// Concrete implementation of IOrderBookService backed by the pure domain
// matching engine. Follows the same adapter pattern as NexusUnitOfWork --
// bridges an Application abstraction with a domain implementation.
//
// CLASS DOCUMENTATION:
// - OrderBookService: Singleton service wrapping a single Domain.OrderBook
//   instance configured for the "BTC/USD" trading pair. All methods delegate
//   directly to the inner OrderBook without additional logic.
//
// MEMBER DOCUMENTATION:
// - _orderBook: The underlying domain matching engine instance.
// - Match(): Delegates to Domain.OrderBook.Match(), returning all trades.
// - AddOrder(): Delegates to Domain.OrderBook.Match() to place an order.
// ============================================================================

namespace NexusEngine.Infrastructure.OrderBook;

using NexusEngine.Api.Domain.Entities;
using NexusEngine.Application.Abstractions;
using NexusEngine.Domain.OrderBook;

public class OrderBookService : IOrderBookService
{
    private readonly NexusEngine.Domain.OrderBook.OrderBook _orderBook;

    public OrderBookService()
    {
        _orderBook = new NexusEngine.Domain.OrderBook.OrderBook("BTC/USD");
    }

    public MatchResult Match(Order order)
    {
        return _orderBook.Match(order);
    }

    public void AddOrder(Order order)
    {
        _orderBook.Match(order);
    }
}
