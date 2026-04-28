using OrderAccumulator.Application.Services;
using OrderAccumulator.Worker.Fix;
using QuickFix;
using QuickFix.Logger;
using QuickFix.Store;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<ExposureService>();
builder.Services.AddSingleton<OrderServer>();
builder.Services.AddHostedService<AccumulatorHostedService>();

var host = builder.Build();
host.Run();

public class AccumulatorHostedService(OrderServer orderServer) : BackgroundService
{
    private ThreadedSocketAcceptor? _acceptor;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "Fix", "Config", "accumulator.cfg");
        var settings   = new SessionSettings(configPath);
        var storeFactory = new FileStoreFactory(settings);
        var logFactory   = new FileLogFactory(settings);

        _acceptor = new ThreadedSocketAcceptor(orderServer, storeFactory, settings, logFactory);
        _acceptor.Start();

        Console.WriteLine("[Accumulator] FIX Acceptor started on port 5001");

        stoppingToken.Register(() =>
        {
            _acceptor?.Stop();
            Console.WriteLine("[Accumulator] FIX Acceptor stopped");
        });

        return Task.CompletedTask;
    }
}