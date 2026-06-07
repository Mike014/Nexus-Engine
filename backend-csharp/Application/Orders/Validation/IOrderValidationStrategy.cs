// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Application.Orders.Validation
//
// FILE DESCRIPTION:
// Defines the contract for order validation strategies.
//
// CLASS DOCUMENTATION:
// - IOrderValidationStrategy: Each implementation encapsulates a single
//   validation rule. The Handler runs all strategies in sequence via
//   IEnumerable<IOrderValidationStrategy> -- open to extension, closed to
//   modification (Open/Closed Principle).
//
// MEMBER DOCUMENTATION:
// - Validate: Receives the loaded account and the incoming command.
//   Throws an exception if the validation rule is violated.
//   Returns void -- success is implicit, failure is explicit via exception.
// ============================================================================

namespace NexusEngine.Api.Application.Orders.Validation;

using NexusEngine.Api.Application.Orders.Commands.PlaceOrder;
using NexusEngine.Api.Domain.Entities;

public interface IOrderValidationStrategy
{
    void Validate(Account? account, PlaceOrderCommand command);
}
