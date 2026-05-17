// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Application.Accounts.Queries.GetAccount
//
// FILE DESCRIPTION:
// Defines the CQRS read-side Query and response DTO for retrieving an Account by ID.
//
// CLASS DOCUMENTATION:
// - GetAccountQuery: Read-only MediatR query. Never modifies state.
//   Returns a nullable AccountDto -- null when the account does not exist.
// - AccountDto: Flat data transfer object returned to the Controller layer.
//   Contains only the fields required by the HTTP response contract.
//   Has no dependency on EF Core tracking infrastructure or domain entities.
//
// MEMBER DOCUMENTATION:
// GetAccountQuery:
// - AccountId: The unique identifier of the account to retrieve.
//
// AccountDto:
// - Id: The unique identifier of the account.
// - OwnerName: The full name of the account owner.
// - Balance: The current available balance, expressed as decimal (never float).
// - ReservedBalance: Funds locked by open orders, unavailable for new operations.
// - Currency: ISO 4217 three-letter currency code (e.g., "EUR").
// - Status: Current lifecycle state of the account ("Active", "Suspended", "Closed").
// - CreatedAt: UTC timestamp of account creation.
// ============================================================================

namespace NexusEngine.Api.Application.Accounts.Queries.GetAccount;

using MediatR;

public record GetAccountQuery(Guid AccountId) : IRequest<AccountDto?>;

public record AccountDto(
    Guid Id,
    string OwnerName,
    decimal Balance,
    decimal ReservedBalance,
    string Currency,
    string Status,
    DateTime CreatedAt
);