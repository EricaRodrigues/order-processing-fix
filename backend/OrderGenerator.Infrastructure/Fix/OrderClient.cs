using System.Collections.Concurrent;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
using QuickFix.Logger;
using QuickFix.Store;
using QuickFix.Transport;

namespace OrderGenerator.Infrastructure.Fix;

public class OrderClient : MessageCracker, IApplication, IDisposable
{
    private SocketInitiator? _initiator;
    private SessionID? _sessionId;

    private readonly ConcurrentDictionary<string, TaskCompletionSource<ExecutionReport>> _pendingOrders = new();
    public bool IsConnected => _sessionId is not null;

    public void Start(string configPath, string accumulatorHost)
    {
        var configText = File.ReadAllText(configPath)
            .Replace("{ACCUMULATOR_HOST}", accumulatorHost);

        var tempPath = Path.Combine(Path.GetTempPath(), "generator_resolved.cfg");
        File.WriteAllText(tempPath, configText);

        var settings     = new SessionSettings(tempPath);
        var storeFactory = new FileStoreFactory(settings);
        var logFactory   = new FileLogFactory(settings);

        _initiator = new SocketInitiator(this, storeFactory, settings, logFactory);
        _initiator.Start();
    }

    public void OnLogon(SessionID sessionId)
    {
        _sessionId = sessionId;
        Console.WriteLine($"[FIX] Connected to Accumulator: {sessionId}");
    }

    public void OnLogout(SessionID sessionId)
    {
        _sessionId = null;
        Console.WriteLine($"[FIX] Disconnected from Accumulator");
    }

    public void FromApp(QuickFix.Message message, SessionID sessionId)
        => Crack(message, sessionId);

    public void OnMessage(ExecutionReport report, SessionID sessionId)
    {
        var clOrdId = report.ClOrdID.Value;
        
        if (_pendingOrders.TryRemove(clOrdId, out var tcs))
            tcs.SetResult(report);
    }

    public async Task<ExecutionReport?> SendOrderAsync(
        string symbol, char side, int quantity, decimal price,
        CancellationToken ct = default)
    {
        if (_sessionId is null)
            throw new InvalidOperationException("FIX session not established.");

        var clOrdId = Guid.NewGuid().ToString();
        var tcs     = new TaskCompletionSource<ExecutionReport>();

        _pendingOrders[clOrdId] = tcs;

        var order = new NewOrderSingle(
            new ClOrdID(clOrdId),
            new Symbol(symbol),
            new Side(side),
            new TransactTime(DateTime.UtcNow),
            new OrdType(OrdType.LIMIT)
        );

        order.Set(new OrderQty(quantity));
        order.Set(new Price(price));
        order.Set(new TimeInForce(TimeInForce.DAY));

        Session.SendToTarget(order, _sessionId);
        
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            return await tcs.Task.WaitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            _pendingOrders.TryRemove(clOrdId, out _);
            return null;
        }
    }
    
    public async Task WaitForConnectionAsync(CancellationToken ct = default)
    {
        while (!IsConnected && !ct.IsCancellationRequested)
            await Task.Delay(100, ct);
    }

    public void OnCreate(SessionID sessionId) { }
    public void ToApp(QuickFix.Message message, SessionID sessionId) { }
    public void FromAdmin(QuickFix.Message message, SessionID sessionId) { }
    public void ToAdmin(QuickFix.Message message, SessionID sessionId) { }

    public void Dispose() => _initiator?.Stop();
}