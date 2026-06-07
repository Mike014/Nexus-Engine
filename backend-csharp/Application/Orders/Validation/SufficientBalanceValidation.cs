// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Application.Orders.Validation
//
// FILE DESCRIPTION:
// Validates that the account has sufficient available balance for a Buy order.
//
// CLASS DOCUMENTATION:
// - SufficientBalanceValidation: Computes available balance as
//   balance - reserved_balance. Only applies to Buy orders --
//   Sell orders require asset reservation, out of scope for this phase.
//
// MEMBER DOCUMENTATION:
// - Validate: Throws InvalidOperationException if available balance
//   is less than the required reservation amount.
// ============================================================================

namespace NexusEngine.Api.Application.Orders.Validation;

using NexusEngine.Api.Application.Orders.Commands.PlaceOrder;
using NexusEngine.Api.Domain.Entities;

public class SufficientBalanceValidation : IOrderValidationStrategy
{
    public void Validate(Account? account, PlaceOrderCommand command)
    {
        if (command.Side != "Buy")
            return;

        var reservationAmount = command.Quantity * command.Price;
        var availableBalance = account!.Balance - account.ReservedBalance;

        if (availableBalance < reservationAmount)
            throw new InvalidOperationException(
                $"Insufficient available balance. " +
                $"Available: {availableBalance}, Required: {reservationAmount}.");
    }
}