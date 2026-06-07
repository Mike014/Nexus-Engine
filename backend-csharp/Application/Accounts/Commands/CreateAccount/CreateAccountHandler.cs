// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Application.Accounts.Commands.CreateAccount
//
// FILE DESCRIPTION:
// Implements the MediatR request handler to execute the CreateAccountCommand workflow.
//
// ARCHITECTURAL DOCUMENTATION:
// - Dependency Rule fix (ADR-006):
//   Depends on INexusUnitOfWork instead of NexusDbContext directly.
//   The Application layer is now fully decoupled from Infrastructure.
// - Event Sourcing / Projection Dual-Write Invariant:
//   DomainEvent and Account projection written in the same transaction
//   via SaveChangesAsync. Atomic -- either both succeed or both fail.
// ============================================================================

namespace NexusEngine.Api.Application.Accounts.Commands.CreateAccount;

using System.Text.Json;
using MediatR;
using NexusEngine.Api.Application.Abstractions;
using NexusEngine.Api.Domain.Entities;

public class CreateAccountHandler : IRequestHandler<CreateAccountCommand, Guid>
{
    private readonly INexusUnitOfWork _uow;

    public CreateAccountHandler(INexusUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Guid> Handle(
        CreateAccountCommand command,
        CancellationToken cancellationToken)
    {
        var accountId = Guid.NewGuid();

        var domainEvent = new DomainEvent
        {
            AggregateId = accountId,
            AggregateType = "Account",
            EventType = "AccountCreated",
            AggregateVersion = 1,
            Payload = JsonSerializer.Serialize(new
            {
                command.OwnerName,
                command.Currency,
                InitialBalance = 0m
            })
        };

        var account = new Account
        {
            Id = accountId,
            OwnerName = command.OwnerName,
            Currency = command.Currency,
            Balance = 0m,
            ReservedBalance = 0m,
            Status = "Active",
            LastEventVersion = 1
        };

        _uow.DomainEvents.Add(domainEvent);
        _uow.Accounts.Add(account);

        await _uow.SaveChangesAsync(cancellationToken);

        return accountId;
    }
}