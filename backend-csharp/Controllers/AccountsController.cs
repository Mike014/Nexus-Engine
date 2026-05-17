// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Presentation.Controllers
//
// FILE DESCRIPTION:
// Exposes HTTP REST endpoints for the routing and lifecycle orchestration of Accounts.
//
// ARCHITECTURAL DOCUMENTATION:
// - Thin Controller Architecture (CQRS/MediatR):
//   Acts strictly as an infrastructure routing boundary. The controller contains 
//   zero domain validation or execution mechanics. Its sole responsibility is parsing 
//   the inbound HTTP network envelope, mapping the parameters to a dispatchable command,
//   and handing execution off asynchronously to the MediatR mediator pipeline.
// - Request DTO Separation Contract:
//   Isolates the external public HTTP contract ('CreateAccountRequest') from internal
//   application messages ('CreateAccountCommand'). This ensures serialization changes or 
//   API version updates do not leak downstream or break internal core domain models.
// - REST Compliance Mechanics:
//   Returns a standard semantic '201 Created' response code via 'CreatedAtAction'. It supplies 
//   the calling client with an immediate operational resource path header ('Location') 
//   referencing the corresponding query route.
// ============================================================================

namespace NexusEngine.Api.Controllers;

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NexusEngine.Api.Application.Accounts.Commands.CreateAccount;

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
    public IActionResult GetAccount(Guid id)
    {
        return Ok();
    }
}

public record CreateAccountRequest(
    string OwnerName,
    string Currency
);