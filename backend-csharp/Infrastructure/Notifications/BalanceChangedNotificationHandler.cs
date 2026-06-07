// FILE DESCRIPTION: MediatR notification handler that broadcasts account balance
// updates to all connected SignalR clients via NexusHub.

using MediatR;
using Microsoft.AspNetCore.SignalR;
using NexusEngine.Api.Application.Orders.Notifications;
using NexusEngine.Api.Hubs;

namespace NexusEngine.Api.Infrastructure.Notifications;

// CLASS DOCUMENTATION: Handles BalanceChangedNotification.
// Pushes the updated balance and reserved balance for a specific account
// to all connected clients via SignalR event "BalanceChanged".
public class BalanceChangedNotificationHandler
    : INotificationHandler<BalanceChangedNotification>
{
    // MEMBER DOCUMENTATION: SignalR hub context injected by DI.
    private readonly IHubContext<NexusHub> _hubContext;

    public BalanceChangedNotificationHandler(IHubContext<NexusHub> hubContext)
    {
        _hubContext = hubContext;
    }

    // MEMBER DOCUMENTATION: Broadcasts balance update to all connected clients.
    public async Task Handle(
        BalanceChangedNotification notification,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            notification.AccountId,
            notification.Balance,
            notification.ReservedBalance
        };

        await _hubContext.Clients.All.SendAsync(
            "BalanceChanged", payload, cancellationToken);
    }
}
