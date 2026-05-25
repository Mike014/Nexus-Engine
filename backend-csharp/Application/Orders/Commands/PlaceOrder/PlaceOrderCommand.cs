// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Application.Orders.Commands.PlaceOrder
//
// FILE DESCRIPTION:
// Defines the immutable command for placing a new order in the system.
//
// CLASS DOCUMENTATION:
// - PlaceOrderCommand: Carries all data required to place a limit order.
//   Immutable by design -- records cannot be modified after construction.
//   MediatR routes this command to PlaceOrderHandler.
//
// MEMBER DOCUMENTATION:
// - AccountId: The account placing the order.
// - Symbol:    The traded instrument (e.g. "BTC-EUR", "TEAM-A-WIN").
// - Side:      Buy or Sell -- determines which side of the order book.
// - Quantity:  Number of units to trade. Must be greater than zero.
// - Price:     Limit price per unit. Must be greater than zero.
// ============================================================================

namespace NexusEngine.Api.Application.Orders.Commands.PlaceOrder;

using MediatR;

public record PlaceOrderCommand(
    Guid AccountId,
    string Symbol,
    string Side,
    decimal Quantity,
    decimal Price
) : IRequest<Guid>;