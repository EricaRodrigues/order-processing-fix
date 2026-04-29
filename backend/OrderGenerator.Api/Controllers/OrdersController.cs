using Microsoft.AspNetCore.Mvc;
using OrderGenerator.Api.DTOs;
using OrderGenerator.Infrastructure.Fix;
using QuickFix.Fields;

namespace OrderGenerator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderClient _orderClient;

    public OrdersController(OrderClient orderClient)
    {
        _orderClient = orderClient;
    }

    [HttpPost]
    public async Task<IActionResult> SendOrder([FromBody] OrderRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var validSymbols = new[] { "PETR4", "VALE3", "VIIA4" };
        if (!validSymbols.Contains(request.Symbol))
            return BadRequest(new OrderResponse { Accepted = false, Status = "Error", Message = "Invalid symbol." });

        if (request.Price % 0.01m != 0)
            return BadRequest(new OrderResponse
                { Accepted = false, Status = "Error", Message = "Price must be a multiple of 0.01." });

        var side = request.Side.ToLower() switch
        {
            "buy" => Side.BUY,
            "sell" => Side.SELL,
            _ => throw new ArgumentException("Invalid side")
        };

        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await _orderClient.WaitForConnectionAsync(timeoutCts.Token);

            var report = await _orderClient.SendOrderAsync(request.Symbol, side, request.Quantity, request.Price);

            if (report is null)
                return StatusCode(504,
                    new OrderResponse
                        { Accepted = false, Status = "Timeout", Message = "No response from OrderAccumulator." });

            var accepted = report.ExecType.Value == ExecType.NEW;

            return Ok(new OrderResponse
            {
                Accepted = accepted,
                Status = accepted ? "New" : "Rejected",
                Message = accepted
                    ? $"Order accepted. Symbol: {request.Symbol}, Qty: {request.Quantity}, Price: {request.Price}"
                    : $"Order rejected. Exposure limit exceeded for {request.Symbol}."
            });
        }
        catch (OperationCanceledException)
        {
            return StatusCode(503,
                new OrderResponse
                    { Accepted = false, Status = "Unavailable", Message = "FIX session could not be established." });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(503,
                new OrderResponse { Accepted = false, Status = "Unavailable", Message = ex.Message });
        }
    }
}