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

    // canal para receber a resposta de forma assíncrona
    private readonly SemaphoreSlim _responseSemaphore = new(0, 1);
    private ExecutionReport? _lastReport;

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
    {
        Crack(message, sessionId);
    }

    // recebe o ExecutionReport do Accumulator
    public void OnMessage(ExecutionReport report, SessionID sessionId)
    {
        _lastReport = report;
        _responseSemaphore.Release();
    }

    public async Task<ExecutionReport?> SendOrderAsync(
        string symbol, char side, int quantity, decimal price,
        CancellationToken ct = default)
    {
        if (_sessionId is null)
            throw new InvalidOperationException("FIX session not established.");

        var order = new NewOrderSingle(
            new ClOrdID(Guid.NewGuid().ToString()),
            new Symbol(symbol),
            new Side(side),
            new TransactTime(DateTime.UtcNow),
            new OrdType(OrdType.LIMIT)
        );

        order.Set(new OrderQty(quantity));
        order.Set(new Price(price));
        order.Set(new TimeInForce(TimeInForce.DAY));

        Session.SendToTarget(order, _sessionId);

        // aguarda resposta por até 5 segundos
        var received = await _responseSemaphore.WaitAsync(TimeSpan.FromSeconds(5), ct);
        return received ? _lastReport : null;
    }

    public void OnCreate(SessionID sessionId) { }
    public void ToApp(QuickFix.Message message, SessionID sessionId) { }
    public void FromAdmin(QuickFix.Message message, SessionID sessionId) { }
    public void ToAdmin(QuickFix.Message message, SessionID sessionId) { }

    public void Dispose() => _initiator?.Stop();
}