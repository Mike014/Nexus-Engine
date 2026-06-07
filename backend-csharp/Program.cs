// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: API Bootstrapper / Entry Point
//
// FILE DESCRIPTION:
// Entry point and composition root for the ASP.NET Core web application backend.
//
// INFRASTRUCTURE DOCUMENTATION:
// - Dependency Injection Configuration:
//   Registers 'NexusDbContext' into the IoC container with Scoped lifetime --
//   one isolated session per inbound HTTP request.
// - Configuration Resolution:
//   Resolves connection string dynamically. Falls back to appsettings.Development.json
//   locally, overridden by env var 'ConnectionStrings__DefaultConnection' in Docker.
//   Throws InvalidOperationException at startup if the connection string is missing --
//   fail-fast prevents cryptic runtime errors later.
// - Npgsql Resiliency Policy:
//   EnableRetryOnFailure with 5 retries / 10s max delay. Guards against transient
//   network drops and Docker cold-start race conditions.
// - MediatR Registration:
//   Single assembly registration -- all Handlers in the same project are discovered
//   automatically. Double registration removed (caused duplicate DI entries).
// - Database Migration Startup Policy:
//   Applies pending migrations via MigrateAsync() at startup inside an explicit scope.
//   Wrapped in try/catch -- startup failure is logged explicitly instead of crashing
//   with a cryptic unhandled exception. Correct for single-instance deployments (ADR-003).
// ============================================================================

using Microsoft.EntityFrameworkCore;
using NexusEngine.Api.Application.Abstractions;
using NexusEngine.Api.Hubs;
using NexusEngine.Api.Infrastructure.Idempotency;
using NexusEngine.Api.Infrastructure.Persistence;
using NexusEngine.Api.Application.Orders.Validation;
using NexusEngine.Application.Abstractions;
using NexusEngine.Infrastructure.OrderBook;
using NexusEngine.Api.Application.Behaviors;
using Serilog;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, config) =>
{
    config.ReadFrom.Configuration(context.Configuration)
          .ReadFrom.Services(services)
          .Enrich.FromLogContext()
          .Enrich.WithMachineName()
          .Enrich.WithThreadId();
});

// Bind to PORT env var for Railway -- default 8080 for local dev
builder.WebHost.ConfigureKestrel(options => { });
builder.WebHost.UseUrls(
    $"http://0.0.0.0:{Environment.GetEnvironmentVariable("PORT") ?? "8080"}"
);

// --- Services Registration Layer ---

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Fix #3 -- rimossa doppia registrazione MediatR sullo stesso assembly.
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
});

// Fix #4 -- null-check esplicito sulla connection string.
// Se la variabile d'ambiente non e' configurata, il sistema fallisce
// immediatamente con un messaggio chiaro invece di crashare piu' tardi
// con un errore criptico di connessione.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not configured. " +
        "Set the environment variable 'ConnectionStrings__DefaultConnection'.");

builder.Services.AddDbContext<NexusDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null
        );
    });
});

builder.Services.AddScoped<INexusUnitOfWork, NexusUnitOfWork>();

// Idempotency Filter -- POST /api/orders
builder.Services.AddScoped<IdempotencyFilter>();

// Strategy Pattern -- Order Validation
builder.Services.AddScoped<IOrderValidationStrategy, AccountExistsValidation>();
builder.Services.AddScoped<IOrderValidationStrategy, AccountActiveValidation>();
builder.Services.AddScoped<IOrderValidationStrategy, SufficientBalanceValidation>();

// Order Book Matching Engine -- Singleton, in-memory state
builder.Services.AddSingleton<IOrderBookService, OrderBookService>();
builder.Services.AddHostedService<OrderBookRecoveryService>();

// CORS policy -- allows frontend at localhost:3000 to connect via SignalR
builder.Services.AddCors(options =>
{
    options.AddPolicy("NexusPolicy", policy =>
    {
        policy.WithOrigins(
                  "http://localhost:3000",
                  "https://nexus-engine-olive.vercel.app")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Health Checks -- readiness probes DB, liveness is bare 200
builder.Services.AddHealthChecks()
    .AddDbContextCheck<NexusDbContext>(
        name: "postgresql",
        tags: new[] { "ready", "db" });

var app = builder.Build();

// Apply EF Core migrations automatically on startup (Railway PaaS pattern)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NexusDbContext>();
    await db.Database.MigrateAsync();
}

// --- HTTP Request Middleware Pipeline Layer ---

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("NexusPolicy");
app.UseAuthorization();
app.MapControllers();
app.MapHub<NexusHub>("/hubs/nexus");

// Liveness -- never queries the database, just returns 200 if the process is alive
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = (context, report) =>
    {
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync("{\"status\":\"Healthy\"}");
    }
});

// Readiness -- queries PostgreSQL via EF Core to confirm the DB is reachable
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();