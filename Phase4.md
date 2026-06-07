## Phase 4 -- Full Recap

---

### Objective
Add real-time push notifications to Nexus Engine via SignalR, with an interactive React dashboard and automated tests.

---

### Completed Tasks

**Task 1 -- Branch and SignalR Setup**
- Created branch `phase-4` from `main`
- Added package `Microsoft.AspNetCore.SignalR.Core`
- Created `backend-csharp/Hubs/NexusHub.cs`
- Registered SignalR in `Program.cs` with `AddSignalR()` and `MapHub<NexusHub>("/hubs/nexus")`

**Task 2 -- MediatR Notifications**
- Created folder `Application/Orders/Notifications/`
- Created three `INotification` records:
  - `TradeExecutedNotification` -- list of executed trades
  - `OrderBookChangedNotification` -- market symbol that changed
  - `BalanceChangedNotification` -- accountId, balance, reservedBalance
- `PlaceOrderHandler` publishes all three notifications after match
- `CancelOrderHandler` publishes `OrderBookChangedNotification` and `BalanceChangedNotification`

**Task 3 -- Notification Handlers**
- Created folder `Infrastructure/Notifications/`
- Created three `INotificationHandler`:
  - `TradeExecutedNotificationHandler` -- broadcasts `"TradesExecuted"`
  - `OrderBookChangedNotificationHandler` -- broadcasts `"OrderBookSnapshot"` via `IOrderBookService.GetSnapshot()`
  - `BalanceChangedNotificationHandler` -- broadcasts `"BalanceChanged"`
- Added `GetSnapshot()` to `IOrderBookService` and implemented in `OrderBookService` with thread-safe lock
- Extended `Trade` with `BuyOrderId`, `SellOrderId`, `ExecutedAt`
- Configured CORS in `Program.cs` with `AllowCredentials()` for WebSocket support

**Task 4 -- React Frontend Dashboard**
- Installed `@microsoft/signalr`
- Created `src/hooks/useNexusHub.ts` -- SignalR connection with `withAutomaticReconnect()`
- Created `src/components/NexusDashboard.tsx` with:
  - Real-time panels: Order Book, Recent Trades, Balance Update
  - Action forms: Create Account, Deposit, Place Order, Cancel Order, Get Orders
  - Guide sidebar with: How to Use, Technical Concepts (formulas), Market Data
  - Responsive layout: fixed sidebar on desktop, accordion on mobile
- Created `src/api/nexusApi.ts` with all REST calls
- Fixed CORS backend for cross-origin WebSocket connection
- Fixed `createAccount()` and `placeOrder()` to extract `res.id` from response

**Task 5 -- Tests**
- Added `Moq` package to test project
- Created `backend-csharp.Tests/Application/Notifications/NotificationHandlerTests.cs`
- 8 new xUnit + Moq tests -- total **20/20 passing**
- Created `nexus_e2e_test.py` -- Selenium E2E test with headless Edge

---

### Commands Used

```bash
# Branch
git checkout main
git pull origin main
git checkout -b phase-4

# Backend packages
dotnet add package Microsoft.AspNetCore.SignalR.Core --version 8.*
dotnet add backend-csharp.Tests/ package Moq

# Build and test
dotnet build
dotnet test backend-csharp.Tests/

# Frontend packages
npm install @microsoft/signalr
npm run build

# Docker
docker compose --profile csharp --profile frontend up --build -d
docker compose --profile csharp --profile frontend down -v
docker compose ps
docker compose logs --tail=10 backend-csharp

# Database
docker exec -it nexus-engine-postgres-1 psql -U nexus -d nexusdb

# E2E test
pip install selenium webdriver-manager --break-system-packages
python nexus_e2e_test.py

# Commit
git add -A
git commit -m "feat: Phase 4 - SignalR real-time hub, MediatR notifications, React dashboard with guide sidebar, Selenium E2E tests, 20/20 unit tests passing"
git push origin phase-4
```

---

### PostgreSQL Queries Run

```sql
-- Full event store
SELECT aggregate_id, event_type, aggregate_version, occurred_at
FROM domain_events
ORDER BY occurred_at;

-- Accounts
SELECT id, balance, reserved_balance, status FROM accounts;

-- Orders
SELECT id, side, price, quantity, remaining_quantity, status FROM orders;

-- Transactions
SELECT account_id, type, amount, created_at FROM transactions ORDER BY created_at;
```

---

### Test Cases

| # | Test | Expected | Result |
|---|------|----------|--------|
| 1 | Buy + Sell at same price and quantity | Trade executed, balance updated | PASS |
| 2 | Cancel pending order | Reserved released, order removed from Order Book | PASS |
| 3 | Insufficient balance | 400 error, order rejected | PASS |
| 4 | Partial fill (Buy 0.5, Sell 0.2) | Trade 0.2, Buy residual 0.3 stays in book | PASS |
| 5 | Price priority (Buy 49000 and 51000, Sell 49000) | Sell matches Buy at 51000 first | PASS |

**Edge Cases:**

| # | Edge Case | Expected | Result |
|---|-----------|----------|--------|
| E1 | Insufficient balance (100 for order requiring 5000) | `Insufficient available balance. Available: 100.00, Required: 5000.0` | PASS |
| E2 | Cancel order with wrong account | `Order does not belong to this account` | PASS |

---

### Bugs Fixed During Phase

- `net8.0` → `net9.0` in `.csproj` and Dockerfile to align with local runtime
- Removed `[FromBody]` from `CreateAccount` -- endpoint requires no body
- `createAccount()` and `placeOrder()` in `nexusApi.ts` now extract `res.id` instead of returning the full object
- Missing CORS caused SignalR `Disconnected` -- added `AllowCredentials()` with `WithOrigins("http://localhost:3000")`
- `Trade` extended with `BuyOrderId`, `SellOrderId`, `ExecutedAt` for complete SignalR payload

---

### Notification Architecture

```
PlaceOrderHandler
    └── _mediator.Publish(TradeExecutedNotification)
    └── _mediator.Publish(OrderBookChangedNotification)
    └── _mediator.Publish(BalanceChangedNotification)
            │
            ▼
    INotificationHandler (Infrastructure layer)
            │
            ▼
    IHubContext<NexusHub>.Clients.All.SendAsync(...)
            │
            ▼
    React frontend (useNexusHub hook)
```

Application layer has no knowledge of SignalR. Decoupling is guaranteed by MediatR.

