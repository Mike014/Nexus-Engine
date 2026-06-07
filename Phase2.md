# Nexus Engine -- Phase 2 Recap

## Objective

Implement the core transactional engine: order placement with validation,
concurrency control, idempotency, and atomically updated projections.

---

## What We Built

### 1. INexusUnitOfWork -- Technical Debt Fix (ADR-006)

**Problem solved:** Handlers depended directly on `NexusDbContext`,
violating the Dependency Rule. The Application layer knew infrastructure details.

**Solution:**

```
Application/
    Abstractions/
        INexusUnitOfWork.cs       -- interface (Application layer)
Infrastructure/
    Persistence/
        NexusUnitOfWork.cs        -- concrete implementation (Infrastructure layer)
```

`INexusUnitOfWork` exposes database collections and `SaveChangesAsync`.
`NexusUnitOfWork` wraps `NexusDbContext` and exposes it through the interface.
Registered as `Scoped` in `Program.cs` -- same lifetime as `NexusDbContext`.

All four existing Handlers updated:
- `CreateAccountHandler` -- `NexusDbContext _db` -> `INexusUnitOfWork _uow`
- `DepositFundsHandler` -- same
- `GetAccountHandler` -- same
- `ReplayAccountHandler` -- same

---

### 2. PlaceOrder Command

**Implemented flow:**

1. Controller validates input and builds `PlaceOrderCommand`
2. Handler loads the account projection with `SELECT FOR UPDATE` (pessimistic lock)
3. Runs all validation strategies in sequence
4. Computes `reservationAmount = quantity * price` for Buy orders
5. Writes `OrderPlaced` event to the Event Store
6. Creates `Order` projection with status `Pending`
7. Updates `reserved_balance` on the account
8. Writes a record to `transactions`
9. Persists everything in the same atomic transaction

**Files created:**

```
Application/
    Orders/
        Commands/
            PlaceOrder/
                PlaceOrderCommand.cs    -- immutable record, IRequest<Guid>
                PlaceOrderHandler.cs    -- full business logic
Controllers/
    OrdersController.cs                 -- POST /api/orders, GET /api/orders
```

**Endpoint:**

```
POST /api/orders
Body: { accountId, symbol, side, quantity, price }
Response: HTTP 201 { id: orderId }
```

---

### 3. Strategy Pattern for Validation

**Problem solved:** validation rules were hardcoded inside the Handler.
Every new rule required modifying the Handler -- violation of the Open/Closed Principle.

**Solution:** each rule is an independent class implementing `IOrderValidationStrategy`.
The Handler injects `IEnumerable<IOrderValidationStrategy>` and runs them in sequence.
Adding a new rule means creating a new class and registering it -- zero changes to the Handler.

**Strategies implemented:**

```
Application/
    Orders/
        Validation/
            IOrderValidationStrategy.cs         -- contract
            AccountExistsValidation.cs          -- account exists (KeyNotFoundException)
            AccountActiveValidation.cs          -- account is Active (InvalidOperationException)
            SufficientBalanceValidation.cs      -- balance - reserved_balance >= reservationAmount
```

**Registration in Program.cs:**

```csharp
builder.Services.AddScoped<IOrderValidationStrategy, AccountExistsValidation>();
builder.Services.AddScoped<IOrderValidationStrategy, AccountActiveValidation>();
builder.Services.AddScoped<IOrderValidationStrategy, SufficientBalanceValidation>();
```

---

### 4. Pessimistic Locking -- SELECT FOR UPDATE

**Problem solved:** two concurrent requests on the same account could read the same
balance, both pass validation, and produce an inconsistent state (double-spend).

**Solution:** `SELECT FOR UPDATE` locks the account row for the entire duration
of the transaction. The second concurrent request waits until the first commits.

**Implementation in PlaceOrderHandler:**

```csharp
var account = await _uow.Accounts
    .FromSqlInterpolated($"SELECT * FROM accounts WHERE id = {command.AccountId} FOR UPDATE")
    .FirstOrDefaultAsync(cancellationToken);
```

**OS-level connection:** `SELECT FOR UPDATE` uses a row-level lock managed by PostgreSQL.
The second process blocks on a syscall -- no CPU consumed while waiting. When the first
transaction commits, the lock is released and the kernel wakes up the second process.

---

### 5. Idempotency -- X-Idempotency-Key

**Problem solved:** a network request can fail after the server has processed the command
but before the client receives the response. The client retries -- without idempotency,
the order gets placed twice.

**Solution:** Action Filter that intercepts every `POST /api/orders`.

**Flow:**

```
Request arrives with X-Idempotency-Key
    |
    +-- Key absent  --> execute normally
    |
    +-- Key present --> check idempotency_keys table
            |
            +-- Found     --> return cached response (HTTP 201 + same orderId)
            |
            +-- Not found --> execute handler, save key + response atomically
```

**File created:**

```
Infrastructure/
    Idempotency/
        IdempotencyFilter.cs    -- IAsyncActionFilter
```

---

### 6. GetOrders Query

**Endpoint:**

```
GET /api/orders?accountId={id}
Response: HTTP 200 [ { id, symbol, side, quantity, remainingQuantity, price, status, createdAt } ]
```

**Files created:**

```
Application/
    Orders/
        Queries/
            GetOrders/
                OrderDto.cs             -- DTO with 8 fields
                GetOrdersQuery.cs       -- IRequest<List<OrderDto>>
                GetOrdersHandler.cs     -- AsNoTracking, Where(AccountId), map to DTO
```

---

### 7. Transactions Projection

Every `PlaceOrder` writes a record to `transactions` in the same atomic transaction:

```
Type        = "OrderPlaced"
Amount      = -reservationAmount  (negative -- funds being reserved)
AccountId   = command.AccountId
OrderId     = orderId
EventId     = domainEvent.Id
OccurredAt  = DateTime.UtcNow
```

---

### 8. Migrations Added

```
AddOrderSymbolAndRemainingQuantity    -- adds Symbol and RemainingQuantity to the orders table
FixOrderColumnNames                   -- renames Symbol -> symbol, RemainingQuantity -> remaining_quantity
```

**Root cause:** `OrderConfiguration.cs` was missing explicit mappings for the two new
properties -- EF Core used the C# property names instead of snake_case.
Lesson: every new property must have its own `HasColumnName()` in the Configuration.

---

## Errors Encountered and Resolved

| Error | Cause | Fix |
|-------|-------|-----|
| `column "RemainingQuantity" does not exist` | Migration created before adding `HasColumnName` to `OrderConfiguration` | Added mapping + `FixOrderColumnNames` migration |
| Missing `return orderId` in `PlaceOrderHandler` | Method declared `Task<Guid>` but returned nothing | Added `return orderId` |
| Typo `OrderPlace` instead of `OrderPlaced` | Typo in EventType string | Corrected |
| `NexusUnitOfWork` created as file without extension | Manual error during file creation | Deleted and recreated correctly |
| Port 5000 already allocated | Previous container not stopped | `docker compose down --remove-orphans` |

---

## Commands Used

```powershell
# Build
dotnet build --no-incremental
dotnet restore

# Migrations
dotnet ef migrations add AddOrderSymbolAndRemainingQuantity --output-dir Infrastructure/Migrations
dotnet ef migrations add FixOrderColumnNames --output-dir Infrastructure/Migrations

# Docker
docker compose --profile csharp up --build -d
docker compose down --remove-orphans
docker compose logs backend-csharp --tail=60

# Database verification
docker compose exec postgres psql -U nexus -d nexusdb -c "\d orders"
docker compose exec postgres psql -U nexus -d nexusdb -c "SELECT id, symbol, side, quantity, remaining_quantity, price, status FROM orders;"
docker compose exec postgres psql -U nexus -d nexusdb -c "SELECT id, balance, reserved_balance FROM accounts WHERE id = '<UUID>';"
docker compose exec postgres psql -U nexus -d nexusdb -c "SELECT event_type, aggregate_version, payload FROM domain_events WHERE aggregate_id = '<UUID>' ORDER BY aggregate_version;"

# Endpoint tests
Invoke-WebRequest -Uri "http://localhost:5000/api/accounts" `
  -Method POST -ContentType "application/json" `
  -Body '{"ownerName": "Test User", "currency": "EUR"}' -UseBasicParsing

Invoke-WebRequest -Uri "http://localhost:5000/api/accounts/<UUID>/deposit" `
  -Method POST -ContentType "application/json" `
  -Body '{"amount": 1000.00}' -UseBasicParsing

Invoke-WebRequest -Uri "http://localhost:5000/api/orders" `
  -Method POST -ContentType "application/json" `
  -Headers @{"X-Idempotency-Key" = "test-key-001"} `
  -Body '{"accountId": "<UUID>", "symbol": "BTC-EUR", "side": "Buy", "quantity": 2, "price": 100.00}' `
  -UseBasicParsing

Invoke-WebRequest -Uri "http://localhost:5000/api/orders?accountId=<UUID>" `
  -Method GET -UseBasicParsing

# Git
git add .
git commit -m "feat: Phase 2 complete -- Strategy Pattern, Pessimistic Locking, Idempotency, GetOrders, Transactions projection"
git push origin phase-2
```

---

## Phase 2 Checklist -- COMPLETE

```
[x] INexusUnitOfWork introduction (ADR-006 technical debt fix)
[x] PlaceOrder Command
[x] Strategy Pattern for validation
[x] Pessimistic Locking (SELECT FOR UPDATE)
[x] Idempotency (X-Idempotency-Key header)
[x] Transactions projection
[x] GetOrders query
```

---

## End-to-End Test Results

```
Test 1 -- POST /api/accounts              HTTP 201  OK
Test 2 -- POST /api/accounts/{id}/deposit HTTP 204  OK
Test 3 -- POST /api/orders (first time)   HTTP 201  OK -- order created
Test 4 -- POST /api/orders (same key)     HTTP 201  OK -- same orderId, not re-executed
Test 5 -- GET  /api/orders?accountId={}   HTTP 200  OK -- order list correct
Test 6 -- POST /api/orders (no funds)     HTTP 400  OK -- correctly rejected
```

---

## Project Status

**Phase 1:** COMPLETE
**Phase 2:** COMPLETE
**Phase 3:** Order Book and Matching -- next
**Phase 4:** WebSocket real-time
**Phase 5:** Observability and Polish