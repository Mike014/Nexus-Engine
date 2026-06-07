// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Infrastructure.OrderBook
//
// FILE DESCRIPTION:
// Implements a hosted service that reloads active orders into the in-memory
// order book at application startup, ensuring continuity after a restart.
//
// CLASS DOCUMENTATION:
// - OrderBookRecoveryService: IHostedService that runs once during startup.
//   Queries all orders with status Pending or PartiallyFilled from the
//   orders projection table and adds them back to the singleton OrderBook
//   via IOrderBookService.AddOrder(). The order book matching engine
//   is stateless at boot and requires this recovery step.
//
// MEMBER DOCUMENTATION:
// - _scopeFactory: Injected IServiceScopeFactory to create an isolated
//   scope for resolving the scoped INexusUnitOfWork during startup.
// - _orderBookService: Injected IOrderBookService singleton to reload
//   orders into the in-memory matching engine.
// - StartAsync: Creates a scope, queries active orders, adds each to
//   the order book, and logs the count.
// - StopAsync: No-op -- no cleanup required on graceful shutdown.
// ============================================================================

namespace NexusEngine.Infrastructure.OrderBook;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexusEngine.Api.Application.Abstractions;
using NexusEngine.Application.Abstractions;

public class OrderBookRecoveryService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOrderBookService _orderBookService;

    public OrderBookRecoveryService(
        IServiceScopeFactory scopeFactory,
        IOrderBookService orderBookService)
    {
        _scopeFactory = scopeFactory;
        _orderBookService = orderBookService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<INexusUnitOfWork>();

        var orders = await uow.Orders
            .Where(o => o.Status == "Pending" || o.Status == "PartiallyFilled")
            .ToListAsync(cancellationToken);

        foreach (var order in orders)
            _orderBookService.AddOrder(order);

        Console.WriteLine(
            $"[OrderBookRecovery] Reloaded {orders.Count} orders into order book.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
