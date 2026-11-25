// Matching engine abstraction. Books are kept per symbol in memory.
// Mutations (Match/Load/Remove/Checkpoint/Restore) must happen while holding the
// engine lock obtained from AcquireAsync, so that the in-memory book and the
// database are changed as a single serialized unit of work.

namespace NexusEngine.Application.Abstractions;

using NexusEngine.Api.Domain.Entities;
using NexusEngine.Domain.OrderBook;

public interface IOrderBookService
{
    Task<IDisposable> AcquireAsync(CancellationToken cancellationToken = default);

    MatchResult Match(Order order);

    void Load(Order order);

    bool RemoveOrder(Order order);

    IOrderBookCheckpoint Checkpoint(string symbol);

    void Restore(IOrderBookCheckpoint checkpoint);

    object GetSnapshot(string symbol);
}

public interface IOrderBookCheckpoint
{
    string Symbol { get; }
}
