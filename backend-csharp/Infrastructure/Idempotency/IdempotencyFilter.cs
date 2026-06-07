// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Infrastructure.Idempotency
//
// FILE DESCRIPTION:
// ASP.NET Core Action Filter for idempotent POST endpoint handling.
// Uses the idempotency_keys table to store and retrieve responses keyed by
// the X-Idempotency-Key request header.
//
// CLASS DOCUMENTATION:
// - IdempotencyFilter: Implements IAsyncActionFilter to provide idempotency
//   guarantees for POST /api/orders. Reads the X-Idempotency-Key header before
//   action execution. If a cached response exists, short-circuits with HTTP 201
//   and the stored body. Otherwise executes the action and atomically persists
//   the response keyed by the idempotency key.
//
// MEMBER DOCUMENTATION:
// - _uow: Injected INexusUnitOfWork -- used to query and persist
//   IdempotencyKey records in the idempotency_keys table.
// - OnActionExecutionAsync: Core filter method. If the header is absent the
//   action runs normally. If present and cached, returns the stored response.
//   If present but uncached, runs the action then stores the result.
// ============================================================================

namespace NexusEngine.Api.Infrastructure.Idempotency;

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using NexusEngine.Api.Application.Abstractions;
using NexusEngine.Api.Domain.Entities;

public class IdempotencyFilter : IAsyncActionFilter
{
    private readonly INexusUnitOfWork _uow;

    public IdempotencyFilter(INexusUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers
                .TryGetValue("X-Idempotency-Key", out var idempotencyKeyValues))
        {
            await next();
            return;
        }

        var idempotencyKey = idempotencyKeyValues.ToString();

        var existing = await _uow.IdempotencyKeys
            .FirstOrDefaultAsync(k => k.Key == idempotencyKey,
                context.HttpContext.RequestAborted);

        if (existing is not null)
        {
            context.Result = new ContentResult
            {
                StatusCode = existing.ResponseStatus,
                Content = existing.ResponseBody,
                ContentType = "application/json"
            };
            return;
        }

        var executed = await next();

        if (executed.Result is ObjectResult { Value: not null } objectResult)
        {
            var responseBody = JsonSerializer.Serialize(objectResult.Value);
            var responseStatus = objectResult.StatusCode ?? 201;

            _uow.IdempotencyKeys.Add(new IdempotencyKey
            {
                Key = idempotencyKey,
                ResponseBody = responseBody,
                ResponseStatus = responseStatus,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow
            });

            await _uow.SaveChangesAsync(context.HttpContext.RequestAborted);
        }
    }
}
