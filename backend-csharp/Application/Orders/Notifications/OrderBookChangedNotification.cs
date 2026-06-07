// FILE DESCRIPTION: MediatR notification published when the order book
// state changes (new order placed, order cancelled, order partially filled).

using MediatR;

namespace NexusEngine.Api.Application.Orders.Notifications;

// CLASS DOCUMENTATION: Published by PlaceOrderHandler and CancelOrderHandler
// after any mutation to the order book. Triggers a full snapshot broadcast.
public record OrderBookChangedNotification(
    // MEMBER DOCUMENTATION: The market symbol that changed (e.g. BTC/USD).
    string Symbol) : INotification;
