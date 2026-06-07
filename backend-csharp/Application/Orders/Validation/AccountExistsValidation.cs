// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Application.Orders.Validation
//
// FILE DESCRIPTION:
// Validates that the account referenced by the command exists.
//
// CLASS DOCUMENTATION:
// - AccountExistsValidation: First validation in the chain.
//   If the account is null, all subsequent validations would throw
//   NullReferenceException -- this guard makes the failure explicit and clean.
//
// MEMBER DOCUMENTATION:
// - Validate: Throws KeyNotFoundException if account is null.
// ============================================================================

namespace NexusEngine.Api.Application.Orders.Validation;

using NexusEngine.Api.Application.Orders.Commands.PlaceOrder;
using NexusEngine.Api.Domain.Entities;

public class AccountExistsValidation : IOrderValidationStrategy
{
    public void Validate(Account? account, PlaceOrderCommand command)
    {
        if (account is null)
            throw new KeyNotFoundException(
                $"Account {command.AccountId} not found.");
    }
}