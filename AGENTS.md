# Nexus Engine — Agent Guide

## Active branch
`main` is the integration branch (phases 1-5 merged). Work on feature branches off `main`.

## Architecture in 10 lines

- **Event Sourcing**: `domain_events` (append-only) is the source of truth. `accounts`, `orders`, `transactions` are synchronous projections updated in the same DB transaction (atomic dual-write).
- **CQRS**: MediatR commands/queries in `Application/`, thin controllers in `Controllers/`.
- **Clean Architecture**: `Domain/` (pure C#) → `Application/` (depends on `INexusUnitOfWork` only) → `Infrastructure/` (EF Core, migrations).
- **Optimistic Locking**: Unique index `(aggregate_id, aggregate_version)` on `domain_events`. Postgres error `23505` = concurrent write detected.
- **Matching engine**: in-memory, one `OrderBook` per symbol (`OrderBookService` singleton). Writers hold `AcquireAsync()` for match + persist; the book is checkpointed and restored if `SaveChanges` fails. Recovery reloads open orders with `Load` (no matching). Single instance only.
- **Ledger**: every balance-affecting event is written through `AccountLedger.Append` and carries `BalanceDelta`/`ReservedDelta`, so `/replay` rebuilds the projection exactly. Makers get their own events.
- **Real-time**: MediatR notifications -> SignalR hub `/hubs/nexus`.
## Docker Compose

```bash
make up-csharp    # docker compose --profile csharp up --build
make down         # stops all profiles
make logs-csharp  # tail C# logs
make db-shell     # psql -U nexus -d nexusdb
```

Port `5000:8080`. Frontend on `3000`.

## Production (Railway, no Docker)

`railway.toml` builds with Railpack (`dotnet publish`). Dashboard: Root Directory `backend-csharp`, Postgres plugin (`DATABASE_URL` is auto-converted), App Sleeping off, 1 replica.

### Generate a migration
```bash
cd backend-csharp
dotnet ef migrations add <Name> --output-dir Infrastructure/Migrations
```

Connection string from `appsettings.Development.json` (`localhost:5432`) or env var `ConnectionStrings__DefaultConnection` (Docker).

In Docker, `Program.cs` auto-applies pending migrations at startup via `MigrateAsync()`.

## C# build & run

```bash
cd backend-csharp
dotnet build
dotnet run   # http://localhost:5140, swagger at /swagger
```

Tests: `dotnet test backend-csharp.Tests/NexusEngine.Tests.csproj` (xUnit, Moq, EF InMemory). CI runs them on push/PR to `main`.

## Database

- `postgres:16-alpine`, DB `nexusdb`, user `nexus`, password `nexus_dev_password`
- Local port `5432`
- All tables/columns use **snake_case** (EF Core fluent configs in `Infrastructure/Persistence/Configurations/`)
- Monetary columns: `NUMERIC(18,2)`. Quantity columns: `NUMERIC(18,8)`. Never `float`/`double`.
- CHECK constraints enforce `balance >= 0`, `reserved_balance >= 0`.

## API endpoints (C# active)

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/accounts` | Create account |
| GET | `/api/accounts/{id}` | Get account projection |
| POST | `/api/accounts/{id}/deposit` | Deposit funds |
| GET | `/api/accounts/{id}/replay` | Reconstruct state from events |
| POST | `/api/orders` | Place order (reserves funds, matches) |
| GET | `/api/orders?accountId=` | List orders |
| DELETE | `/api/orders/{id}?accountId=` | Cancel order |
| GET | `/health/live`, `/health/ready` | Health checks |

## Key domain entities

| Entity | Table | Purpose |
|--------|-------|---------|
| `DomainEvent` | `domain_events` | Append-only event store (JSONB payload) |
| `Account` | `accounts` | Read-side projection (balance, reserved_balance, status) |
| `Order` | `orders` | Read-side projection (side, price, qty, status) |
| `Transaction` | `transactions` | Ledger row linking account/order/event |
| `IdempotencyKey` | `idempotency_keys` | `X-Idempotency-Key` on `POST /api/orders`; claimed atomically, scoped to method+path |

## Frontend

Vite + React dashboard (`src/components/NexusDashboard.tsx`, SignalR via `src/hooks/useNexusHub.ts`). Deployed on Vercel.
```bash
cd frontend
npm run dev
npm run build
npm run lint
```

## Conventions

- **No XML doc comments** in code — remove them if present.
- **Snake_case** for DB columns, **PascalCase** for C# properties (EF Core fluent mapping bridges the two).
- `DateTime.UtcNow` everywhere — never local time.
- Handlers throw `KeyNotFoundException` (→ 404) and `InvalidOperationException` (→ 400) for business rule violations.
- Controllers do **zero business logic** — parse request → build command/query → dispatch via `IMediator`.
- `INexusUnitOfWork` is the only persistence abstraction the Application layer sees.

## Known gaps

- No asset positions: sells don't require holdings and buyers don't receive the asset.
- Legacy `OrderMatched` events (written before deltas) are skipped by replay (`LegacyEventsSkipped`).
- Matching engine is in-process: run exactly one backend instance.
