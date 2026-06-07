// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Application.Orders.Validation
//
// FILE DESCRIPTION:
// Validates that the account is in Active status.
//
// CLASS DOCUMENTATION:
// - AccountActiveValidation: Ensures suspended or closed accounts
//   cannot place orders. Status check is a hard business rule --
//   no order can be accepted on a non-Active account regardless of balance.
//
// MEMBER DOCUMENTATION:
// - Validate: Throws InvalidOperationException if account status is not Active.
// ============================================================================

namespace NexusEngine.Api.Application.Orders.Validation;

using NexusEngine.Api.Application.Orders.Commands.PlaceOrder;
using NexusEngine.Api.Domain.Entities;

public class AccountActiveValidation : IOrderValidationStrategy
{
    public void Validate(Account? account, PlaceOrderCommand command)
    {
        if (account!.Status != "Active")
            throw new InvalidOperationException(
                $"Account {command.AccountId} is not Active.");
    }
}