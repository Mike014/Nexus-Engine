# Nexus Engine

<div align="center">
  <img src="assets/images/logo.jpg" alt="Nexus Engine"
            alt="Nexus Engine" 
            width="50%" 
            style="max-width: 1100px; border-radius: 12px;">
</div>

<br>

A simulated order/betting engine built to demonstrate production-grade backend engineering skills relevant to iGaming and Fintech companies.

Nexus Engine is not a commercial product. It is a technical portfolio project that showcases mastery of transactional systems, concurrency, real-time communication, and configurable business rules — implemented twice, in C# and Java, sharing the same database and frontend.

### **Documentation**
- [Nexus Engine](https://docs.google.com/document/d/1_g5yustt4jpWbeg5F9ApeEEyxlwxWvcyzC5pAtTxDK4/edit?tab=t.0#heading=h.cfhcybw6q3iy)
- [Phase1](https://github.com/Mike014/Nexus-Engine/blob/main/Phase1.md)
---

## What This Project Demonstrates

- **Event Sourcing** — every state change is recorded as an immutable domain event. The event store is the source of truth. Current state is a projection derived from events.
- **CQRS** — strict separation between write operations (Commands) and read operations (Queries) via MediatR (.NET) and manual pattern implementation (Java).
- **Concurrency control** — progressive strategy: Pessimistic Locking (Phase 2) migrated to Optimistic Locking (Phase 3), documented as a conscious engineering decision.
- **Atomic dual-write** — domain event and read-side projection written in the same database transaction. No inconsistent state possible.
- **Event replay** — any aggregate's state can be reconstructed at any point in time by replaying its event stream.
- **Configurable business rules** — validation rules (limits, odds, fees) are externalized, not hardcoded.
- **Real-time updates** — WebSocket push notifications via SignalR (C#) and STOMP (Java).
- **Polyglot architecture** — two independent backend implementations sharing the same PostgreSQL schema and React frontend, demonstrating language-agnostic system design.

---

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                   React Frontend                    │
│              (TypeScript + Vite + nginx)            │
└──────────────────────┬──────────────────────────────┘
                       │ HTTP + WebSocket
          ┌────────────┴────────────┐
          │                         │
┌─────────▼──────────┐   ┌──────────▼─────────┐
│   Backend C#        │   │   Backend Java      │
│   ASP.NET Core 8    │   │   Spring Boot 3.5   │
│   MediatR + EF Core │   │   Spring Data JPA   │
│   SignalR           │   │   STOMP WebSocket   │
└─────────┬──────────┘   └──────────┬──────────┘
          │                         │
          └────────────┬────────────┘
                       │
              ┌────────▼────────┐
              │   PostgreSQL 16  │
              │                 │
              │  domain_events  │  ← source of truth (append-only)
              │  accounts       │  ← read-side projection
              │  orders         │  ← read-side projection
              │  transactions   │  ← read-side projection
              │  idempotency_keys│
              └─────────────────┘
```

> **Architectural constraint (ADR-003):** only one backend runs at a time. The order book lives in-memory inside the backend process. Running both backends simultaneously would produce divergent in-memory state. Switch between backends using Docker Compose profiles.

---

## Tech Stack

### Backend C# (Master)
- ASP.NET Core 8 Web API
- Entity Framework Core 8 + Npgsql
- MediatR 12 (CQRS)
- SignalR (real-time WebSocket)
- Swashbuckle (Swagger UI)

### Backend Java (Slave)
- Spring Boot 3.5
- Spring Data JPA + Hibernate 6
- Spring WebSocket (STOMP)
- Maven

### Database
- PostgreSQL 16
- Schema owned by C# (EF Core Migrations)
- Java uses `ddl-auto=validate` — Hibernate validates but never modifies schema

### Frontend
- React 18 + TypeScript
- Vite 8
- nginx (production serving)

### Infrastructure
- Docker + Docker Compose (profiles for backend switching)
- GitHub Actions (CI pipeline with boot tests)

---

## Getting Started

### Prerequisites

- Docker Desktop (WSL2 backend on Windows)
- Git

### Run with C# backend

```bash
git clone https://github.com/Mike014/Nexus-Engine-.git
cd Nexus-Engine
docker compose --profile csharp up --build
```

### Run with Java backend

```bash
docker compose --profile java up --build
```

### Switch between backends

```bash
# Stop current backend
docker compose --profile csharp down

# Start the other
docker compose --profile java up --build
```

### Available endpoints (C# backend)

```
POST   /api/accounts              Create a new account
GET    /api/accounts/{id}         Get current account state (from projection)
POST   /api/accounts/{id}/deposit Deposit funds
GET    /api/accounts/{id}/replay  Reconstruct account state from event stream
GET    /swagger                   Swagger UI
```

---

## Architectural Decisions

### ADR-001: Event Sourcing as persistence strategy
The event store (`domain_events`) is the source of truth. Current state (`accounts`, `orders`) is a read-side projection derived from events. This makes audit trails intrinsic, enables point-in-time replay, and eliminates the need for a separate audit table.

### ADR-002: Progressive concurrency strategy
- **Phase 2:** Pessimistic Locking (`SELECT FOR UPDATE`) — simple, correct, serializes operations on the same account.
- **Phase 3:** Migration to Optimistic Locking (version column on aggregates) — documented refactoring demonstrating awareness of trade-offs between throughput and correctness guarantees.

### ADR-003: Single active backend
Both backends share the same PostgreSQL database. The in-memory order book cannot be shared between processes without a coordination layer (Redis, etc.). One backend is active at a time. Switch via Docker Compose profiles.

### ADR-004: Synchronous projections (Phases 1–3)
Read-side projections are updated in the same database transaction as the domain event write. Strong consistency, no lag between write and read model. Async projection (outbox pattern / LISTEN-NOTIFY) is documented as a future evolution.

### ADR-005: C# as schema master, Java as slave
EF Core Migrations own the schema. Hibernate runs in `validate` mode — it validates entity mappings against the existing schema but never modifies it. Schema drift is caught early via CI boot tests.

---

## Development Roadmap

### Phase 1 — Foundation ✅
- Project setup (C# + Java + React)
- Docker Compose with backend profiles
- Database schema: Event Store + projections
- Account CRUD with Event Sourcing
- Event replay endpoint

### Phase 2 — Transactional Engine 🔄
- Place order endpoint
- Validation with Strategy Pattern
- Atomic balance updates via events
- Pessimistic Locking (`SELECT FOR UPDATE`)
- Request idempotency (idempotency key)

### Phase 3 — Order Book and Matching
- Buy/sell matching engine (price-time priority, in-memory)
- Partial order fills
- Order lifecycle: Pending → PartiallyFilled → Filled / Cancelled
- Migration: Pessimistic → Optimistic Locking
- Order book recovery on restart

### Phase 4 — Real-time
- SignalR push (C#) / STOMP WebSocket (Java)
- Live order book, balance updates, trade feed
- React dashboard with live data

### Phase 5 — Observability and Polish
- Structured logging (Serilog / Logback)
- Health checks
- Architecture diagrams and ADR documentation
- Event replay demonstration
- Metrics (Prometheus + Grafana, optional)

---

## Domain Model (iGaming / Fintech)

Nexus Engine models a betting exchange (inspired by Betfair) and a financial trading platform (inspired by LMAX):

- **Back = Buy** — "I bet €10 that X wins at odds 3.0"
- **Lay = Sell** — "I offer the counterpart at odds 3.0"
- **Price-time priority matching** — orders matched by best price first, FIFO at equal price
- **Partial fills** — an order can be partially matched, remainder stays in the book
- **Configurable commission** — exchange fee on net winnings, never on stake

---

## References

- [LMAX Architecture](https://martinfowler.com/articles/lmax.html) — Martin Fowler
- [Betfair Exchange API](https://developer.betfair.com) — domain model reference
- [Event Sourcing](https://martinfowler.com/eaaDev/EventSourcing.html) — Martin Fowler
- [CQRS](https://martinfowler.com/bliki/CQRS.html) — Martin Fowler

---

