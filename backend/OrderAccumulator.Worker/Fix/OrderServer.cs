namespace OrderAccumulator.Worker.Fix;

using OrderAccumulator.Application.Services;
using OrderAccumulator.Domain.Entities;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;

public class OrderServer : MessageCracker, IApplication
{
    private readonly ExposureService _exposureService;

    public OrderServer(ExposureService exposureService)
    {
        _exposureService = exposureService;
    }

    public void OnLogon(SessionID sessionId)
        => Console.WriteLine($"[FIX] Client connected: {sessionId}");

    public void OnLogout(SessionID sessionId)
        => Console.WriteLine($"[FIX] Client disconnected: {sessionId}");

    public void FromApp(QuickFix.Message message, SessionID sessionId)
        => Crack(message, sessionId);

    public void OnMessage(NewOrderSingle order, SessionID sessionId)
    {
        var clOrdId  = order.ClOrdID.Value;
        var symbol   = order.Symbol.Value;
        var side     = order.Side.Value;
        var quantity = (int)order.OrderQty.Value;
        var price    = order.Price.Value;

        Console.WriteLine($"[FIX] NewOrderSingle received: {symbol} {(side == '1' ? "BUY" : "SELL")} {quantity} @ {price}");

        var domainOrder = new Order(clOrdId, symbol, side, quantity, price);
        var accepted    = _exposureService.TryAccept(domainOrder);

        Console.WriteLine($"[FIX] Order {clOrdId}: {(accepted ? "ACCEPTED" : "REJECTED")} | Exposure[{symbol}]: {_exposureService.GetExposure(symbol):C}");

        SendExecutionReport(sessionId, domainOrder, accepted);
    }

    protected virtual void SendExecutionReport(SessionID sessionId, Order order, bool accepted)
    {
        var report = new ExecutionReport(
            new OrderID(Guid.NewGuid().ToString()),
            new ExecID(Guid.NewGuid().ToString()),
            new ExecType(accepted ? ExecType.NEW : ExecType.REJECTED),
            new OrdStatus(accepted ? OrdStatus.NEW : OrdStatus.REJECTED),
            new Symbol(order.Symbol),
            new Side(order.Side),
            new LeavesQty(accepted ? order.Quantity : 0),
            new CumQty(0),
            new AvgPx(0)
        );

        if (!accepted)
            report.Set(new Text("Exposure limit exceeded"));
        
        report.Set(new ClOrdID(order.ClOrdId));
        Session.SendToTarget(report, sessionId);
    }

    public void OnCreate(SessionID sessionId) { }
    public void ToApp(QuickFix.Message message, SessionID sessionId) { }
    public void FromAdmin(QuickFix.Message message, SessionID sessionId) { }
    public void ToAdmin(QuickFix.Message message, SessionID sessionId) { }
}