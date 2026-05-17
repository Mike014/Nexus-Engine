// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Application.Accounts.Commands.DepositFunds
//
// FILE DESCRIPTION:
// Defines the CQRS Command for depositing funds into an existing Account.
//
// CLASS DOCUMENTATION:
// - DepositFundsCommand: Immutable command representing the intent to credit
//   an amount to an existing account. Never modifies state itself.
//
// MEMBER DOCUMENTATION:
// - AccountId: The unique identifier of the target account.
// - Amount: The amount to deposit. Must be positive -- validated at controller level.
// ============================================================================

namespace NexusEngine.Api.Application.Accounts.Commands.DepositFunds;

using MediatR;

public record DepositFundsCommand(
    Guid AccountId,
    decimal Amount
) : IRequest<Unit>;