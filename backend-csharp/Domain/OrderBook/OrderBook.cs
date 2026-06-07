// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Domain.OrderBook
//
// FILE DESCRIPTION:
// Contains the OrderBook class, the core limit-order matching engine.
// Maintains two sorted price-ordered dictionaries (bids descending,
// asks ascending) and implements price-time priority FIFO matching.
//
// CLASS DOCUMENTATION:
// - OrderBook: Pure domain logic matching engine with zero infrastructure
//   dependencies. Accepts incoming orders and matches them against resting
//   liquidity using price-time priority rules. Unmatched quantity is added
//   to the appropriate side of the book.
//
// MEMBER DOCUMENTATION:
// - Symbol: The trading pair symbol this book manages.
// - Bids: Read-only view of resting buy orders, sorted descending by price.
// - Asks: Read-only view of resting sell orders, sorted ascending by price.
// - Match(): Processes an incoming order against the book. For buy orders,
//   matches against asks where taker price >= ask price. For sell orders,
//   matches against bids where taker price <= bid price. Returns all
//   trades produced. Unmatched quantity is added to the resting book.
// ============================================================================

namespace NexusEngine.Domain.OrderBook;

using NexusEngine.Api.Domain.Entities;

public class OrderBook
{
    private readonly SortedDictionary<decimal, Queue<Order>> _bids;
    private readonly SortedDictionary<decimal, Queue<Order>> _asks;

    public OrderBook(string symbol)
    {
        Symbol = symbol;
        _bids = new SortedDictionary<decimal, Queue<Order>>(
            Comparer<decimal>.Create((a, b) => b.CompareTo(a)));
        _asks = new SortedDictionary<decimal, Queue<Order>>();
    }

    public string Symbol { get; }

    public IReadOnlyDictionary<decimal, Queue<Order>> Bids => _bids;

    public IReadOnlyDictionary<decimal, Queue<Order>> Asks => _asks;

    public MatchResult Match(Order incomingOrder)
    {
        var trades = new List<Trade>();

        if (incomingOrder.Side.Equals("Buy", StringComparison.OrdinalIgnoreCase))
        {
            TryMatchAgainstAsks(incomingOrder, trades);
        }
        else
        {
            TryMatchAgainstBids(incomingOrder, trades);
        }

        if (incomingOrder.RemainingQuantity > 0)
        {
            AddToBook(incomingOrder);
        }

        return new MatchResult(trades);
    }

    private void TryMatchAgainstAsks(Order taker, List<Trade> trades)
    {
        while (taker.RemainingQuantity > 0 && _asks.Count > 0)
        {
            var bestAsk = _asks.First();
            if (taker.Price < bestAsk.Key)
                break;

            var maker = bestAsk.Value.Peek();
            var matchQuantity = Math.Min(taker.RemainingQuantity, maker.RemainingQuantity);

            trades.Add(new Trade(maker.Id, maker.Price, matchQuantity));

            taker.RemainingQuantity -= matchQuantity;
            maker.RemainingQuantity -= matchQuantity;

            if (maker.RemainingQuantity == 0)
            {
                bestAsk.Value.Dequeue();
                if (bestAsk.Value.Count == 0)
                {
                    _asks.Remove(bestAsk.Key);
                }
            }
        }
    }

    private void TryMatchAgainstBids(Order taker, List<Trade> trades)
    {
        while (taker.RemainingQuantity > 0 && _bids.Count > 0)
        {
            var bestBid = _bids.First();
            if (taker.Price > bestBid.Key)
                break;

            var maker = bestBid.Value.Peek();
            var matchQuantity = Math.Min(taker.RemainingQuantity, maker.RemainingQuantity);

            trades.Add(new Trade(maker.Id, maker.Price, matchQuantity));

            taker.RemainingQuantity -= matchQuantity;
            maker.RemainingQuantity -= matchQuantity;

            if (maker.RemainingQuantity == 0)
            {
                bestBid.Value.Dequeue();
                if (bestBid.Value.Count == 0)
                {
                    _bids.Remove(bestBid.Key);
                }
            }
        }
    }

    private void AddToBook(Order order)
    {
        var book = order.Side.Equals("Buy", StringComparison.OrdinalIgnoreCase)
            ? _bids
            : _asks;

        if (!book.TryGetValue(order.Price, out var queue))
        {
            queue = new Queue<Order>();
            book[order.Price] = queue;
        }

        queue.Enqueue(order);
    }
}
