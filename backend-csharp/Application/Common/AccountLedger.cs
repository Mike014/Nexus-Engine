// Appends a domain event to an Account aggregate together with its ledger row,
// bumping the projection's version. Every balance-affecting event carries
// BalanceDelta / ReservedDelta so the account can be rebuilt purely from events.

namespace NexusEngine.Api.Application.Common;

using System.Text.Json;
using NexusEngine.Api.Application.Abstractions;
using NexusEngine.Api.Domain.Entities;

public static class AccountLedger
{
    public static DomainEvent Append(
        INexusUnitOfWork uow,
        Account account,
        string eventType,
        Guid? orderId,
        decimal balanceDelta,
        decimal reservedDelta,
        object details)
    {
        account.Balance += balanceDelta;
        account.ReservedBalance += reservedDelta;
        account.LastEventVersion++;
        account.UpdatedAt = DateTime.UtcNow;

        var payload = JsonSerializer.SerializeToElement(details);
        var dict = payload.EnumerateObject().ToDictionary(p => p.Name, p => (object?)p.Value);
        dict["BalanceDelta"] = balanceDelta;
        dict["ReservedDelta"] = reservedDelta;

        var domainEvent = new DomainEvent
        {
            AggregateId = account.Id,
            AggregateType = "Account",
            EventType = eventType,
            AggregateVersion = account.LastEventVersion,
            Payload = JsonSerializer.Serialize(dict)
        };
        uow.DomainEvents.Add(domainEvent);

        if (orderId is not null)
        {
            uow.Transactions.Add(new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                OrderId = orderId,
                Type = eventType,
                Amount = balanceDelta != 0 ? balanceDelta : -reservedDelta,
                EventId = domainEvent.Id,
                OccurredAt = DateTime.UtcNow
            });
        }

        return domainEvent;
    }
}
