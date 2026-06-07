// FILE DESCRIPTION: MediatR notification published when an account balance
// or reserved balance changes as a result of order activity.

using MediatR;

namespace NexusEngine.Api.Application.Orders.Notifications;

// CLASS DOCUMENTATION: Published by PlaceOrderHandler and CancelOrderHandler
// after balance mutations. Triggers a balance push to connected clients.
public record BalanceChangedNotification(
    // MEMBER DOCUMENTATION: The account whose balance changed.
    Guid AccountId,
    // MEMBER DOCUMENTATION: New available balance after the operation.
    decimal Balance,
    // MEMBER DOCUMENTATION: New reserved balance after the operation.
    decimal ReservedBalance) : INotification;
