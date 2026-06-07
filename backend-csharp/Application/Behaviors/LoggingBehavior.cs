// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Application.Behaviors
//
// FILE DESCRIPTION:
// Implements a cross-cutting MediatR pipeline behavior that provides structured
// logging for every dispatched request/command/query in the system.
//
// CLASS DOCUMENTATION:
// - Pipeline Cross-Cutting Concern (Decorator Pattern):
//   Wraps all MediatR request handlers with pre/post-invocation logging and
//   exception capture. Registered as an open generic in the DI container so
//   every IRequest<TResponse> automatically flows through this behavior.
// - Structured Logging:
//   Uses ILogger<T> for all output. Entry log includes request type and UTC
//   timestamp. Success log includes elapsed milliseconds. Exception log
//   captures the exception object itself alongside request name and message
//   so Serilog (or any structured sink) can index the named properties.
// - Rethrow Policy:
//   Exceptions are never swallowed. After logging, the original exception is
//   rethrown so the caller / middleware receives the same error unchanged.
//
// MEMBER DOCUMENTATION:
// - Handle(...):
//   Logs entry with request name + UtcNow, starts a Stopwatch, awaits next(),
//   logs success with elapsed ms, catches/logs/rethrows on failure.
// ============================================================================

namespace NexusEngine.Api.Application.Behaviors;

using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation(
            "Handling {RequestName} at {Timestamp}",
            requestName,
            DateTime.UtcNow);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();

            stopwatch.Stop();

            _logger.LogInformation(
                "Handled {RequestName} in {ElapsedMs} ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Request {RequestName} failed after {ElapsedMs} ms: {ErrorMessage}",
                requestName,
                stopwatch.ElapsedMilliseconds,
                ex.Message);

            throw;
        }
    }
}
