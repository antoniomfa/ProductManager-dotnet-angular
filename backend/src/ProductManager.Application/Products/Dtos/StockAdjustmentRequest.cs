namespace ProductManager.Application.Products.Dtos;

/// <summary>
/// Optional body for the stock endpoints. The quantity travels in the route, so the body
/// exists purely to attach a human-readable reason to the resulting audit entry.
/// </summary>
public sealed record StockAdjustmentRequest
{
    /// <summary>Free-text note stored against the movement, for example "Customer order #4821".</summary>
    public string? Reason { get; init; }
}
