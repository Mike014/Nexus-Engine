# Nexus Engine -- Phase 3: Order Book & Matching

## Objective

Build the core limit-order matching engine as pure domain logic with zero
infrastructure dependencies, enabling price-time priority FIFO matching
between resting and incoming orders.

---

## Sprint 3A -- Progress

### Task 3A-1: COMPLETED

**Domain/OrderBook layer created (pure C#, no EF Core, no MediatR, no ASP.NET):**

```
Domain/OrderBook/
    Trade.cs              -- record with MakerOrderId, Price, Quantity
    MatchResult.cs        -- IReadOnlyList of Trade, static Empty() factory
    OrderBook.cs          -- pure domain matching engine
```

**OrderBook implementation details:**

- Internal state: two `SortedDictionary<decimal, Queue<Order>>` — bids with
  descending comparer, asks with default ascending comparer
- `Match(Order)` decomposes into `TryMatchAgainstAsks` (buys) and
  `TryMatchAgainstBids` (sells)
- Price-time priority FIFO: match when taker bid >= best ask, or taker ask <= best bid
- Exhausted price levels removed from dictionary automatically
- Unmatched quantity added to the resting book after matching

**Test project created:**

```
backend-csharp.Tests/
    NexusEngine.Tests.csproj      -- xUnit, references NexusEngine.Api
    Domain/OrderBook/
        OrderBookTests.cs         -- 6 unit tests
```

**Test scenarios:**

| # | Scenario | Assertion |
|---|----------|-----------|
| 1 | Buy order with no matching asks | 0 trades, order sits in bids |
| 2 | Sell order with no matching bids | 0 trades, order sits in asks |
| 3 | Exact full match at same price | 1 trade, both consumed, book empty |
| 4 | Partial match (taker larger) | 1 trade, taker remains in bids |
| 5 | Multiple makers same price level | 2 trades in FIFO order |
| 6 | Price priority (two asks, different prices) | Cheaper ask hit first |

**Build/Tests:**

```
dotnet build:  0 warnings, 0 errors
dotnet test:   6/6 passed (555 ms)
```

---

## Technical Debt

`backend-csharp.Tests` references `NexusEngine.Api` directly. Acceptable for now.
When the project grows, Domain tests should be isolated in a dedicated
`NexusEngine.Domain.Tests` project with no dependency on the Api layer.

---

## Project Status

**Phase 1:** COMPLETE
**Phase 2:** COMPLETE
**Phase 3 (Sprint 3A):** Matching engine implemented
**Phase 4:** WebSocket real-time -- next
**Phase 5:** Observability and Polish
