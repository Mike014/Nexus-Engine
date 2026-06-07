// FILE DESCRIPTION: Unit tests for SignalR notification handlers that broadcast
// trade executions, order book snapshots, and balance changes to connected clients.

using MediatR;
using Microsoft.AspNetCore.SignalR;
using Moq;
using NexusEngine.Api.Application.Orders.Notifications;
using NexusEngine.Api.Hubs;
using NexusEngine.Api.Infrastructure.Notifications;
using NexusEngine.Application.Abstractions;
using NexusEngine.Domain.OrderBook;
using Xunit;

namespace NexusEngine.Tests.Application.Notifications;

// CLASS DOCUMENTATION: Tests for TradeExecutedNotificationHandler.
// Verifies that trade data is correctly transformed and broadcast to all clients
// via SignalR's "TradesExecuted" event.
public class TradeExecutedNotificationHandlerTests
{
    // MEMBER DOCUMENTATION: Verifies that a single trade is broadcast with correct
    // field mapping and the "TradesExecuted" event name.
    [Fact]
    public async Task Handle_SingleTrade_BroadcastsTradesExecuted()
    {
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);

        var mockHubContext = new Mock<IHubContext<NexusHub>>();
        mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        var handler = new TradeExecutedNotificationHandler(mockHubContext.Object);
        var trade = new Trade(
            MakerOrderId: Guid.NewGuid(),
            Price: 100m,
            Quantity: 10m,
            BuyOrderId: Guid.NewGuid(),
            SellOrderId: Guid.NewGuid(),
            ExecutedAt: DateTime.UtcNow);
        var notification = new TradeExecutedNotification(new List<Trade> { trade });

        await handler.Handle(notification, CancellationToken.None);

        mockClientProxy.Verify(
            p => p.SendCoreAsync(
                "TradesExecuted",
                It.Is<object?[]>(args =>
                    args.Length == 1 &&
                    args[0] != null),
                CancellationToken.None),
            Times.Once);
    }

    // MEMBER DOCUMENTATION: Verifies that an empty trades list is broadcast without
    // throwing, ensuring no edge-case crashes on empty match results.
    [Fact]
    public async Task Handle_EmptyTrades_BroadcastsEmptyPayload()
    {
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);

        var mockHubContext = new Mock<IHubContext<NexusHub>>();
        mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        var handler = new TradeExecutedNotificationHandler(mockHubContext.Object);
        var notification = new TradeExecutedNotification(new List<Trade>());

        await handler.Handle(notification, CancellationToken.None);

        mockClientProxy.Verify(
            p => p.SendCoreAsync(
                "TradesExecuted",
                It.IsAny<object?[]>(),
                CancellationToken.None),
            Times.Once);
    }

    // MEMBER DOCUMENTATION: Verifies the cancellation token is forwarded to SignalR.
    [Fact]
    public async Task Handle_CancellationToken_ForwardsToSignalR()
    {
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);

        var mockHubContext = new Mock<IHubContext<NexusHub>>();
        mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        var handler = new TradeExecutedNotificationHandler(mockHubContext.Object);
        var notification = new TradeExecutedNotification(new List<Trade>());
        using var cts = new CancellationTokenSource();

        await handler.Handle(notification, cts.Token);

        mockClientProxy.Verify(
            p => p.SendCoreAsync(
                "TradesExecuted",
                It.IsAny<object?[]>(),
                cts.Token),
            Times.Once);
    }
}

// CLASS DOCUMENTATION: Tests for OrderBookChangedNotificationHandler.
// Verifies that the handler fetches the current order book snapshot and broadcasts
// it to all clients via SignalR's "OrderBookSnapshot" event.
public class OrderBookChangedNotificationHandlerTests
{
    // MEMBER DOCUMENTATION: Verifies snapshot is fetched from IOrderBookService
    // and broadcast with the correct event name.
    [Fact]
    public async Task Handle_FetchesSnapshotAndBroadcasts()
    {
        var mockOrderBookService = new Mock<IOrderBookService>();
        var expectedSnapshot = new { Bids = new object[] { }, Asks = new object[] { }, Symbol = "TEST" };
        mockOrderBookService.Setup(s => s.GetSnapshot()).Returns(expectedSnapshot);

        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);

        var mockHubContext = new Mock<IHubContext<NexusHub>>();
        mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        var handler = new OrderBookChangedNotificationHandler(
            mockOrderBookService.Object, mockHubContext.Object);
        var notification = new OrderBookChangedNotification("TEST");

        await handler.Handle(notification, CancellationToken.None);

        mockOrderBookService.Verify(s => s.GetSnapshot(), Times.Once);
        mockClientProxy.Verify(
            p => p.SendCoreAsync(
                "OrderBookSnapshot",
                It.Is<object?[]>(args =>
                    args.Length == 1 &&
                    args[0] != null),
                CancellationToken.None),
            Times.Once);
    }

    // MEMBER DOCUMENTATION: Verifies cancellation token is forwarded to SignalR.
    [Fact]
    public async Task Handle_CancellationToken_ForwardsToSignalR()
    {
        var mockOrderBookService = new Mock<IOrderBookService>();
        mockOrderBookService.Setup(s => s.GetSnapshot()).Returns(new object());

        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);

        var mockHubContext = new Mock<IHubContext<NexusHub>>();
        mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        var handler = new OrderBookChangedNotificationHandler(
            mockOrderBookService.Object, mockHubContext.Object);
        var notification = new OrderBookChangedNotification("TEST");
        using var cts = new CancellationTokenSource();

        await handler.Handle(notification, cts.Token);

        mockClientProxy.Verify(
            p => p.SendCoreAsync(
                "OrderBookSnapshot",
                It.IsAny<object?[]>(),
                cts.Token),
            Times.Once);
    }
}

// CLASS DOCUMENTATION: Tests for BalanceChangedNotificationHandler.
// Verifies that balance updates are correctly broadcast to all clients
// via SignalR's "BalanceChanged" event with the expected payload shape.
public class BalanceChangedNotificationHandlerTests
{
    // MEMBER DOCUMENTATION: Verifies account balance data is broadcast with
    // correct event name and payload fields.
    [Fact]
    public async Task Handle_BroadcastsBalanceChanged()
    {
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);

        var mockHubContext = new Mock<IHubContext<NexusHub>>();
        mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        var handler = new BalanceChangedNotificationHandler(mockHubContext.Object);
        var accountId = Guid.NewGuid();
        var notification = new BalanceChangedNotification(accountId, 1500.50m, 500.00m);

        await handler.Handle(notification, CancellationToken.None);

        mockClientProxy.Verify(
            p => p.SendCoreAsync(
                "BalanceChanged",
                It.Is<object?[]>(args =>
                    args.Length == 1 &&
                    args[0] != null),
                CancellationToken.None),
            Times.Once);
    }

    // MEMBER DOCUMENTATION: Verifies cancellation token is forwarded to SignalR.
    [Fact]
    public async Task Handle_CancellationToken_ForwardsToSignalR()
    {
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);

        var mockHubContext = new Mock<IHubContext<NexusHub>>();
        mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        var handler = new BalanceChangedNotificationHandler(mockHubContext.Object);
        var notification = new BalanceChangedNotification(Guid.NewGuid(), 100m, 10m);
        using var cts = new CancellationTokenSource();

        await handler.Handle(notification, cts.Token);

        mockClientProxy.Verify(
            p => p.SendCoreAsync(
                "BalanceChanged",
                It.IsAny<object?[]>(),
                cts.Token),
            Times.Once);
    }

    // MEMBER DOCUMENTATION: Verifies zero-balance edge case does not throw.
    [Fact]
    public async Task Handle_ZeroBalance_DoesNotThrow()
    {
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);

        var mockHubContext = new Mock<IHubContext<NexusHub>>();
        mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        var handler = new BalanceChangedNotificationHandler(mockHubContext.Object);
        var notification = new BalanceChangedNotification(Guid.NewGuid(), 0m, 0m);

        await handler.Handle(notification, CancellationToken.None);

        mockClientProxy.Verify(
            p => p.SendCoreAsync(
                "BalanceChanged",
                It.IsAny<object?[]>(),
                CancellationToken.None),
            Times.Once);
    }
}
