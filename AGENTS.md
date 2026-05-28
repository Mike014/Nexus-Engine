# Nexus Engine — Agent Guide

## Active branch
`phase-2` is HEAD. `main` is stable. Work on `phase-2`.

## Architecture in 10 lines

- **Event Sourcing**: `domain_events` (append-only) is the source of truth. `accounts`, `orders`, `transactions` are synchronous projections updated in the same DB transaction (atomic dual-write).
- **CQRS**: MediatR commands/queries in `Application/`, thin controllers in `Controllers/`.
- **Clean Architecture**: `Domain/` (pure C#) → `Application/` (depends on `INexusUnitOfWork` only) → `Infrastructure/` (EF Core, migrations).
- **Optimistic Locking**: Unique index `(aggregate_id, aggregate_version)` on `domain_events`. Postgres error `23505` = concurrent write detected.
- **Single active backend (ADR-003)**: C# and Java **cannot run simultaneously** — in-memory order book would diverge.

## Switch backends via Docker Compose profiles

```bash
make up-csharp    # docker compose --profile csharp up --build
make up-java      # docker compose --profile java up --build
make down         # stops all profiles
make logs-csharp  # tail C# logs
make logs-java    # tail Java logs
make db-shell     # psql -U nexus -d nexusdb
```

Port `5000:8080` for both backends. Frontend on `3000`. One backend at a time — no port conflict.

## Schema ownership (ADR-005)

**C# is schema master** — EF Core migrations own the schema.
**Java is slave** — Hibernate `ddl-auto=validate` only (never creates/alters tables).

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

No test project exists for C# backend.

## Java build & run

```bash
cd backend-java
./mvnw.cmd package -DskipTests
# Only skeleton — main class + context load test exist, no domain/controllers yet.
```

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
| POST | `/api/orders` | Place order (reserves funds) |

## Key domain entities

| Entity | Table | Purpose |
|--------|-------|---------|
| `DomainEvent` | `domain_events` | Append-only event store (JSONB payload) |
| `Account` | `accounts` | Read-side projection (balance, reserved_balance, status) |
| `Order` | `orders` | Read-side projection (side, price, qty, status) |
| `Transaction` | `transactions` | Ledger row linking account/order/event |
| `IdempotencyKey` | `idempotency_keys` | Duplicate request protection (table exists, not fully wired) |

## Frontend

Vite 8 scaffold with placeholder Vite/React demo content. **Not customized for Nexus Engine**. Build:
```bash
cd frontend
npm run dev      # local dev
npm run build    # tsc -b && vite build
npm run lint     # eslint .
```

## Conventions

- **No XML doc comments** in code — remove them if present.
- **Snake_case** for DB columns, **PascalCase** for C# properties (EF Core fluent mapping bridges the two).
- `DateTime.UtcNow` everywhere — never local time.
- Handlers throw `KeyNotFoundException` (→ 404) and `InvalidOperationException` (→ 400) for business rule violations.
- Controllers do **zero business logic** — parse request → build command/query → dispatch via `IMediator`.
- `INexusUnitOfWork` is the only persistence abstraction the Application layer sees.

## What doesn't exist yet

- Java backend: missing entities, repositories, controllers, services (skeleton only).
- Frontend: not customized — still showing Vite/React demo.
- Order book / matching engine (Phase 3).
- Real-time: SignalR (C#) / STOMP (Java) (Phase 4).
- Observability: structured logging, health checks, metrics (Phase 5).
- CI/CD: no GitHub Actions workflows.
- Tests: C# backend has no test project.
