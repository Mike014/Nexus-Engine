// FILE DESCRIPTION: MediatR notification handler that broadcasts the current
// order book snapshot to all connected SignalR clients via NexusHub.

using MediatR;
using Microsoft.AspNetCore.SignalR;
using NexusEngine.Application.Abstractions;
using NexusEngine.Api.Application.Orders.Notifications;
using NexusEngine.Api.Hubs;

namespace NexusEngine.Api.Infrastructure.Notifications;

// CLASS DOCUMENTATION: Handles OrderBookChangedNotification.
// Fetches the current order book snapshot from IOrderBookService
// and pushes it to all connected clients via SignalR event "OrderBookSnapshot".
public class OrderBookChangedNotificationHandler
    : INotificationHandler<OrderBookChangedNotification>
{
    // MEMBER DOCUMENTATION: Order book service to fetch current bids/asks.
    private readonly IOrderBookService _orderBookService;

    // MEMBER DOCUMENTATION: SignalR hub context injected by DI.
    private readonly IHubContext<NexusHub> _hubContext;

    public OrderBookChangedNotificationHandler(
        IOrderBookService orderBookService,
        IHubContext<NexusHub> hubContext)
    {
        _orderBookService = orderBookService;
        _hubContext = hubContext;
    }

    // MEMBER DOCUMENTATION: Fetches current snapshot and broadcasts to clients.
    public async Task Handle(
        OrderBookChangedNotification notification,
        CancellationToken cancellationToken)
    {
        var snapshot = _orderBookService.GetSnapshot();

        await _hubContext.Clients.All.SendAsync(
            "OrderBookSnapshot", snapshot, cancellationToken);
    }
}
