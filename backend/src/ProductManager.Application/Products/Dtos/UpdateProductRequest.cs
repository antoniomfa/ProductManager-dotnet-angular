namespace ProductManager.Application.Products.Dtos;

/// <summary>
/// Payload for PUT /api/products/{id}.
/// </summary>
public sealed record UpdateProductRequest
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public decimal Price { get; init; }

    public int LowStockThreshold { get; init; } = 5;

    public int CategoryId { get; init; }

    /// <summary>
    /// Optional absolute stock level, for stock takes. Leave null to leave stock untouched —
    /// the dedicated add-to-stock and decrement-stock endpoints are the normal way to move stock.
    /// When supplied and different from the current level, the difference is recorded as an
    /// Adjustment movement.
    /// </summary>
    public int? StockQuantity { get; init; }
}
