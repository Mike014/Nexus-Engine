// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Tests
// Layer: Domain.OrderBook
//
// FILE DESCRIPTION:
// Unit tests for the OrderBook matching engine covering price-time priority
// FIFO matching, partial fills, and resting order placement.
//
// TEST DOCUMENTATION:
// - Each test follows Arrange / Act / Assert structure.
// - No mocking frameworks used; OrderBook is pure domain logic.
// ============================================================================

namespace NexusEngine.Tests.Domain.OrderBook;

using NexusEngine.Api.Domain.Entities;
using NexusEngine.Domain.OrderBook;
using Xunit;

public class OrderBookTests
{
    private static Order CreateOrder(Guid id, string side, decimal price, decimal quantity, decimal remainingQuantity)
    {
        return new Order
        {
            Id = id,
            Symbol = "TEST",
            Side = side,
            Price = price,
            Quantity = quantity,
            RemainingQuantity = remainingQuantity,
        };
    }

    [Fact]
    public void BuyOrder_NoMatchingAsks_SitsInBids()
    {
        // Arrange
        var book = new NexusEngine.Domain.OrderBook.OrderBook("TEST");
        var buyOrder = CreateOrder(Guid.NewGuid(), "Buy", 100m, 10m, 10m);

        // Act
        var result = book.Match(buyOrder);

        // Assert
        Assert.Empty(result.Trades);
        Assert.Contains(100m, book.Bids.Keys);
        Assert.Contains(buyOrder, book.Bids[100m]);
    }

    [Fact]
    public void SellOrder_NoMatchingBids_SitsInAsks()
    {
        // Arrange
        var book = new NexusEngine.Domain.OrderBook.OrderBook("TEST");
        var sellOrder = CreateOrder(Guid.NewGuid(), "Sell", 100m, 10m, 10m);

        // Act
        var result = book.Match(sellOrder);

        // Assert
        Assert.Empty(result.Trades);
        Assert.Contains(100m, book.Asks.Keys);
        Assert.Contains(sellOrder, book.Asks[100m]);
    }

    [Fact]
    public void ExactFullMatch_OneBuyOneSell_OneTradeBothConsumed()
    {
        // Arrange
        var book = new NexusEngine.Domain.OrderBook.OrderBook("TEST");
        var makerId = Guid.NewGuid();
        var maker = CreateOrder(makerId, "Sell", 100m, 10m, 10m);
        book.Match(maker);

        var taker = CreateOrder(Guid.NewGuid(), "Buy", 100m, 10m, 10m);

        // Act
        var result = book.Match(taker);

        // Assert
        var trade = Assert.Single(result.Trades);
        Assert.Equal(makerId, trade.MakerOrderId);
        Assert.Equal(100m, trade.Price);
        Assert.Equal(10m, trade.Quantity);
        Assert.Empty(book.Bids);
        Assert.Empty(book.Asks);
    }

    [Fact]
    public void PartialMatch_TakerLargerThanMaker_TakerRemainsInBids()
    {
        // Arrange
        var book = new NexusEngine.Domain.OrderBook.OrderBook("TEST");
        var maker = CreateOrder(Guid.NewGuid(), "Sell", 100m, 5m, 5m);
        book.Match(maker);

        var takerId = Guid.NewGuid();
        var taker = CreateOrder(takerId, "Buy", 100m, 10m, 10m);

        // Act
        var result = book.Match(taker);

        // Assert
        var trade = Assert.Single(result.Trades);
        Assert.Equal(5m, trade.Quantity);
        Assert.Equal(5m, taker.RemainingQuantity);
        Assert.Contains(100m, book.Bids.Keys);
        Assert.Contains(taker, book.Bids[100m]);
    }

    [Fact]
    public void MultipleMakers_SamePriceLevel_FifoOrder()
    {
        // Arrange
        var book = new NexusEngine.Domain.OrderBook.OrderBook("TEST");
        var maker1Id = Guid.NewGuid();
        var maker2Id = Guid.NewGuid();
        var maker1 = CreateOrder(maker1Id, "Sell", 100m, 5m, 5m);
        var maker2 = CreateOrder(maker2Id, "Sell", 100m, 3m, 3m);
        book.Match(maker1);
        book.Match(maker2);

        var taker = CreateOrder(Guid.NewGuid(), "Buy", 100m, 8m, 8m);

        // Act
        var result = book.Match(taker);

        // Assert
        Assert.Equal(2, result.Trades.Count);
        Assert.Equal(maker1Id, result.Trades[0].MakerOrderId);
        Assert.Equal(5m, result.Trades[0].Quantity);
        Assert.Equal(maker2Id, result.Trades[1].MakerOrderId);
        Assert.Equal(3m, result.Trades[1].Quantity);
        Assert.Empty(book.Bids);
        Assert.Empty(book.Asks);
    }

    [Fact]
    public void PricePriority_TwoAsksDifferentPrices_HitsCheaperFirst()
    {
        // Arrange
        var book = new NexusEngine.Domain.OrderBook.OrderBook("TEST");
        var expensiveAskId = Guid.NewGuid();
        var cheapAskId = Guid.NewGuid();
        var expensiveAsk = CreateOrder(expensiveAskId, "Sell", 102m, 5m, 5m);
        var cheapAsk = CreateOrder(cheapAskId, "Sell", 101m, 5m, 5m);
        book.Match(expensiveAsk);
        book.Match(cheapAsk);

        var taker = CreateOrder(Guid.NewGuid(), "Buy", 102m, 8m, 8m);

        // Act
        var result = book.Match(taker);

        // Assert
        Assert.Equal(2, result.Trades.Count);
        Assert.Equal(cheapAskId, result.Trades[0].MakerOrderId);
        Assert.Equal(101m, result.Trades[0].Price);
        Assert.Equal(5m, result.Trades[0].Quantity);
        Assert.Equal(expensiveAskId, result.Trades[1].MakerOrderId);
        Assert.Equal(102m, result.Trades[1].Price);
        Assert.Equal(3m, result.Trades[1].Quantity);
    }

    [Fact]
    public void RemoveOrder_SingleOrder_RemovedAndPriceLevelCleared()
    {
        // Arrange
        var book = new NexusEngine.Domain.OrderBook.OrderBook("TEST");
        var order = CreateOrder(Guid.NewGuid(), "Buy", 100m, 10m, 10m);
        book.Match(order);

        // Act
        book.RemoveOrder(order);

        // Assert
        Assert.DoesNotContain(100m, book.Bids.Keys);
    }

    [Fact]
    public void RemoveOrder_MultipleOrdersAtSamePrice_OnlyTargetRemoved()
    {
        // Arrange
        var book = new NexusEngine.Domain.OrderBook.OrderBook("TEST");
        var keepOrder = CreateOrder(Guid.NewGuid(), "Buy", 100m, 5m, 5m);
        var removeOrder = CreateOrder(Guid.NewGuid(), "Buy", 100m, 3m, 3m);
        book.Match(keepOrder);
        book.Match(removeOrder);

        // Act
        book.RemoveOrder(removeOrder);

        // Assert
        Assert.Contains(100m, book.Bids.Keys);
        Assert.Contains(keepOrder, book.Bids[100m]);
        Assert.DoesNotContain(removeOrder, book.Bids[100m]);
    }

    [Fact]
    public void RemoveOrder_OrderNotInBook_ThrowsInvalidOperationException()
    {
        // Arrange
        var book = new NexusEngine.Domain.OrderBook.OrderBook("TEST");
        var order = CreateOrder(Guid.NewGuid(), "Buy", 100m, 10m, 10m);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => book.RemoveOrder(order));
        Assert.Contains("not found", ex.Message);
    }
}
