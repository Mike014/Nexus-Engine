// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Application.Abstractions
//
// FILE DESCRIPTION:
// Defines the service contract for the limit-order matching engine.
// Provides an abstraction over the OrderBook domain logic for the
// Application layer, following the same pattern as INexusUnitOfWork.
//
// CLASS DOCUMENTATION:
// - IOrderBookService: Interface exposing order matching and order placement
//   operations. The concrete implementation lives in Infrastructure.OrderBook.
//   This keeps the Application layer decoupled from the matching engine's
//   internal state management.
//
// MEMBER DOCUMENTATION:
// - Match(): Processes an incoming order through price-time priority FIFO
//   matching. Returns all trades produced during matching. Unmatched quantity
//   is automatically added to the resting book.
// - AddOrder(): Places an order directly into the book without explicit
//   match result handling. The matching engine processes it internally.
// ============================================================================

namespace NexusEngine.Application.Abstractions;

using NexusEngine.Api.Domain.Entities;
using NexusEngine.Domain.OrderBook;

public interface IOrderBookService
{
    MatchResult Match(Order order);

    void AddOrder(Order order);
}
