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

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using NexusEngine.Api.Application.Abstractions;
using NexusEngine.Api.Application.Common;
using NexusEngine.Api.Domain.Entities;

// The key is claimed atomically BEFORE the action runs by inserting a placeholder
// row (ResponseStatus = 0); the primary key makes concurrent duplicates fail.
// A duplicate arriving while the first request is still running gets 409; a
// duplicate arriving afterwards gets the stored response. Failed requests release
// the key so the client can retry. Keys are scoped to method + path.
public class IdempotencyFilter : IAsyncActionFilter
{
    private const int InProgress = 0;

    private readonly INexusUnitOfWork _uow;

    public IdempotencyFilter(INexusUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        if (!request.Headers.TryGetValue("X-Idempotency-Key", out var values)
            || string.IsNullOrWhiteSpace(values.ToString()))
        {
            await next();
            return;
        }

        var ct = context.HttpContext.RequestAborted;
        var key = Scope(request.Method, request.Path, values.ToString());

        if (!await TryClaim(key, ct))
        {
            var existing = await _uow.IdempotencyKeys.AsNoTracking()
                .FirstOrDefaultAsync(k => k.Key == key, ct);

            context.Result = existing is null || existing.ResponseStatus == InProgress
                ? new ConflictObjectResult("A request with this idempotency key is already in progress.")
                : new ContentResult
                {
                    StatusCode = existing.ResponseStatus,
                    Content = existing.ResponseBody,
                    ContentType = "application/json"
                };
            return;
        }

        var executed = await next();

        _uow.DiscardChanges();
        var row = await _uow.IdempotencyKeys.FirstOrDefaultAsync(k => k.Key == key, CancellationToken.None);
        if (row is null)
            return;

        var status = (executed.Result as ObjectResult)?.StatusCode
            ?? (executed.Result as StatusCodeResult)?.StatusCode;

        if (executed.Exception is null
            && executed.Result is ObjectResult { Value: not null } result
            && status is >= 200 and < 300)
        {
            row.ResponseBody = JsonSerializer.Serialize(result.Value);
            row.ResponseStatus = status.Value;
        }
        else
        {
            _uow.IdempotencyKeys.Remove(row);
        }

        await _uow.SaveChangesAsync(CancellationToken.None);
    }

    private async Task<bool> TryClaim(string key, CancellationToken ct)
    {
        var expired = await _uow.IdempotencyKeys
            .FirstOrDefaultAsync(k => k.Key == key && k.ExpiresAt < DateTime.UtcNow, ct);
        if (expired is not null)
        {
            _uow.IdempotencyKeys.Remove(expired);
            await _uow.SaveChangesAsync(ct);
        }

        _uow.IdempotencyKeys.Add(new IdempotencyKey
        {
            Key = key,
            ResponseStatus = InProgress,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow
        });

        try
        {
            await _uow.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (OptimisticConcurrencyHelper.IsUniqueConstraintViolation(ex))
        {
            _uow.DiscardChanges();
            return false;
        }
    }

    private static string Scope(string method, string path, string clientKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{method}:{path}:{clientKey}"));
        return Convert.ToHexString(bytes);
    }
}
