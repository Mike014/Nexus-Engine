// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Application.Orders.Commands.CancelOrder
//
// FILE DESCRIPTION:
// Implements the MediatR handler for CancelOrderCommand.
// Handles cancellation of resting orders with balance refund logic.
//
// CLASS DOCUMENTATION:
// - CancelOrderHandler: Executes the order cancellation workflow.
//   Verifies ownership and order status, removes the order from the
//   matching engine, refunds reserved balance for buy orders, and
//   persists an OrderCancelled domain event atomically.
//
// MEMBER DOCUMENTATION:
// - _uow: Injected INexusUnitOfWork -- single unit of work for atomic persistence.
// - _orderBookService: Injected IOrderBookService singleton -- in-memory matching engine.
// - _mediator: Injected IMediator -- publishes notifications after successful persistence.
// - Handle: Loads the order, validates ownership and cancellable status,
//   removes from order book, applies refund, persists the cancellation event.
//   Throws KeyNotFoundException if order or account does not exist.
//   Throws InvalidOperationException for ownership mismatch or invalid status.
// ============================================================================

namespace NexusEngine.Api.Application.Orders.Commands.CancelOrder;

using MediatR;
using NexusEngine.Api.Application.Abstractions;
using NexusEngine.Api.Application.Common;
using NexusEngine.Api.Application.Orders.Notifications;
using NexusEngine.Api.Domain.Entities;
using NexusEngine.Application.Abstractions;

public class CancelOrderHandler : IRequestHandler<CancelOrderCommand, Unit>
{
    private readonly INexusUnitOfWork _uow;
    private readonly IOrderBookService _orderBookService;
    private readonly IMediator _mediator;

    public CancelOrderHandler(
        INexusUnitOfWork uow,
        IOrderBookService orderBookService,
        IMediator mediator)
    {
        _uow = uow;
        _orderBookService = orderBookService;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(
        CancelOrderCommand command,
        CancellationToken cancellationToken)
    {
        Account account;
        string symbol;

        using (await _orderBookService.AcquireAsync(cancellationToken))
        {
            var order = await _uow.Orders.FindAsync([command.OrderId], cancellationToken)
                ?? throw new KeyNotFoundException($"Order {command.OrderId} not found.");

            if (order.AccountId != command.AccountId)
                throw new InvalidOperationException("Order does not belong to this account.");

            if (order.Status != "Pending" && order.Status != "PartiallyFilled")
                throw new InvalidOperationException(
                    "Only Pending or PartiallyFilled orders can be cancelled.");

            account = await _uow.Accounts.FindAsync([order.AccountId], cancellationToken)
                ?? throw new KeyNotFoundException($"Account {order.AccountId} not found.");

            symbol = order.Symbol;
            var refundAmount = order.Side == "Buy"
                ? order.RemainingQuantity * order.Price
                : 0m;

            order.Status = "Cancelled";
            order.UpdatedAt = DateTime.UtcNow;

            AccountLedger.Append(_uow, account, "OrderCancelled", order.Id,
                balanceDelta: 0m, reservedDelta: -refundAmount,
                new { OrderId = order.Id, RefundAmount = refundAmount });

            var checkpoint = _orderBookService.Checkpoint(symbol);
            _orderBookService.RemoveOrder(order);

            try
            {
                await _uow.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                _orderBookService.Restore(checkpoint);
                _uow.DiscardChanges();
                throw;
            }
        }

        await _mediator.Publish(new OrderBookChangedNotification(symbol), cancellationToken);

        await _mediator.Publish(
            new BalanceChangedNotification(account.Id, account.Balance, account.ReservedBalance),
            cancellationToken);

        return Unit.Value;
    }
}
