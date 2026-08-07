using ProductManager.Domain.Enums;

namespace ProductManager.Application.Products.Dtos;

/// <summary>A single entry from a product's stock audit trail.</summary>
public sealed record StockMovementDto
{
    public required long Id { get; init; }

    public required StockMovementType Type { get; init; }

    /// <summary>Signed delta: positive for additions, negative for removals.</summary>
    public required int QuantityChange { get; init; }

    /// <summary>Stock level immediately after this movement was applied.</summary>
    public required int ResultingStock { get; init; }

    public string? Reason { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }
}
