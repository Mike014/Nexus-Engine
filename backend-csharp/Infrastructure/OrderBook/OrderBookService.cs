// In-memory, per-symbol matching engine. A SemaphoreSlim serializes writers for the
// whole duration of a command (match + persist); an inner lock protects reads
// (snapshots) that may run concurrently from notification handlers.

namespace NexusEngine.Infrastructure.OrderBook;

using NexusEngine.Api.Domain.Entities;
using NexusEngine.Application.Abstractions;
using NexusEngine.Domain.OrderBook;
using Book = NexusEngine.Domain.OrderBook.OrderBook;

public class OrderBookService : IOrderBookService
{
    private readonly Dictionary<string, Book> _books = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _writer = new(1, 1);
    private readonly object _lock = new();

    public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken = default)
    {
        await _writer.WaitAsync(cancellationToken);
        return new Releaser(_writer);
    }

    public MatchResult Match(Order order)
    {
        lock (_lock)
            return GetOrCreate(order.Symbol).Match(order);
    }

    public void Load(Order order)
    {
        lock (_lock)
            GetOrCreate(order.Symbol).Add(order);
    }

    public bool RemoveOrder(Order order)
    {
        lock (_lock)
            return _books.TryGetValue(order.Symbol, out var book) && book.TryRemoveOrder(order);
    }

    public IOrderBookCheckpoint Checkpoint(string symbol)
    {
        lock (_lock)
            return new BookCheckpoint(symbol, GetOrCreate(symbol).Clone());
    }

    public void Restore(IOrderBookCheckpoint checkpoint)
    {
        var cp = (BookCheckpoint)checkpoint;
        lock (_lock)
            _books[cp.Symbol] = cp.Book;
    }

    public object GetSnapshot(string symbol)
    {
        lock (_lock)
        {
            _books.TryGetValue(symbol, out var book);

            var bids = book?.Bids
                .Select(b => new { Price = b.Key, Quantity = b.Value.Sum(o => o.RemainingQuantity) })
                .Where(l => l.Quantity > 0)
                .ToList() ?? [];

            var asks = book?.Asks
                .Select(a => new { Price = a.Key, Quantity = a.Value.Sum(o => o.RemainingQuantity) })
                .Where(l => l.Quantity > 0)
                .ToList() ?? [];

            return new { Symbol = symbol, Bids = bids, Asks = asks };
        }
    }

    private Book GetOrCreate(string symbol)
    {
        if (!_books.TryGetValue(symbol, out var book))
        {
            book = new Book(symbol);
            _books[symbol] = book;
        }
        return book;
    }

    private sealed record BookCheckpoint(string Symbol, Book Book) : IOrderBookCheckpoint;

    private sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                semaphore.Release();
        }
    }
}
