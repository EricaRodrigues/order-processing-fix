namespace OrderGenerator.Api.DTOs;

using System.ComponentModel.DataAnnotations;

public class OrderRequest
{
    [Required]
    public string Symbol { get; set; } = string.Empty;   // PETR4, VALE3, VIIA4

    [Required]
    public string Side { get; set; } = string.Empty;     // Buy, Sell

    [Range(1, 99999)]
    public int Quantity { get; set; }

    [Range(0.01, 999.99)]
    public decimal Price { get; set; }
}

public class OrderResponse
{
    public bool Accepted { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
