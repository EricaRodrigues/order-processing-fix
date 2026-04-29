using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
using OrderAccumulator.Application.Services;
using OrderAccumulator.Worker.Fix;
using OrderAccumulator.Domain.Entities;

namespace OrderAccumulator.Tests;

public class TestableOrderServer(ExposureService exposureService) : OrderServer(exposureService)
{
    public bool LastAccepted { get; private set; }

    protected override void SendExecutionReport(SessionID sessionId, Order order, bool accepted)
    {
        LastAccepted = accepted;
    }
}

public class OrderServerTests
{
    private readonly ExposureService _exposureService;
    private readonly TestableOrderServer _orderServer;
    private readonly SessionID _sessionId;

    public OrderServerTests()
    {
        _exposureService = new ExposureService();
        _orderServer = new TestableOrderServer(_exposureService);
        _sessionId = new SessionID("FIX.4.4", "SENDER", "TARGET");
    }

    private NewOrderSingle CreateOrder(string clOrdId, string symbol, char side, int quantity, decimal price)
    {
        var order = new NewOrderSingle();
        order.ClOrdID = new ClOrdID(clOrdId);
        order.Symbol = new Symbol(symbol);
        order.Side = new Side(side);
        order.OrderQty = new OrderQty(quantity);
        order.Price = new Price(price);
        order.TransactTime = new TransactTime(DateTime.UtcNow);
        return order;
    }

    [Fact]
    public void OnMessage_BuyOrder_AcceptedAndUpdatesExposure()
    {
        var order = CreateOrder("id1", "PETR4", '1', 100, 38.50m);
        _orderServer.OnMessage(order, _sessionId);

        Assert.True(_orderServer.LastAccepted);
        Assert.Equal(3850m, _exposureService.GetExposure("PETR4"));
    }

    [Fact]
    public void OnMessage_SellOrder_AcceptedAndDecreasesExposure()
    {
        _orderServer.OnMessage(CreateOrder("id1", "PETR4", '1', 1000, 50m), _sessionId);
        _orderServer.OnMessage(CreateOrder("id2", "PETR4", '2', 500, 50m), _sessionId);

        Assert.True(_orderServer.LastAccepted);
        Assert.Equal(25000m, _exposureService.GetExposure("PETR4"));
    }

    [Fact]
    public void OnMessage_OrderExceedingLimit_RejectedAndExposureUnchanged()
    {
        var order = CreateOrder("id1", "PETR4", '1', 2000000, 100m);
        _orderServer.OnMessage(order, _sessionId);

        Assert.False(_orderServer.LastAccepted);
        Assert.Equal(0m, _exposureService.GetExposure("PETR4"));
    }

    [Fact]
    public void OnMessage_MultipleSymbols_MaintainsSeparateExposures()
    {
        _orderServer.OnMessage(CreateOrder("id1", "PETR4", '1', 1000, 50m), _sessionId);
        _orderServer.OnMessage(CreateOrder("id2", "VALE3", '1', 500, 100m), _sessionId);

        Assert.Equal(50000m, _exposureService.GetExposure("PETR4"));
        Assert.Equal(50000m, _exposureService.GetExposure("VALE3"));
    }

    [Fact]
    public void OnMessage_SecondOrderPushesOverLimit_Rejected()
    {
        _orderServer.OnMessage(CreateOrder("id1", "PETR4", '1', 999999, 100m), _sessionId);
        _orderServer.OnMessage(CreateOrder("id2", "PETR4", '1', 10000, 100m), _sessionId); 

        Assert.False(_orderServer.LastAccepted);
        Assert.Equal(99999900m, _exposureService.GetExposure("PETR4"));
    }
    
    [Fact]
    public void OnMessage_RejectedOrder_DoesNotAffectSubsequentAcceptance()
    {
        _orderServer.OnMessage(CreateOrder("id1", "PETR4", '1', 2000000, 100m), _sessionId);
        Assert.Equal(0m, _exposureService.GetExposure("PETR4"));

        _orderServer.OnMessage(CreateOrder("id2", "PETR4", '1', 100, 38.50m), _sessionId);
        Assert.True(_orderServer.LastAccepted);
        Assert.Equal(3850m, _exposureService.GetExposure("PETR4"));
    }
}