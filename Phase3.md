# Nexus Engine -- Phase 3: Order Book & Matching

## Objective

Build the core limit-order matching engine as pure domain logic with zero
infrastructure dependencies, enabling price-time priority FIFO matching
between resting and incoming orders.

---

## Sprint 3A -- COMPLETED

### Task 3A-1: Domain -- OrderBook, MatchResult, Trade -- COMPLETED

- Created `Domain/OrderBook/Trade.cs` -- immutable record with MakerOrderId, Price, Quantity
- Created `Domain/OrderBook/MatchResult.cs` -- match result container with static Empty() factory
- Created `Domain/OrderBook/OrderBook.cs` -- pure domain matching engine, SortedDictionary bids (descending) / asks (ascending), price-time priority FIFO, methods: Match, AddOrder, RemoveOrder
- Created `backend-csharp.Tests/Domain/OrderBook/OrderBookTests.cs` -- 9 xUnit tests: no-match, full match, partial fill, FIFO, price priority, remove single, remove from multi, remove missing order throws
- Build: 0 warnings, 0 errors. Tests: 9/9 passed.

### Task 3A-2: Infrastructure -- IOrderBookService singleton -- COMPLETED

- Created `Application/Abstractions/IOrderBookService.cs` -- contract in Application layer (Match, AddOrder, RemoveOrder)
- Created `Infrastructure/OrderBook/OrderBookService.cs` -- singleton implementation, thread-safe with `lock(_lock)`, fixed symbol BTC/USD
- Modified `Program.cs` -- registered as `AddSingleton`

### Task 3A-3: Application -- PlaceOrderHandler integration -- COMPLETED

- Modified `Application/Orders/Commands/PlaceOrder/PlaceOrderHandler.cs`
- Integrated IOrderBookService via constructor injection
- Added `private static ApplyTradeToAccount` method -- handles balance movements by order side
- Flow: validation -> match -> process trades -> update maker and taker accounts -> AddOrder if partial -> single SaveChangesAsync

### Task 3A-4/5: CancelOrder command -- COMPLETED

- Created `Application/Orders/Commands/CancelOrder/CancelOrderCommand.cs`
- Created `Application/Orders/Commands/CancelOrder/CancelOrderHandler.cs` -- verifies ownership, cancellable status, refunds reserved_balance, appends OrderCancelled event
- Modified `Controllers/OrdersController.cs` -- added `DELETE /api/orders/{orderId}?accountId={id}` returning 204 NoContent
- Modified `Domain/OrderBook/OrderBook.cs` -- added RemoveOrder with fail-fast on missing order
- Modified `Application/Abstractions/IOrderBookService.cs` -- added RemoveOrder to contract
- Modified `Infrastructure/OrderBook/OrderBookService.cs` -- implemented RemoveOrder with `lock`

### Commit history Sprint 3A

```
d3ecb95 -- feat: CancelOrder + RemoveOrder + DELETE endpoint (Task 3A-4/5)
e28c94d -- feat: PlaceOrderHandler integrazione OrderBookService (Task 3A-3)
953291d -- feat: IOrderBookService + OrderBookService singleton (Task 3A-2)
811bcf2 -- feat: OrderBook domain layer + xUnit + Phase3.md (Task 3A-1)
```

### API endpoints after Sprint 3A

| Method | Path | Phase |
|--------|------|-------|
| POST   | `/api/accounts` | Phase 1 |
| GET    | `/api/accounts/{id}` | Phase 1 |
| POST   | `/api/accounts/{id}/deposit` | Phase 1 |
| GET    | `/api/accounts/{id}/replay` | Phase 1 |
| POST   | `/api/orders` | Phase 2 |
| GET    | `/api/orders?accountId={id}` | Phase 2 |
| DELETE | `/api/orders/{orderId}?accountId={id}` | Phase 3 |

---

## Technical Debt

`backend-csharp.Tests` references `NexusEngine.Api` directly. When the project
grows, Domain tests should be isolated in a dedicated
`NexusEngine.Domain.Tests` project with no dependency on the Api layer.

---

## Sprint 3B -- NEXT

### Task 3B-1: Optimistic Locking

Version column on aggregates, retry on conflict.

### Task 3B-2: Recovery

Order Book rebuild at restart via event replay.

---

## Project Status

**Phase 1:** COMPLETE
**Phase 2:** COMPLETE
**Phase 3 (Sprint 3A):** COMPLETE
**Phase 3 (Sprint 3B):** Pending
**Phase 4:** WebSocket real-time
**Phase 5:** Observability and Polish
