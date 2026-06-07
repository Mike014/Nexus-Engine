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
using NexusEngine.Api.Infrastructure.Idempotency;
using NexusEngine.Api.Infrastructure.Persistence;
using NexusEngine.Api.Application.Orders.Validation;

var builder = WebApplication.CreateBuilder(args);

// --- Services Registration Layer ---

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Fix #3 -- rimossa doppia registrazione MediatR sullo stesso assembly.
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

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

var app = builder.Build();

// --- Startup Migration Layer ---

// Fix #10 -- MigrateAsync invece di Migrate, wrappato in try/catch.
// Se la migration fallisce, il processo si arresta con un messaggio
// esplicito invece di propagare un'eccezione non gestita.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NexusDbContext>();
    try
    {
        await db.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider
            .GetRequiredService<ILogger<Program>>();
        logger.LogCritical(ex,
            "Database migration failed at startup. Application cannot start.");
        throw;
    }
}

// --- HTTP Request Middleware Pipeline Layer ---

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();