using ProductManager.Domain.Enums;

namespace ProductManager.Application.Products.Dtos;

/// <summary>
/// Product representation returned by every listing endpoint. Always carries the available
/// stock and its derived status, so clients never have to compute either themselves.
/// </summary>
public sealed record ProductDto
{
    /// <summary>Unique auto-generated 6-digit identifier.</summary>
    public required int Id { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public required decimal Price { get; init; }

    /// <summary>Units currently available to sell.</summary>
    public required int StockQuantity { get; init; }

    public required int LowStockThreshold { get; init; }

    /// <summary>Derived indicator: OutOfStock, LowStock or InStock.</summary>
    public required StockStatus StockStatus { get; init; }

    public required int CategoryId { get; init; }

    public string? CategoryName { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
