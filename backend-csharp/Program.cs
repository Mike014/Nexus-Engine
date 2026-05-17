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
//   Registers 'NexusDbContext' into the IoC container. By default, it applies a 'Scoped'
//   lifetime—instantiating exactly one isolated session context per inbound HTTP request.
//   This isolates data persistence workflows across unique operational worker contexts.
// - Configuration Resolution:
//   Resolves connection strings dynamically. In local development environments, it falls 
//   back to 'appsettings.Development.json'. In Docker container environments, it overrides via 
//   the environment variable 'ConnectionStrings__DefaultConnection' (double underscore matching hierarchy).
// - Npgsql Resiliency Policy:
//   Appends '.EnableRetryOnFailure()' behavior with a maximum cap of 5 retries spaced up to 
//   10 seconds apart. This guards against transient network drops and prevents startup crashes 
//   in Docker compositions where the API application initializes faster than the database engine instance.
// - Database Migration Startup Policy:
//   Initializes an explicit IoC scope block upon runtime startup to apply pending migrations
//   automatically via 'db.Database.Migrate()'. This approach is highly practical for single-instance
//   deployments (per ADR-003) to ensure immediate structural schema synchronization.
// ============================================================================

using System;
using Microsoft.EntityFrameworkCore;
using NexusEngine.Api.Infrastructure.Persistence;
using NexusEngine.Api.Application.Accounts.Commands.CreateAccount;

var builder = WebApplication.CreateBuilder(args);

// --- Services Registration Layer ---

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly)
       .RegisterServicesFromAssemblyContaining<CreateAccountHandler>());

builder.Services.AddDbContext<NexusDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null
            );
        }
    );
});

var app = builder.Build();

// --- Startup Execution Initialization Layer ---

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NexusDbContext>();
    db.Database.Migrate();
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