// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Application.Orders.Queries.GetOrders
//
// FILE DESCRIPTION:
// Implements the MediatR handler for GetOrdersQuery.
//
// CLASS DOCUMENTATION:
// - GetOrdersHandler: Queries the orders projection filtered by AccountId.
//   Uses AsNoTracking() for read-only performance. Returns an empty list
//   when no orders exist.
//
// MEMBER DOCUMENTATION:
// - _uow: Injected INexusUnitOfWork -- single unit of work for atomic persistence.
// - Handle: Filters orders by AccountId, projects to OrderDto, returns
//   empty list if no matching orders found.
// ============================================================================

namespace NexusEngine.Api.Application.Orders.Queries.GetOrders;

using MediatR;
using Microsoft.EntityFrameworkCore;
using NexusEngine.Api.Application.Abstractions;

public class GetOrdersHandler : IRequestHandler<GetOrdersQuery, List<OrderDto>>
{
    private readonly INexusUnitOfWork _uow;

    public GetOrdersHandler(INexusUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<List<OrderDto>> Handle(
        GetOrdersQuery query,
        CancellationToken cancellationToken)
    {
        var orders = await _uow.Orders
            .AsNoTracking()
            .Where(o => o.AccountId == query.AccountId)
            .Select(o => new OrderDto(
                o.Id,
                o.Symbol,
                o.Side,
                o.Quantity,
                o.RemainingQuantity,
                o.Price,
                o.Status,
                o.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return orders;
    }
}
