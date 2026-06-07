// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Presentation.Controllers
//
// FILE DESCRIPTION:
// Exposes HTTP REST endpoints for Order lifecycle management.
//
// CLASS DOCUMENTATION:
// - OrdersController: Thin HTTP routing boundary. Contains zero business logic.
//   Responsibilities: parse HTTP request, validate input, build Command,
//   dispatch via MediatR, translate result to HTTP response.
// - PlaceOrderRequest: Public HTTP request DTO. Isolates the external API contract
//   from the internal PlaceOrderCommand.
//
// MEMBER DOCUMENTATION:
// - _mediator: Injected IMediator. Single dependency -- decouples Controller from
//   all Handler implementations.
// - PlaceOrder: POST /api/orders. Validates input, dispatches PlaceOrderCommand.
//   Returns HTTP 201 Created with orderId, 400 on invalid input or business rule
//   violation, 404 if account not found, 409 Conflict on concurrent modification.
// - GetOrders: GET /api/orders?accountId={id}. Dispatches GetOrdersQuery.
//   Returns HTTP 200 with list of OrderDto, 400 if accountId is empty.
// - CancelOrder: DELETE /api/orders/{orderId}?accountId={id}. Dispatches
//   CancelOrderCommand. Returns HTTP 204 NoContent on success.
// ============================================================================

namespace NexusEngine.Api.Controllers;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NexusEngine.Api.Application.Orders.Commands.CancelOrder;
using NexusEngine.Api.Application.Orders.Commands.PlaceOrder;
using NexusEngine.Api.Application.Orders.Queries.GetOrders;
using NexusEngine.Api.Infrastructure.Idempotency;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ServiceFilter(typeof(IdempotencyFilter))]
    public async Task<IActionResult> PlaceOrder(
        [FromBody] PlaceOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AccountId == Guid.Empty)
            return BadRequest("AccountId is required.");

        if (string.IsNullOrWhiteSpace(request.Symbol))
            return BadRequest("Symbol is required.");

        if (request.Side != "Buy" && request.Side != "Sell")
            return BadRequest("Side must be 'Buy' or 'Sell'.");

        if (request.Quantity <= 0)
            return BadRequest("Quantity must be positive.");

        if (request.Price <= 0)
            return BadRequest("Price must be positive.");

        var command = new PlaceOrderCommand(
            request.AccountId,
            request.Symbol.ToUpperInvariant(),
            request.Side,
            request.Quantity,
            request.Price
        );

        try
        {
            var orderId = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(
                nameof(PlaceOrder),
                new { id = orderId },
                new { id = orderId }
            );
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
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders(
        [FromQuery] Guid accountId,
        CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty)
            return BadRequest("AccountId is required.");

        var orders = await _mediator.Send(
            new GetOrdersQuery(accountId),
            cancellationToken);

        return Ok(orders);
    }

    [HttpDelete("{orderId:guid}")]
    public async Task<IActionResult> CancelOrder(
        Guid orderId,
        [FromQuery] Guid accountId,
        CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty)
            return BadRequest("AccountId is required.");

        try
        {
            await _mediator.Send(
                new CancelOrderCommand(orderId, accountId),
                cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

public record PlaceOrderRequest(
    Guid AccountId,
    string Symbol,
    string Side,
    decimal Quantity,
    decimal Price
);