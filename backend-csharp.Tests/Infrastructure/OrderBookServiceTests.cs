namespace NexusEngine.Tests.Infrastructure;

using NexusEngine.Api.Domain.Entities;
using NexusEngine.Infrastructure.OrderBook;
using Xunit;
using Book = NexusEngine.Domain.OrderBook.OrderBook;

public class OrderBookServiceTests
{
    private static Order NewOrder(string symbol, string side, decimal price, decimal qty) => new()
    {
        Id = Guid.NewGuid(),
        Symbol = symbol,
        Side = side,
        Price = price,
        Quantity = qty,
        RemainingQuantity = qty
    };

    [Fact]
    public void Book_Add_DoesNotMatchCrossingOrders()
    {
        var book = new Book("TEST");
        book.Add(NewOrder("TEST", "Buy", 100m, 1m));
        book.Add(NewOrder("TEST", "Sell", 90m, 1m));

        Assert.Single(book.Bids);
        Assert.Single(book.Asks);
    }

    [Fact]
    public void Book_Match_RestingRemainderIsInsertedExactlyOnce()
    {
        var book = new Book("TEST");
        book.Match(NewOrder("TEST", "Buy", 100m, 2m));

        Assert.Single(book.Bids[100m]);
    }

    [Fact]
    public void Book_TryRemoveOrder_MissingOrder_ReturnsFalse()
    {
        var book = new Book("TEST");
        Assert.False(book.TryRemoveOrder(NewOrder("TEST", "Buy", 100m, 1m)));
    }

    [Fact]
    public void Book_Clone_IsIndependentOfOriginal()
    {
        var book = new Book("TEST");
        book.Match(NewOrder("TEST", "Sell", 100m, 5m));
        var clone = book.Clone();

        book.Match(NewOrder("TEST", "Buy", 100m, 5m));

        Assert.Empty(book.Asks);
        Assert.Equal(5m, clone.Asks[100m].Peek().RemainingQuantity);
    }

    [Fact]
    public void Service_BooksAreIsolatedPerSymbol()
    {
        var service = new OrderBookService();
        service.Match(NewOrder("BTC/USD", "Sell", 100m, 1m));

        var result = service.Match(NewOrder("ETH/USD", "Buy", 200m, 1m));

        Assert.Empty(result.Trades);
    }

    [Fact]
    public void Service_Restore_RevertsMatching()
    {
        var service = new OrderBookService();
        service.Match(NewOrder("BTC/USD", "Sell", 100m, 1m));

        var checkpoint = service.Checkpoint("BTC/USD");
        Assert.Single(service.Match(NewOrder("BTC/USD", "Buy", 100m, 1m)).Trades);

        service.Restore(checkpoint);

        Assert.Single(service.Match(NewOrder("BTC/USD", "Buy", 100m, 1m)).Trades);
    }

    [Fact]
    public async Task Service_Acquire_SerializesWriters()
    {
        var service = new OrderBookService();
        var first = await service.AcquireAsync();

        var second = service.AcquireAsync();
        await Task.Delay(50);
        Assert.False(second.IsCompleted);

        first.Dispose();
        (await second).Dispose();
    }
}
