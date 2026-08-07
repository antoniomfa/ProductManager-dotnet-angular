using ProductManager.Application.Products.Dtos;
using ProductManager.Domain.Entities;

namespace ProductManager.Application.Products.Mapping;

/// <summary>
/// Hand-written entity-to-DTO projections.
/// </summary>
/// <remarks>
/// Deliberately not a convention-based mapper. These projections are compile-time checked, cost
/// nothing at startup, step through in a debugger, and make it obvious at review time exactly
/// which fields cross the API boundary — which matters more here than saving a few lines.
/// </remarks>
public static class ProductMappings
{
    public static ProductDto ToDto(this Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        Price = product.Price,
        StockQuantity = product.StockQuantity,
        LowStockThreshold = product.LowStockThreshold,
        StockStatus = product.StockStatus,
        CategoryId = product.CategoryId,
        CategoryName = product.Category?.Name,
        CreatedAt = product.CreatedAt,
        UpdatedAt = product.UpdatedAt,
    };

    public static ProductDetailDto ToDetailDto(this Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        Price = product.Price,
        StockQuantity = product.StockQuantity,
        LowStockThreshold = product.LowStockThreshold,
        StockStatus = product.StockStatus,
        CategoryId = product.CategoryId,
        CategoryName = product.Category?.Name,
        CreatedAt = product.CreatedAt,
        UpdatedAt = product.UpdatedAt,
        StockMovements = [.. product.StockMovements
            .OrderByDescending(movement => movement.OccurredAt)
            .ThenByDescending(movement => movement.Id)
            .Select(ToDto)],
    };

    public static StockMovementDto ToDto(this StockMovement movement) => new()
    {
        Id = movement.Id,
        Type = movement.Type,
        QuantityChange = movement.QuantityChange,
        ResultingStock = movement.ResultingStock,
        Reason = movement.Reason,
        OccurredAt = movement.OccurredAt,
    };

    public static IReadOnlyList<ProductDto> ToDtoList(this IEnumerable<Product> products) =>
        [.. products.Select(ToDto)];
}
