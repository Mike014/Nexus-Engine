// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Presentation.Controllers
//
// FILE DESCRIPTION:
// Exposes HTTP REST endpoints for Account lifecycle management.
//
// CLASS DOCUMENTATION:
// - AccountsController: Thin HTTP routing boundary. Contains zero business logic.
//   Responsibilities: parse HTTP request, validate input, build Command or Query,
//   dispatch via MediatR, translate result to HTTP response.
// - DepositFundsRequest: Public HTTP request DTO for deposit operations.
//   Isolates the external amount field from the internal DepositFundsCommand.
//
// MEMBER DOCUMENTATION:
// - _mediator: Injected IMediator. Single dependency -- decouples Controller from
//   all Handler implementations.
// - CreateAccount: POST /api/accounts. Validates OwnerName and Currency before
//   dispatching CreateAccountCommand. Returns HTTP 201 Created with Location header,
//   400 BadRequest on invalid input, 500 on unexpected database error.
// - GetAccount: GET /api/accounts/{id}. Builds GetAccountQuery, dispatches via MediatR.
//   Returns HTTP 200 OK with AccountDto, or HTTP 404 NotFound if account does not exist.
// - DepositFunds: POST /api/accounts/{id}/deposit. Validates amount > 0, dispatches
//   DepositFundsCommand. Returns HTTP 204 NoContent on success, 400 on invalid input
//   or inactive account, 404 if not found, 409 Conflict on concurrent modification.
// - ReplayAccount: GET /api/accounts/{id}/replay. Reconstructs Account state
//   exclusively from domain_events. Never reads from the accounts projection.
//   Returns ReplayAccountDto with EventsReplayed count, or HTTP 404 if not found.
// ============================================================================

namespace NexusEngine.Api.Controllers;

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NexusEngine.Api.Application.Accounts.Commands.CreateAccount;
using NexusEngine.Api.Application.Accounts.Commands.DepositFunds;
using NexusEngine.Api.Application.Accounts.Queries.GetAccount;
using NexusEngine.Api.Application.Accounts.Queries.ReplayAccount;

[ApiController]
[Route("api/accounts")]
public class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAccount(
        CancellationToken cancellationToken)
    {
        var command = new CreateAccountCommand("Default", "USD");

        try
        {
            var accountId = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(
                nameof(GetAccount),
                new { id = accountId },
                new { id = accountId }
            );
        }
        catch (DbUpdateException)
        {
            return StatusCode(500, "A database error occurred while creating the account.");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAccount(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetAccountQuery(id);
        var account = await _mediator.Send(query, cancellationToken);

        if (account is null)
            return NotFound();

        return Ok(account);
    }

    [HttpPost("{id:guid}/deposit")]
    public async Task<IActionResult> DepositFunds(
        Guid id,
        [FromBody] DepositFundsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            return BadRequest("Amount must be positive.");

        var command = new DepositFundsCommand(id, request.Amount);

        try
        {
            await _mediator.Send(command, cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            return Conflict("Concurrent modification detected. Please retry.");
        }

        return NoContent();
    }

    [HttpGet("{id:guid}/replay")]
    public async Task<IActionResult> ReplayAccount(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new ReplayAccountQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }
}

public record DepositFundsRequest(decimal Amount);