namespace NexusEngine.Tests.Application.Orders;

using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using NexusEngine.Api.Application.Accounts.Commands.DepositFunds;
using NexusEngine.Api.Application.Accounts.Queries.ReplayAccount;
using NexusEngine.Api.Application.Orders.Commands.CancelOrder;
using NexusEngine.Api.Application.Orders.Commands.PlaceOrder;
using NexusEngine.Api.Application.Orders.Validation;
using NexusEngine.Api.Domain.Entities;
using NexusEngine.Api.Infrastructure.Persistence;
using NexusEngine.Infrastructure.OrderBook;
using Xunit;

public class OrderHandlerTests
{
    private readonly string _dbName = Guid.NewGuid().ToString();
    private readonly OrderBookService _engine = new();
    private readonly Mock<IMediator> _mediator = new();

    private NexusUnitOfWork NewUow() => new(new NexusDbContext(
        new DbContextOptionsBuilder<NexusDbContext>().UseInMemoryDatabase(_dbName).Options));

    private async Task<Guid> CreateAccount(decimal deposit)
    {
        var id = Guid.NewGuid();
        var uow = NewUow();
        uow.Accounts.Add(new Account { Id = id, OwnerName = "t", Currency = "USD", LastEventVersion = 1 });
        uow.DomainEvents.Add(new DomainEvent
        {
            AggregateId = id,
            AggregateType = "Account",
            EventType = "AccountCreated",
            AggregateVersion = 1,
            Payload = "{\"OwnerName\":\"t\",\"Currency\":\"USD\",\"InitialBalance\":0}"
        });
        await uow.SaveChangesAsync();

        if (deposit > 0)
            await new DepositFundsHandler(NewUow()).Handle(new DepositFundsCommand(id, deposit), default);

        return id;
    }

    private Task<Guid> Place(Guid account, string side, decimal qty, decimal price, string symbol = "BTC/USD")
    {
        var handler = new PlaceOrderHandler(
            NewUow(),
            [new AccountExistsValidation(), new AccountActiveValidation(), new SufficientBalanceValidation()],
            _engine,
            _mediator.Object);
        return handler.Handle(new PlaceOrderCommand(account, symbol, side, qty, price), default);
    }

    private Task Cancel(Guid orderId, Guid account, OrderBookService? engine = null) =>
        new CancelOrderHandler(NewUow(), engine ?? _engine, _mediator.Object)
            .Handle(new CancelOrderCommand(orderId, account), default);

    private async Task<Account> LoadAccount(Guid id) => (await NewUow().Accounts.FindAsync(id))!;

    private async Task<Order> LoadOrder(Guid id) => (await NewUow().Orders.FindAsync(id))!;

    [Fact]
    public async Task Fill_BuyerPaysTradePrice_AndPriceImprovementIsReleased()
    {
        var seller = await CreateAccount(0m);
        var buyer = await CreateAccount(1000m);

        await Place(seller, "Sell", 2m, 90m);
        await Place(buyer, "Buy", 2m, 100m);

        var b = await LoadAccount(buyer);
        Assert.Equal(820m, b.Balance);
        Assert.Equal(0m, b.ReservedBalance);
        Assert.Equal(180m, (await LoadAccount(seller)).Balance);
    }

    [Fact]
    public async Task RestingOrder_IsInBookOnlyOnce()
    {
        var buyer = await CreateAccount(1000m);
        var seller = await CreateAccount(0m);

        await Place(buyer, "Buy", 1m, 100m);
        await Place(seller, "Sell", 1m, 100m);
        var second = await Place(seller, "Sell", 1m, 100m);

        Assert.Equal("Pending", (await LoadOrder(second)).Status);
    }

    [Fact]
    public async Task EveryAccount_ReplayMatchesProjection()
    {
        var seller = await CreateAccount(0m);
        var buyer = await CreateAccount(1000m);

        await Place(seller, "Sell", 3m, 90m);
        await Place(buyer, "Buy", 2m, 100m);
        var resting = await Place(buyer, "Buy", 1m, 50m);
        await Cancel(resting, buyer);

        foreach (var id in new[] { seller, buyer })
        {
            var projection = await LoadAccount(id);
            var replay = await new ReplayAccountHandler(NewUow()).Handle(new ReplayAccountQuery(id), default);

            Assert.NotNull(replay);
            Assert.Equal(projection.Balance, replay!.Balance);
            Assert.Equal(projection.ReservedBalance, replay.ReservedBalance);
            Assert.Equal(projection.LastEventVersion, replay.LastEventVersion);
            Assert.Equal(0, replay.LegacyEventsSkipped);
        }
    }

    [Fact]
    public async Task RejectedOrder_LeavesBookUntouched()
    {
        var seller = await CreateAccount(0m);
        await Place(seller, "Sell", 1m, 100m);

        var poorBuyer = await CreateAccount(10m);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Place(poorBuyer, "Buy", 1m, 100m));

        var buyer = await CreateAccount(1000m);
        await Place(buyer, "Buy", 1m, 100m);
        Assert.Equal(100m, (await LoadAccount(seller)).Balance);
    }

    [Fact]
    public async Task DifferentSymbols_DoNotCross()
    {
        var seller = await CreateAccount(0m);
        var buyer = await CreateAccount(1000m);

        await Place(seller, "Sell", 1m, 100m, "BTC/USD");
        var buyId = await Place(buyer, "Buy", 1m, 100m, "ETH/USD");

        Assert.Equal("Pending", (await LoadOrder(buyId)).Status);
    }

    [Fact]
    public async Task Cancel_OrderMissingFromBook_StillReleasesReservation()
    {
        var buyer = await CreateAccount(1000m);
        var orderId = await Place(buyer, "Buy", 1m, 100m);

        await Cancel(orderId, buyer, new OrderBookService());

        Assert.Equal(0m, (await LoadAccount(buyer)).ReservedBalance);
        Assert.Equal("Cancelled", (await LoadOrder(orderId)).Status);
    }
}
