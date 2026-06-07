# Nexus Engine — Project Structure

```
Nexus-Engine/
├── docker-compose.yml                   # Profili: csharp, frontend
├── Makefile                             # up-csharp, down, db-shell, logs-csharp
├── AGENTS.md                            # Guida per agent AI
├── Phase1.md / Phase2.md / Phase3.md    # Roadmap
├── project-structure.md                 # Questo file
├── README.md
│
├── backend-csharp/                      # ▲ C# ASP.NET Core 8 — Clean Architecture
│   ├── Program.cs                       # Punto d'ingresso + DI composition root
│   ├── NexusEngine.Api.csproj
│   ├── Dockerfile / .dockerignore
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── NexusEngine.Api.http
│   ├── Properties/
│   │   └── launchSettings.json
│   │
│   ├── Controllers/                     # 🟢 Presentation Layer
│   │   ├── AccountsController.cs        # POST/GET /api/accounts, deposit, replay
│   │   └── OrdersController.cs          # POST/GET/DELETE /api/orders
│   │
│   ├── Application/                     # 🔵 Application Layer (CQRS)
│   │   ├── Abstractions/
│   │   │   ├── INexusUnitOfWork.cs      # Contratto UoW (DbSet<Account, Order, ...>)
│   │   │   └── IOrderBookService.cs     # Contratto Order Book in-memory
│   │   │
│   │   ├── Common/
│   │   │   └── OptimisticConcurrencyHelper.cs  # Rilevamento errore Postgres 23505
│   │   │
│   │   ├── Accounts/
│   │   │   ├── Commands/
│   │   │   │   ├── CreateAccount/
│   │   │   │   │   ├── CreateAccountCommand.cs
│   │   │   │   │   └── CreateAccountHandler.cs
│   │   │   │   └── DepositFunds/
│   │   │   │       ├── DepositFundsCommand.cs
│   │   │   │       └── DepositFundsHandler.cs    # Retry ottimistico (max 3)
│   │   │   └── Queries/
│   │   │       ├── GetAccount/
│   │   │       │   ├── GetAccountQuery.cs
│   │   │       │   └── GetAccountHandler.cs      # Inline AccountDto
│   │   │       └── ReplayAccount/
│   │   │           ├── ReplayAccountQuery.cs
│   │   │           └── ReplayAccountHandler.cs   # Inline ReplayAccountDto
│   │   │
│   │   └── Orders/
│   │       ├── Commands/
│   │       │   ├── PlaceOrder/
│   │       │   │   ├── PlaceOrderCommand.cs
│   │       │   │   └── PlaceOrderHandler.cs      # Matching engine + retry
│   │       │   └── CancelOrder/
│   │       │       ├── CancelOrderCommand.cs
│   │       │       └── CancelOrderHandler.cs     # Rimuove da OrderBook
│   │       ├── Queries/
│   │       │   └── GetOrders/
│   │       │       ├── GetOrdersQuery.cs
│   │       │       ├── GetOrdersHandler.cs
│   │       │       └── OrderDto.cs
│   │       └── Validation/
│   │           ├── IOrderValidationStrategy.cs       # Interfaccia Strategy
│   │           ├── AccountExistsValidation.cs        # 1°: null check
│   │           ├── AccountActiveValidation.cs        # 2°: status check
│   │           └── SufficientBalanceValidation.cs    # 3°: saldo check
│   │
│   ├── Domain/                         # 🟡 Domain Layer (entità pure)
│   │   ├── Entities/
│   │   │   ├── Account.cs              # balance, reserved_balance, status
│   │   │   ├── DomainEvent.cs          # Append-only event store
│   │   │   ├── IdempotencyKey.cs       # Chiave idempotenza
│   │   │   ├── Order.cs                # Proiezione ordine
│   │   │   └── Transaction.cs          # Ledger contabile
│   │   └── OrderBook/                  # 🆕 Matching Engine (dominio puro)
│   │       ├── Trade.cs                # Record trade eseguito
│   │       ├── MatchResult.cs          # Risultato match (trades + ordini residuali)
│   │       └── OrderBook.cs            # Engine: price-time priority FIFO
│   │
│   ├── Infrastructure/                 # 🟣 Infrastructure Layer
│   │   ├── Idempotency/
│   │   │   └── IdempotencyFilter.cs    # Action Filter (X-Idempotency-Key)
│   │   ├── OrderBook/                  # 🆕 Implementazione Order Book
│   │   │   ├── OrderBookService.cs     # Singleton thread-safe con lock
│   │   │   └── OrderBookRecoveryService.cs  # IHostedService: ricarica ordini all'avvio
│   │   ├── Persistence/
│   │   │   ├── NexusDbContext.cs       # EF Core DbContext
│   │   │   ├── NexusUnitOfWork.cs      # Implementazione UoW
│   │   │   └── Configurations/        # Fluent API (snake_case)
│   │   │       ├── AccountConfiguration.cs
│   │   │       ├── DomainEventConfiguration.cs
│   │   │       ├── IdempotencyKeyConfiguration.cs
│   │   │       ├── OrderConfiguration.cs
│   │   │       └── TransactionConfiguration.cs
│   │   └── Migrations/                # 4 migration EF Core
│   │       ├── 20260516202232_InitialSchema.cs (+ .Designer)
│   │       ├── 20260517115412_AddAccountBalanceConstraints.cs (+ .Designer)
│   │       ├── 20260525074915_AddOrderSymbolAndRemainingQuantity.cs (+ .Designer)
│   │       ├── 20260525075743_FixOrderColumnNames.cs (+ .Designer)
│   │       └── NexusDbContextModelSnapshot.cs
│   │
│   └── bin/ / obj/
│
├── backend-csharp.Tests/               # 🧪 Progetto test xUnit
│   ├── NexusEngine.Tests.csproj
│   └── Domain/
│       └── OrderBook/
│           └── OrderBookTests.cs       # 12 test: match, add, remove, FIFO, rifiuto
│
└── frontend/                           # Vite 8 + React (placeholder / non customizzato)
    ├── Dockerfile
    ├── nginx.conf
    ├── package.json
    ├── vite.config.ts
    ├── tsconfig.json / tsconfig.app.json / tsconfig.node.json
    ├── eslint.config.js
    ├── index.html
    ├── public/
    │   ├── favicon.svg
    │   └── icons.svg
    ├── node_modules/
    └── src/
        ├── main.tsx
        ├── index.css
        ├── App.tsx
        ├── App.css
        └── assets/
            ├── hero.png
            ├── react.svg
            └── vite.svg
```

---

## API Endpoints

| Method | Path | Controller | Handler | Descrizione |
|--------|------|-----------|---------|-------------|
| POST | `/api/accounts` | `AccountsController.Create` | `CreateAccountHandler` | Crea account → 201 |
| GET | `/api/accounts/{id}` | `AccountsController.Get` | `GetAccountHandler` | Legge proiezione → 200 / 404 |
| POST | `/api/accounts/{id}/deposit` | `AccountsController.Deposit` | `DepositFundsHandler` | Deposita fondi (con retry ottimistico) → 204 |
| GET | `/api/accounts/{id}/replay` | `AccountsController.Replay` | `ReplayAccountHandler` | Ricostruisce da eventi → 200 / 404 |
| POST | `/api/orders` | `OrdersController.PlaceOrder` | `PlaceOrderHandler` | Piazza ordine → esegue matching → 201 / 400 / 404 / 409 |
| GET | `/api/orders?accountId={id}` | `OrdersController.GetOrders` | `GetOrdersHandler` | Elenca ordini → 200 |
| DELETE | `/api/orders/{orderId}` | `OrdersController.CancelOrder` | `CancelOrderHandler` | Cancella ordine aperto → 204 / 404 |

---

## Architettura (in 10 righe)

- **Event Sourcing**: `domain_events` (append-only) è la source of truth. `accounts`, `orders`, `transactions` sono proiezioni sincrone aggiornate nella stessa transazione DB (atomic dual-write).
- **CQRS**: MediatR command/query in `Application/`, controller sottili in `Controllers/`.
- **Clean Architecture**: `Domain/` (C# puro) → `Application/` (dipende solo da `INexusUnitOfWork`) → `Infrastructure/` (EF Core, migration, servizi esterni).
- **Order Book in-memory**: `Domain/OrderBook/OrderBook.cs` (dominio puro, zero dipendenze). `OrderBookService` singleton thread-safe con lock. Ricaricato all'avvio da `OrderBookRecoveryService`.
- **Matching Engine**: Price-time priority FIFO. `SortedDictionary` per bids (desc) e asks (asc). Match/add/remove operations.
- **Optimistic Locking**: Unique index `(aggregate_id, aggregate_version)` su `domain_events`. Postgres errore `23505` → retry (max 3 con jitter 50-300ms).
- **Idempotency**: Action Filter `IdempotencyFilter` con header `X-Idempotency-Key` su `POST /api/orders`.

---

## Database

- **PostgreSQL 16**, DB `nexusdb`, user `nexus`
- Tutte le tabelle/colonne in **snake_case** (EF Core fluent configs in `Configurations/`)
- Colonne monetarie: `NUMERIC(18,2)`. Colonne quantità: `NUMERIC(18,8)`. Mai `float`/`double`.
- CHECK constraints: `balance >= 0`, `reserved_balance >= 0`.

### Tabelle

| Tabella | Entità | Scopo |
|---------|--------|-------|
| `domain_events` | `DomainEvent` | Append-only event store (payload JSONB) |
| `accounts` | `Account` | Proiezione read-side (balance, reserved_balance, status) |
| `orders` | `Order` | Proiezione read-side (side, price, qty, status) |
| `transactions` | `Transaction` | Ledger riga che collega account/order/event |
| `idempotency_keys` | `IdempotencyKey` | Protezione richieste duplicate |

---

## Convenzioni C#

- **Niente commenti XML doc** nel codice
- **Snake_case** per colonne DB, **PascalCase** per proprietà C# (EF Core fluent mapping)
- `DateTime.UtcNow` ovunque — mai ora locale
- Le handler lanciano `KeyNotFoundException` (→ 404) e `InvalidOperationException` (→ 400)
- I controller non fanno **zero business logic** — parsano request → costruiscono command/query → dispatciano via `IMediator`
- `INexusUnitOfWork` è l'unica astrazione di persistenza che il layer Application vede
