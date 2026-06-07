// FILE DESCRIPTION: SignalR hub for Nexus Engine real-time communication.
// Clients connect here to receive live updates on trades, order book, and balances.

using Microsoft.AspNetCore.SignalR;

namespace NexusEngine.Api.Hubs;

// CLASS DOCUMENTATION: NexusHub is the central SignalR hub.
// All push notifications (trades, order book snapshots, balance updates)
// are broadcast from INotificationHandlers via IHubContext<NexusHub>.
// No business logic lives here.
public class NexusHub : Hub
{
    // MEMBER DOCUMENTATION: Called automatically by SignalR when a client connects.
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    // MEMBER DOCUMENTATION: Called automatically by SignalR when a client disconnects.
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
