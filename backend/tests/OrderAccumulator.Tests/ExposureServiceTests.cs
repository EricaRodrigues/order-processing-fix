using OrderAccumulator.Application.Services;
using OrderAccumulator.Domain.Entities;
using System.Collections.Concurrent;

namespace OrderAccumulator.Tests;

public class ExposureServiceTests
{
    private readonly ExposureService _exposureService = new();

    [Fact]
    public void TryAccept_BuyOrderWithinLimit_ReturnsTrueAndUpdatesExposure()
    {
        var order = new Order("clOrdId1", "PETR4", '1', 1000, 50.00m);
        
        var result = _exposureService.TryAccept(order);
        
        Assert.True(result);
        Assert.Equal(50000m, _exposureService.GetExposure("PETR4"));
    }

    [Fact]
    public void TryAccept_SellOrderWithinLimit_ReturnsTrueAndUpdatesExposure()
    {
        var buyOrder = new Order("clOrdId1", "PETR4", '1', 2000, 50.00m);
        _exposureService.TryAccept(buyOrder);

        var sellOrder = new Order("clOrdId2", "PETR4", '2', 1000, 50.00m);
        var result = _exposureService.TryAccept(sellOrder);
        
        Assert.True(result);
        Assert.Equal(50000m, _exposureService.GetExposure("PETR4"));
    }

    [Fact]
    public void TryAccept_OrderExceedingPositiveLimit_ReturnsFalseAndDoesNotUpdateExposure()
    {
        var initialOrder = new Order("clOrdIdInitial", "VALE3", '1', 1999999, 50.00m);
        _exposureService.TryAccept(initialOrder);

        var exceedingOrder = new Order("clOrdIdExceed", "VALE3", '1', 2, 50.00m);
        var result = _exposureService.TryAccept(exceedingOrder);
        
        Assert.False(result);
        Assert.Equal(99999950m, _exposureService.GetExposure("VALE3"));
    }

    [Fact]
    public void TryAccept_OrderExceedingNegativeLimit_ReturnsFalseAndDoesNotUpdateExposure()
    {
        var initialOrder = new Order("clOrdIdInitial", "VALE3", '2', 1999999, 50.00m); 
        _exposureService.TryAccept(initialOrder);

        var exceedingOrder = new Order("clOrdIdExceed", "VALE3", '2', 2, 50.00m);
        var result = _exposureService.TryAccept(exceedingOrder);
        
        Assert.False(result);
        Assert.Equal(-99999950m, _exposureService.GetExposure("VALE3")); 
    }

    [Fact]
    public void TryAccept_MultipleSymbols_MaintainsSeparateExposures()
    {
        var order1 = new Order("clOrdId1", "PETR4", '1', 1000, 50.00m);
        var order2 = new Order("clOrdId2", "VALE3", '1', 500, 100.00m);
        
        _exposureService.TryAccept(order1);
        _exposureService.TryAccept(order2);
        
        Assert.Equal(50000m, _exposureService.GetExposure("PETR4"));
        Assert.Equal(50000m, _exposureService.GetExposure("VALE3"));
    }

    [Fact]
    public void GetExposure_ForNonExistentSymbol_ReturnsZero()
    {
        var exposure = _exposureService.GetExposure("NONEXISTENT");
        
        Assert.Equal(0m, exposure);
    }
    
    [Fact]
    public void TryAccept_HandleConcurrentOrders_MaintainsIntegrity()
    {
        const int numOrders = 1000;
        const decimal price = 10.00m;
        const int quantity = 10;
        const decimal totalExpectedExposure = numOrders * price * quantity;

        var orders = new ConcurrentBag<Order>();
        for (int i = 0; i < numOrders; i++)
        {
            orders.Add(new Order($"clOrdId{i}", "PETR4", '1', quantity, price));
        }
        
        Parallel.ForEach(orders, order =>
        {
            _exposureService.TryAccept(order);
        });
        
        Assert.Equal(totalExpectedExposure, _exposureService.GetExposure("PETR4"));
    }

    [Fact]
    public void TryAccept_ExactPositiveLimit_ReturnsTrue()
    {
        var order = new Order("clOrdId1", "PETR4", '1', 1000000, 100.00m); 
        var result = _exposureService.TryAccept(order);
        
        Assert.True(result);
        Assert.Equal(100000000m, _exposureService.GetExposure("PETR4"));
    }

    [Fact]
    public void TryAccept_ExactNegativeLimit_ReturnsTrue()
    {
        var order = new Order("clOrdId1", "PETR4", '2', 1000000, 100.00m); 
        var result = _exposureService.TryAccept(order);
        
        Assert.True(result);
        Assert.Equal(-100000000m, _exposureService.GetExposure("PETR4"));
    }

    [Fact]
    public void TryAccept_GiantOrderExceedingLimit_ReturnsFalse()
    {
        var order = new Order("clOrdId1", "PETR4", '1', 1000001, 100.00m);
        var result = _exposureService.TryAccept(order);
        
        Assert.False(result);
        Assert.Equal(0m, _exposureService.GetExposure("PETR4"));
    }

    [Fact]
    public void TryAccept_SwitchSidesWithinLimit_ReturnsTrueAndUpdatesCorrectly()
    {
        _exposureService.TryAccept(new Order("id1", "PETR4", '1', 500000, 100.00m));
        var order = new Order("id2", "PETR4", '2', 800000, 100.00m);
        var result = _exposureService.TryAccept(order);
        
        Assert.True(result);
        Assert.Equal(-30000000m, _exposureService.GetExposure("PETR4"));
    }
}
