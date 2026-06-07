# Phase 5 -- Observability and Deploy

## Overview

Phase 5 brought Nexus Engine to public production with production-grade observability. The backend runs on Railway with managed PostgreSQL, the frontend on Vercel. The system is publicly accessible via permanent URLs.

---

## Public URLs

| Service | URL |
|---------|-----|
| Frontend | https://nexus-engine-olive.vercel.app |
| Backend API | https://nexus-engine-production-60c8.up.railway.app |
| Swagger UI | https://nexus-engine-production-60c8.up.railway.app/swagger |
| Health Live | https://nexus-engine-production-60c8.up.railway.app/health/live |
| Health Ready | https://nexus-engine-production-60c8.up.railway.app/health/ready |

---

## Task 1 -- MediatR Pipeline Behavior

**File created:** `backend-csharp/Application/Behaviors/LoggingBehavior.cs`

Implemented `IPipelineBehavior<TRequest, TResponse>` as a cross-cutting interceptor in the MediatR pipeline. Every command and query passes through the behavior before and after the handler.

Features:
- Logs request name and UTC timestamp on entry
- Measures elapsed time with `System.Diagnostics.Stopwatch`
- Logs elapsed milliseconds on success
- Catches any exception, logs with structured context, rethrows -- no exception is swallowed

Registration in `Program.cs`:
```csharp
cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
```

**Architectural decision:** the behavior always rethrows because it has no knowledge of the HTTP contract. Translation to 404/400/409 belongs to the controller layer, not the CQRS pipeline.

---

## Task 2 -- Serilog Structured Logging

**Files modified:** `NexusEngine.Api.csproj`, `Program.cs`, `appsettings.json`, `appsettings.Development.json`

**NuGet packages added:**
- `Serilog.AspNetCore 10.0.0`
- `Serilog.Sinks.Console 6.1.1`
- `Serilog.Sinks.File 7.0.0`
- `Serilog.Enrichers.Environment 3.0.1`
- `Serilog.Enrichers.Thread 4.0.0`

**Configuration in `Program.cs`:**
```csharp
builder.Host.UseSerilog((context, services, config) =>
    config.ReadFrom.Configuration(context.Configuration)
          .ReadFrom.Services(services)
          .Enrich.FromLogContext()
          .Enrich.WithMachineName()
          .Enrich.WithThreadId());
```

**Configuration strategy:**
- Zero hardcoding in code -- everything in `appsettings.json`
- Production: `MinimumLevel Information`, Console + File sink, rolling daily, 7-file retention
- Development: `MinimumLevel Debug`, more verbose Microsoft override
- Log files written to `logs/nexus-.log` (folder in `.gitignore`)

---

## Task 3 -- Health Checks

**Files modified:** `NexusEngine.Api.csproj`, `Program.cs`

**NuGet packages added:**
- `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore 8.0.1`
- `AspNetCore.HealthChecks.UI.Client 9.0.0`

**Endpoints exposed:**

| Endpoint | Behavior |
|----------|----------|
| `GET /health/live` | Liveness -- always returns Healthy if the process is alive, never queries the database |
| `GET /health/ready` | Readiness -- runs `CanConnectAsync` on PostgreSQL via EF Core, returns full JSON report |

**Liveness vs readiness distinction:** liveness failure triggers a container restart on Railway. Readiness failure stops traffic routing without restarting. The two signals have different semantics and must not be collapsed into one.

---

## Task 4 -- Backend Deploy on Railway

**Files created/modified:** `railway.toml`, `backend-csharp/Dockerfile`, `backend-csharp/Program.cs`

**railway.toml (repo root):**
```toml
[build]
dockerfilePath = "backend-csharp/Dockerfile"

[deploy]
healthcheckPath = "/health/live"
healthcheckTimeout = 30
restartPolicyType = "on_failure"
restartPolicyMaxRetries = 3
```

**Environment variables configured manually on Railway:**
```
ASPNETCORE_ENVIRONMENT = Production
ConnectionStrings__DefaultConnection = Host=${{Postgres.PGHOST}};Port=${{Postgres.PGPORT}};Database=${{Postgres.PGDATABASE}};Username=${{Postgres.PGUSER}};Password=${{Postgres.PGPASSWORD}};SSL Mode=Require;Trust Server Certificate=true
```

**Automatic migrations on startup** (Railway runtime image has no SDK):
```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NexusDbContext>();
    await db.Database.MigrateAsync();
}
```

**Issues encountered and resolved:**

1. **Build context mismatch** -- Railway uses the repo root as Docker build context. Dockerfile `COPY` paths assumed `backend-csharp/` as context. Fixed by prefixing all `COPY` instructions with `backend-csharp/`.

2. **Incompatible connection string format** -- `${{Postgres.DATABASE_URL}}` generates a URI format (`postgresql://...`) incompatible with Npgsql ADO.NET. Fixed by using individual `PGHOST`, `PGPORT`, `PGUSER`, `PGPASSWORD`, `PGDATABASE` variables.

3. **Healthcheck failure** -- conflict between `ENV ASPNETCORE_URLS` in the Dockerfile and Railway's `PORT` variable. Fixed with `ENV ASPNETCORE_URLS=http://+:8080` in the Dockerfile and port 8080 configured manually in Railway networking settings.

4. **Migrations not applied** -- the runtime container has no dotnet SDK, so `dotnet ef database update` is not available. Fixed with `MigrateAsync()` on startup, the standard pattern for PaaS deployments.

---

## Task 5 -- Frontend Deploy on Vercel

**Files modified:** `frontend/src/hooks/useNexusHub.ts`, `frontend/Dockerfile` renamed to `frontend/Dockerfile.local`

**Vercel configuration (manual):**
- Root Directory: `frontend`
- Framework: Vite (auto-detected)
- Environment variable: `VITE_API_URL = https://nexus-engine-production-60c8.up.railway.app`

**Issues encountered and resolved:**

1. **Dockerfile interference** -- Vercel detected `frontend/Dockerfile` and used it instead of its native Vite builder. `VITE_*` variables are injected only by the native builder at build time. Fixed by renaming the Dockerfile to `Dockerfile.local`.

2. **Hardcoded HUB_URL** -- `useNexusHub.ts` had the SignalR URL hardcoded to `localhost:5000` regardless of the environment variable. Fixed:
```typescript
const HUB_URL = `${import.meta.env.VITE_API_URL ?? "http://localhost:5000"}/hubs/nexus`
```

3. **CORS policy** -- the backend only allowed localhost origins. Added the Vercel frontend URL to the CORS policy in `Program.cs`. `AllowCredentials()` preserved -- mandatory for SignalR WebSocket handshake.

---

## Key Commits

```
feat: Phase 5 - LoggingBehavior, Serilog, Health Checks
feat: Railway deployment config - Dockerfile PORT binding, railway.toml
fix: Railway build context -- use repo root as Docker context
feat: auto-apply EF Core migrations on startup for Railway deploy
feat: enable Swagger UI in Production for portfolio showcase
feat: prepare frontend for Railway deploy
fix: hide Dockerfile from Vercel -- use native Vite builder
fix: read SignalR HUB_URL from VITE_API_URL env var
fix: add Vercel origin to CORS policy for production
```

---

## Final Checklist

- [x] MediatR Pipeline Behavior for global exception handling
- [x] Structured logging (Serilog)
- [x] Health checks (`/health/live` and `/health/ready`)
- [x] Deploy to Railway (PostgreSQL + ASP.NET Core 9 backend)
- [x] Deploy to Vercel (React/Vite frontend)
- [x] Public URLs for portfolio showcase
- [ ] Metrics with Prometheus + Grafana (optional -- not implemented)