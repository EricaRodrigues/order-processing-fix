using OrderGenerator.Infrastructure.Fix;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<OrderClient>(sp =>
{
    var client = new OrderClient();
    var configPath = Path.Combine(AppContext.BaseDirectory, "Fix", "Config", "generator.cfg");
    var host = Environment.GetEnvironmentVariable("ACCUMULATOR_HOST") ?? "localhost";
    client.Start(configPath, host);
    return client;
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();
app.MapControllers();
app.Run();