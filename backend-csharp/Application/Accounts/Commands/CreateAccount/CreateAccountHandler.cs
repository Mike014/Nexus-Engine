// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Application.Accounts.Commands.CreateAccount
//
// FILE DESCRIPTION:
// Implements the MediatR request handler to execute the CreateAccountCommand workflow.
//
// ARCHITECTURAL DOCUMENTATION:
// - MediatR Request Handler Pattern:
//   Implements 'IRequestHandler<CreateAccountCommand, Guid>'. It decouples command 
//   definitions from their business execution engines, eliminating manual DI registrations.
// - Application-Side ID Generation:
//   Instantiates the unique tracking identifier ('Guid.NewGuid()') within the application 
//   layer prior to persistence. This allows the structural alignment of immutable data log
//   entries (Domain Events) and current state representations (Projections) under identical tracking metrics.
// - Event Sourcing / Projection Dual-Write Invariant:
//   Co-ordinates an atomic persistence sequence. It instantiates an unalterable history log 
//   entry ('DomainEvent') encapsulating serialized parameters, alongside a optimized read-side 
//   projection ('Account'). Both records are attached to the unit of work tracking pool and 
//   committed within an implicit transactional boundary upon executing 'SaveChangesAsync()'.
// ============================================================================

namespace NexusEngine.Api.Application.Accounts.Commands.CreateAccount;

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NexusEngine.Api.Domain.Entities;
using NexusEngine.Api.Infrastructure.Persistence;

public class CreateAccountHandler : IRequestHandler<CreateAccountCommand, Guid>
{
    private readonly NexusDbContext _db;

    public CreateAccountHandler(NexusDbContext db)
    {
        _db = db;
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

        _db.DomainEvents.Add(domainEvent);
        _db.Accounts.Add(account);
        
        await _db.SaveChangesAsync(cancellationToken);

        return accountId;
    }
}