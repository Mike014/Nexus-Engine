// FILE DESCRIPTION: MediatR notification published when one or more trades
// are executed by the order book matching engine.

using MediatR;
using NexusEngine.Domain.OrderBook;

namespace NexusEngine.Api.Application.Orders.Notifications;

// CLASS DOCUMENTATION: Published by PlaceOrderHandler after a successful match.
// Consumed by TradeExecutedNotificationHandler in Infrastructure to broadcast
// via SignalR. Application layer has no knowledge of SignalR.
public record TradeExecutedNotification(
    // MEMBER DOCUMENTATION: The list of trades produced by the matching engine.
    IReadOnlyList<Trade> Trades) : INotification;
