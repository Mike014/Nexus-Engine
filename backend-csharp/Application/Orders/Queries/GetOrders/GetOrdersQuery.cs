// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Application.Orders.Queries.GetOrders
//
// FILE DESCRIPTION:
// MediatR query to retrieve all orders for a given account.
//
// MEMBER DOCUMENTATION:
// - AccountId: The account whose orders should be returned.
// ============================================================================

namespace NexusEngine.Api.Application.Orders.Queries.GetOrders;

using MediatR;

public record GetOrdersQuery(Guid AccountId) : IRequest<List<OrderDto>>;
