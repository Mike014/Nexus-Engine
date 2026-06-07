// FILE DESCRIPTION: MediatR notification handler that broadcasts executed trades
// to all connected SignalR clients via NexusHub.

using MediatR;
using Microsoft.AspNetCore.SignalR;
using NexusEngine.Api.Application.Orders.Notifications;
using NexusEngine.Api.Hubs;

namespace NexusEngine.Api.Infrastructure.Notifications;

// CLASS DOCUMENTATION: Handles TradeExecutedNotification.
// Receives the list of trades from the matching engine and pushes them
// to all connected clients via SignalR event "TradesExecuted".
public class TradeExecutedNotificationHandler
    : INotificationHandler<TradeExecutedNotification>
{
    // MEMBER DOCUMENTATION: SignalR hub context injected by DI.
    private readonly IHubContext<NexusHub> _hubContext;

    public TradeExecutedNotificationHandler(IHubContext<NexusHub> hubContext)
    {
        _hubContext = hubContext;
    }

    // MEMBER DOCUMENTATION: Broadcasts all executed trades to connected clients.
    public async Task Handle(
        TradeExecutedNotification notification,
        CancellationToken cancellationToken)
    {
        var payload = notification.Trades.Select(t => new
        {
            t.BuyOrderId,
            t.SellOrderId,
            t.Price,
            t.Quantity,
            t.ExecutedAt
        });

        await _hubContext.Clients.All.SendAsync(
            "TradesExecuted", payload, cancellationToken);
    }
}
