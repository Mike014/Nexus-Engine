# Sprint 1 — Phase 1: Foundations
## Technical Architecture & Summary Report

This document provides a comprehensive technical overview of Phase 1 (Foundations) for the **NexusEngine** platform. It outlines the current state, architectural choices, domain modeling principles, database schema configurations, and the specific commands executed during setup.

---

## 1. Project Status & Checklist

All core foundational objectives for Phase 1 have been successfully implemented and validated:

* **[v] C# Backend Setup:** ASP.NET Core 8 Web API project configured with Swashbuckle OpenAPI.
* **[v] Java Backend Setup:** Spring Boot 3.5.14 project initialized using Java 21, JPA, and Hibernate.
* **[v] Docker Orchestration:** Multi-container ecosystem orchestrated via `docker-compose.yml` utilizing Docker Profiles (`--profile csharp` / `--profile java`) to seamlessly switch the active transactional backend.
* **[v] Frontend Environment:** React + TypeScript single-page application built via Vite, containerized and served using Nginx as a reverse proxy.
* **[v] Database Schema & Strategy:** PostgreSQL instance containing an immutable Event Store and read-model projection tables (`accounts`, `orders`, `transactions`).
* **[v] Core Transactional Workflows:** * `CreateAccount`: Atomic dual-write writing to the event store and updating synchronous projections.
    * `GetAccount`: Clean read-side query leveraging optimized database scanning.
    * `DepositFunds`: Command execution on an existing aggregate demonstrating transactional business validation and version increments.
* **[v] Diagnostics & Replay:** In-memory aggregate state reconstruction via historic event stream replays.

---

## 2. Architectural Paradigm & Design Patterns

The system implements an audio-first/high-performance transactional engine adhering to advanced structural patterns to ensure strict decoupling, auditability, and scalability.

### Dual-Backend Specular Design
The platform maintains two separate, independent implementations of the identical transactional domain: ASP.NET Core and Spring Boot. Both backends share a single, unified database schema. Isolation of computational logic from data storage allows runtime flexibility, performance benchmarking, and architectural specularity.

### Event Sourcing (Source of Truth)
State mutation is captured not by overwriting an existing record, but by appending immutable, fine-grained business facts to an explicit Event Store (`domain_events`). 
* **Granularity:** Events are designed around granular business facts with observable financial or stateful consequences (e.g., `AccountCreated`, `FundsDeposited`). 
* **Diagnostic Power:** As noted by Martin Fowler, this guarantees perfect diagnostic auditability. Any anomalous system behavior can be debugged by copying the exact event sequence into a local testing sandbox and replaying it step-by-step to isolate logic deviations.

### CQRS (Command Query Responsibility Segregation)
The architecture imposes a strict operational divide between write-side actions (Commands) and read-side operations (Queries):
* **Write Side (Commands):** Validates business constraints against the current state, appends to the Event Store, and updates projections.
* **Read Side (Queries):** Directly queries decoupled relational projection tables, completely bypassing the Event Store to maximize throughput and minimize latency.

### Atomic Dual-Write Projections
For Phases 1–3, consistency takes precedence. Events and their corresponding read-model projections are committed within the **same database transaction**. This guarantees immediate read-model alignment (Strong Consistency) with an intentional, well-documented overhead of two database operations per write.


```text

[HTTP Client] ──> [Controller] ──> [MediatR / Command]
│
▼
[Command Handler]
│
┌────────────────┴────────────────┐
▼                                 ▼
(Append Immutable Event)            (Update Current State)
[Table: domain_events]              [Table: accounts/orders]
│                                 │
└───────────────┬─────────────────┘
▼
[PostgreSQL Transaction]
(Commit Atomic / All-or-Nothing)
```

---

## 3. Domain Model & Dependency Rules

The project strictly follows Clean Architecture and Domain-Driven Design (DDD) principles, segregating boundaries to keep business rules decoupled from external frameworks.

### The Dependency Rule
Dependencies flow exclusively inward: **Domain <── Application <── Infrastructure**. 
* **Domain Layer:** The absolute core of the application. It consists of pure C# objects independent of databases, HTTP routers, or ORMs. If the entire infrastructure were swapped, the domain rules would remain untouched.
* **Infrastructure Layer:** The outermost boundary. It handles persistence, external API integration, and framework configurations.

### Domain Artifacts
* **Entities:** Objects with a distinct, continuous identity tracking through time regardless of attribute mutations.
    * `Account`: Tracks ownership, multi-currency balances, and systemic status.
    * `Order`: Captures intent of transaction execution across distinct lifecycles.
    * `Transaction`: Represents single immutable financial movements.
* **Value Objects:** Objects defined solely by their attributes, lacking structural identity (e.g., specific currency amounts).
* **Domain Events:** Fully immutable records of atomic business facts, identified by unique UUIDs.

---

## 4. Database Schema & Persistence Strategy

The schema is explicitly mapped via Entity Framework (EF) Core inside the Infrastructure layer, preserving Domain purity by keeping data mapping out of entity classes.

### Structural Schema Mapping (`IEntityTypeConfiguration<T>`)
Instead of relying on automated ORM conventions or intrusive data annotations, explicit fluent mapping configurations are defined. This isolates structural requirements within `Infrastructure/Persistence/Configurations/`.

```csharp
// Example: DomainEvent mapping to custom PostgreSQL types
builder.ToTable("domain_events");

builder.Property(e => e.Payload)
    .HasColumnName("payload")
    .HasColumnType("jsonb")
    .IsRequired();

builder.HasIndex(e => new { e.AggregateId, e.AggregateVersion })
    .IsUnique()
    .HasDatabaseName("uq_aggregate_version");

```

### Critical Database Design Decisions

1. **`NUMERIC(18,2)` for Financial Ledgering:** Double or float types are strictly forbidden for tracking financial values due to IEEE 754 floating-point rounding errors. High-precision decimals preserve ledger integrity.
2. **`JSONB` Data Type:** Payload data fields are stored utilizing PostgreSQL `jsonb`. This provides native compressed binary storage, indexing support, and efficient structured document querying capabilities.
3. **Implicit Optimistic Locking:** A composite unique index `UNIQUE (aggregate_id, aggregate_version)` is enforced on the `domain_events` table. If concurrent execution contexts attempt to write identical versions for the same aggregate, PostgreSQL throws a constraint violation, instantly protecting data integrity without explicit table locks.
4. **`onDelete: Restrict` Enforcement:** Cascade deletions are blocked globally across financial foreign keys. Financial entries must maintain permanent relational history.

### Schema Management & Sync

* **C# as Master:** EF Core migrations act as the single source of truth for the database schema.
* **Java Validation:** Hibernate is constrained with `ddl-auto=validate`, ensuring it verifies schema compatibility upon application startup without attempting schema alterations.

---

## 5. System Infrastructure & Components

* **Dockerfiles:** Multi-stage build recipes deployed across all components to isolate development environments, caching intermediate build stages and spitting out highly lightweight, secure runtime images.
* **`docker-compose.yml`:** Coordinates service meshes, internal virtual networking, ports, environment variables, and isolation using Docker Profiles.
* **`nginx.conf`:** Configured as a high-performance reverse proxy. It serves the React static files and routes incoming traffic based on request URIs (`/api/v1/csharp` or `/api/v1/java`) into the hidden inner container network, while seamlessly resolving CORS constraints.
* **Makefile:** Acts as a uniform CLI layer to encapsulate long-form Docker or system operations into clean, memorable developer tasks (e.g., `make build`, `make up`).

---

## 6. Technical Debt & Future Evolutions

While Phase 1 successfully establishes a functional, containerized architecture, specific constraints have been acknowledged to accelerate early delivery:

1. **Direct DbContext Coupling:** Command Handlers currently reference `NexusDbContext` directly. This compromises pure DDD layering. Future refactoring will abstract persistence interfaces using the *Repository* and *Unit of Work* patterns.
2. **Synchronous Projection Overhead:** Writing to both the Event Store and read-side projections within a single transaction creates operational overhead. As scaling demands increase, this will evolve into an asynchronous pattern driven by an **Outbox Pattern** combined with PostgreSQL `LISTEN/NOTIFY` or message brokers.

---

## 7. Executed Commands Reference

### Docker Infrastructure Management

```powershell
# Verify installation versions
docker --version
docker compose version

# Spin up system using specific backend profiles
docker compose --profile csharp up --build
docker compose --profile csharp up -d

# Force a clean, un-cached rebuild of a service
docker compose --profile csharp build --no-cache backend-csharp

# Check active container states and configuration
docker compose ps
docker compose --profile csharp config

# Spin up background PostgreSQL database service exclusively
docker compose up postgres -d

# Query database schema status natively via psql container CLI
docker compose exec postgres psql -U nexus -d nexusdb -c "\\dt"
docker compose exec postgres psql -U nexus -d nexusdb -c "SELECT id, owner_name, balance, status FROM accounts;"
docker compose exec postgres psql -U nexus -d nexusdb -c "SELECT event_type, aggregate_version, payload FROM domain_events ORDER BY aggregate_version;"

```

### Frontend Workspace Initialization

```powershell
# Create standard React + TypeScript application template using Vite
npm create vite@latest . -- --template react-ts

# Install local package node modules dependencies
npm install

```

### C# / .NET 8 Backend Development

```powershell
# Initialize core Web API structure with standard Controllers
dotnet new webapi -n NexusEngine.Api --use-controllers

# Package dependencies management
dotnet add package Swashbuckle.AspNetCore
dotnet remove package Microsoft.AspNetCore.OpenApi
dotnet add package MediatR --version 12.4.1
dotnet remove package MediatR.Extensions.Microsoft.DependencyInjection

# Entity Framework Core package management
dotnet add package Microsoft.EntityFrameworkCore --version 8.0.11
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.11
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.11

# Global dotnet tool setup for migrations execution
dotnet tool install --global dotnet-ef --version 8.0.11

# Schema migration management & update execution
dotnet ef migrations add InitialSchema --output-dir Infrastructure/Migrations
dotnet ef database update

# Force clean project compilation bypassing incremental build engine
dotnet build --no-incremental

```

### Java / Spring Boot 3.x Backend Initialization

```powershell
# Extract project initialized from start.spring.io via PowerShell
Expand-Archive -Path demo.zip -DestinationPath . -Force
Remove-Item demo.zip
Move-Item nexus-engine/* .
Remove-Item nexus-engine

# Package and compile Java application skipping automated test execution
./mvnw.cmd package -DskipTests

```

### Endpoints Integration Verification

```powershell
# Verify Frontend Static Delivery Server
curl http://localhost:3000

# Verify C# OpenAPI/Swagger Engine Interface
curl http://localhost:5000/swagger

# Execute Account Creation POST Request
Invoke-WebRequest -Uri "http://localhost:5000/api/accounts" `
  -Method POST `
  -ContentType "application/json" `
  -Body '{"ownerName": "Mario Rossi", "currency": "EUR"}' `
  -UseBasicParsing

# Execute Funds Deposit POST Request targeting specific Aggregate UUID
Invoke-WebRequest -Uri "http://localhost:5000/api/accounts/d347a974-199c-419d-bc56-a1eaa1e16278/deposit" `
  -Method POST `
  -ContentType "application/json" `
  -Body '{"amount": 500.00}' `
  -UseBasicParsing

```

