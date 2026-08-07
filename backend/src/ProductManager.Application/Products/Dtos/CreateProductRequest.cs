namespace ProductManager.Application.Products.Dtos;

/// <summary>
/// Payload for POST /api/products. The identifier is never accepted from the client:
/// the database allocates it so uniqueness holds across API instances.
/// </summary>
public sealed record CreateProductRequest
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public decimal Price { get; init; }

    /// <summary>Units on hand at creation time. Recorded as an InitialStock movement.</summary>
    public int InitialStock { get; init; }

    /// <summary>Level at or below which the product reports as low on stock.</summary>
    public int LowStockThreshold { get; init; } = 5;

    public int CategoryId { get; init; }
}
