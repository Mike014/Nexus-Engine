// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Domain.OrderBook
//
// FILE DESCRIPTION:
// Contains the MatchResult class, which encapsulates the outcome of a
// matching operation in the order book.
//
// CLASS DOCUMENTATION:
// - MatchResult: Immutable result object holding the list of trades produced
//   when an incoming order is matched against resting orders. Includes a
//   static factory method for convenient empty result creation.
//
// MEMBER DOCUMENTATION:
// - Trades: A read-only list of Trade records generated during matching.
// - Empty(): Static factory returning a MatchResult with no trades.
// ============================================================================

namespace NexusEngine.Domain.OrderBook;

public class MatchResult
{
    public IReadOnlyList<Trade> Trades { get; }

    public MatchResult(IReadOnlyList<Trade> trades)
    {
        Trades = trades;
    }

    public static MatchResult Empty() => new MatchResult(Array.Empty<Trade>());
}
