using ProductManager.Domain.Enums;

namespace ProductManager.Application.Products.Dtos;

/// <summary>
/// Single-product representation. Extends the list shape with the stock audit trail,
/// which would be too heavy to include when returning many products at once.
/// </summary>
public sealed record ProductDetailDto
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public required decimal Price { get; init; }

    public required int StockQuantity { get; init; }

    public required int LowStockThreshold { get; init; }

    public required StockStatus StockStatus { get; init; }

    public required int CategoryId { get; init; }

    public string? CategoryName { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>Movement history, most recent first.</summary>
    public IReadOnlyList<StockMovementDto> StockMovements { get; init; } = [];
}
