// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Application.Orders.Commands.CancelOrder
//
// FILE DESCRIPTION:
// Defines the immutable command for cancelling an existing order.
//
// CLASS DOCUMENTATION:
// - CancelOrderCommand: Carries the identifiers required to cancel an order.
//   Immutable by design -- records cannot be modified after construction.
//   MediatR routes this command to CancelOrderHandler.
//
// MEMBER DOCUMENTATION:
// - OrderId: Unique identifier of the order to be cancelled.
// - AccountId: The account that owns the order (ownership verification).
// ============================================================================

namespace NexusEngine.Api.Application.Orders.Commands.CancelOrder;

using MediatR;

public record CancelOrderCommand(
    Guid OrderId,
    Guid AccountId
) : IRequest<Unit>;
