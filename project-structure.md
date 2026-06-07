# Nexus Engine — Project Structure

```
Nexus-Engine/
├── docker-compose.yml                   # Profili: csharp, frontend
├── Makefile                             # up-csharp, down, db-shell, logs-csharp
├── AGENTS.md                            # Guida per agent AI
├── Phase1.md / Phase2.md                # Roadmap
├── README.md
│
├── backend-csharp/                      # ▲ C# ASP.NET Core 8 — Clean Architecture
│   ├── Program.cs                       # Punto d'ingresso + DI composition root
│   ├── NexusEngine.Api.csproj
│   ├── Dockerfile / .dockerignore
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   │
│   ├── Controllers/                     # 🟢 Presentation Layer
│   │   ├── AccountsController.cs        # POST/GET /api/accounts, deposit, replay
│   │   └── OrdersController.cs          # POST/GET /api/orders
│   │
│   ├── Application/                     # 🔵 Application Layer (CQRS)
│   │   ├── Abstractions/
│   │   │   └── INexusUnitOfWork.cs      # Contratto UoW (DbSet<Account, Order, ...>)
│   │   │
│   │   ├── Accounts/
│   │   │   ├── Commands/
│   │   │   │   ├── CreateAccount/
│   │   │   │   │   ├── CreateAccountCommand.cs
│   │   │   │   │   └── CreateAccountHandler.cs
│   │   │   │   └── DepositFunds/
│   │   │   │       ├── DepositFundsCommand.cs
│   │   │   │       └── DepositFundsHandler.cs
│   │   │   └── Queries/
│   │   │       ├── GetAccount/
│   │   │       │   ├── GetAccountQuery.cs
│   │   │       │   ├── GetAccountHandler.cs
│   │   │       │   └── AccountDto.cs
│   │   │       └── ReplayAccount/
│   │   │           ├── ReplayAccountQuery.cs
│   │   │           ├── ReplayAccountHandler.cs
│   │   │           └── ReplayAccountDto.cs
│   │   │
│   │   └── Orders/
│   │       ├── Commands/
│   │       │   └── PlaceOrder/
│   │       │       ├── PlaceOrderCommand.cs
│   │       │       └── PlaceOrderHandler.cs    # FOR UPDATE + Transaction ledger
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
│   │   └── Entities/
│   │       ├── Account.cs              # balance, reserved_balance, status
│   │       ├── DomainEvent.cs          # Append-only event store
│   │       ├── IdempotencyKey.cs       # Chiave idempotenza
│   │       ├── Order.cs                # Proiezione ordine
│   │       └── Transaction.cs          # Ledger contabile
│   │
│   ├── Infrastructure/                 # 🟣 Infrastructure Layer
│   │   ├── Idempotency/
│   │   │   └── IdempotencyFilter.cs    # Action Filter (X-Idempotency-Key)
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
│   │       ├── 20260516202232_InitialSchema.cs
│   │       ├── 20260517115412_AddAccountBalanceConstraints.cs
│   │       ├── 20260525074915_AddOrderSymbolAndRemainingQuantity.cs
│   │       ├── 20260525075743_FixOrderColumnNames.cs
│   │       └── NexusDbContextModelSnapshot.cs
│   │
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── bin/ / obj/
│   └── NexusEngine.Api.http
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
    ├── node_modules/
    └── src/
        ├── main.tsx
        ├── index.css
        ├── App.tsx
        ├── App.css
        └── assets/
```

---

## API Endpoints

| Method | Path | Controller | Handler | Descrizione |
|--------|------|-----------|---------|-------------|
| POST | `/api/accounts` | `AccountsController.CreateAccount` | `CreateAccountHandler` | Crea account → 201 |
| GET | `/api/accounts/{id}` | `AccountsController.GetAccount` | `GetAccountHandler` | Legge proiezione → 200 / 404 |
| POST | `/api/accounts/{id}/deposit` | `AccountsController.DepositFunds` | `DepositFundsHandler` | Deposita fondi → 204 |
| GET | `/api/accounts/{id}/replay` | `AccountsController.ReplayAccount` | `ReplayAccountHandler` | Ricostruisce da eventi → 200 / 404 |
| POST | `/api/orders` | `OrdersController.PlaceOrder` | `PlaceOrderHandler` | Piazza ordine → 201 / 400 / 404 / 409 |
| GET | `/api/orders?accountId={id}` | `OrdersController.GetOrders` | `GetOrdersHandler` | Elenca ordini → 200 |

---

## Architettura (in 10 righe)

- **Event Sourcing**: `domain_events` (append-only) è la source of truth. `accounts`, `orders`, `transactions` sono proiezioni sincrone aggiornate nella stessa transazione DB (atomic dual-write).
- **CQRS**: MediatR command/query in `Application/`, controller sottili in `Controllers/`.
- **Clean Architecture**: `Domain/` (C# puro) → `Application/` (dipende solo da `INexusUnitOfWork`) → `Infrastructure/` (EF Core, migration).
- **Optimistic Locking**: Unique index `(aggregate_id, aggregate_version)` su `domain_events`. Postgres errore `23505` = scrittura concorrente rilevata.
- **Pessimistic Locking**: `SELECT ... FOR UPDATE` su `accounts` in `PlaceOrderHandler`.
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
