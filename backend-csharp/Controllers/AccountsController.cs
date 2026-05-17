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
//   Responsibilities: parse HTTP request, build Command or Query, dispatch via MediatR,
//   translate result to HTTP response.
// - CreateAccountRequest: Public HTTP request DTO. Isolates the external API contract
//   from the internal CreateAccountCommand. Serialization changes do not leak downstream.
// - DepositFundsRequest: Public HTTP request DTO for deposit operations.
//   Isolates the external amount field from the internal DepositFundsCommand.
//
// MEMBER DOCUMENTATION:
// - _mediator: Injected IMediator. Single dependency -- decouples Controller from
//   all Handler implementations.
// - CreateAccount: POST /api/accounts. Builds CreateAccountCommand, dispatches via MediatR.
//   Returns HTTP 201 Created with Location header pointing to the new resource.
// - GetAccount: GET /api/accounts/{id}. Builds GetAccountQuery, dispatches via MediatR.
//   Returns HTTP 200 OK with AccountDto, or HTTP 404 NotFound if account does not exist.
// - DepositFunds: POST /api/accounts/{id}/deposit. Builds DepositFundsCommand, dispatches
//   via MediatR. Returns HTTP 204 NoContent on success, 400 BadRequest if amount is
//   invalid or account is not Active, 404 NotFound if account does not exist.
// ============================================================================

namespace NexusEngine.Api.Controllers;

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NexusEngine.Api.Application.Accounts.Commands.CreateAccount;
using NexusEngine.Api.Application.Accounts.Commands.DepositFunds;
using NexusEngine.Api.Application.Accounts.Queries.GetAccount;

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
        [FromBody] CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateAccountCommand(
            request.OwnerName,
            request.Currency
        );

        var accountId = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetAccount),
            new { id = accountId },
            new { id = accountId }
        );
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

        return NoContent();
    }
}

public record CreateAccountRequest(
    string OwnerName,
    string Currency
);

public record DepositFundsRequest(decimal Amount);