using ProductManager.Domain.Enums;

namespace ProductManager.Application.Products.Dtos;

/// <summary>
/// Returned by the add-to-stock and decrement-stock endpoints. Reporting the level both
/// before and after lets a client confirm the delta it actually got, which matters when
/// concurrent callers are moving the same product's stock.
/// </summary>
public sealed record StockOperationResult
{
    public required int ProductId { get; init; }

    public required string Name { get; init; }

    public required int PreviousStock { get; init; }

    public required int NewStock { get; init; }

    public required StockStatus StockStatus { get; init; }
}
