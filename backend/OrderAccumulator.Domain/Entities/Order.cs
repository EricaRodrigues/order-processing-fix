namespace OrderAccumulator.Domain.Entities;

public class Order
{
    public string ClOrdId { get; }
    public string Symbol { get; }
    public char Side { get; }
    public int Quantity { get; }
    public decimal Price { get; }
    public decimal FinancialValue => Price * Quantity;

    public Order(string clOrdId, string symbol, char side, int quantity, decimal price)
    {
        ClOrdId = clOrdId;
        Symbol = symbol;
        Side = side;
        Quantity = quantity;
        Price = price;
    }
}
