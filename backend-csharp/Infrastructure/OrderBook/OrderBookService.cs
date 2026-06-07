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
//   directly to the inner OrderBook without additional logic. Thread-safe
//   via instance-level locking.
//
// MEMBER DOCUMENTATION:
// - _orderBook: The underlying domain matching engine instance.
// - _lock: Synchronization guard for singleton thread safety.
// - Match(): Delegates to Domain.OrderBook.Match(), returning all trades.
// - AddOrder(): Delegates to Domain.OrderBook.Match() to place an order.
// - RemoveOrder(): Thread-safe removal of a resting order from the book.
// - GetSnapshot(): Thread-safe snapshot of the current order book state.
// ============================================================================

namespace NexusEngine.Infrastructure.OrderBook;

using NexusEngine.Api.Domain.Entities;
using NexusEngine.Application.Abstractions;
using NexusEngine.Domain.OrderBook;

public class OrderBookService : IOrderBookService
{
    private readonly NexusEngine.Domain.OrderBook.OrderBook _orderBook;
    private readonly object _lock = new();

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

    public void RemoveOrder(Order order)
    {
        lock (_lock)
        {
            _orderBook.RemoveOrder(order);
        }
    }

    public object GetSnapshot()
    {
        lock (_lock)
        {
            var bids = _orderBook.Bids
                .Select(b => new
                {
                    Price = b.Key,
                    Quantity = b.Value.Sum(o => o.RemainingQuantity)
                })
                .ToList();

            var asks = _orderBook.Asks
                .Select(a => new
                {
                    Price = a.Key,
                    Quantity = a.Value.Sum(o => o.RemainingQuantity)
                })
                .ToList();

            return new
            {
                Symbol = _orderBook.Symbol,
                Bids = bids,
                Asks = asks
            };
        }
    }
}
