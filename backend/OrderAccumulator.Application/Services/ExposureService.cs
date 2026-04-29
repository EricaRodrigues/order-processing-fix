using QuickFix.Fields;

namespace OrderAccumulator.Application.Services;

using OrderAccumulator.Domain.Entities;

public class ExposureService
{
    private const decimal Limit = 100_000_000m;

    private readonly Dictionary<string, decimal> _exposures = new();
    private readonly object _lock = new();

    public bool TryAccept(Order order)
    {
        lock (_lock)
        {
            var current = _exposures.GetValueOrDefault(order.Symbol, 0m);

            var isBuy = order.Side == Side.BUY;
            
            var delta = isBuy
                ? order.FinancialValue
                : -order.FinancialValue;

            var projected = current + delta;

            if (Math.Abs(projected) > Limit)
                return false;

            _exposures[order.Symbol] = projected;
            return true;
        }
    }

    public decimal GetExposure(string symbol)
    {
        lock (_lock)
        {
            return _exposures.TryGetValue(symbol, out var value) ? value : 0m;
        }
    }
}