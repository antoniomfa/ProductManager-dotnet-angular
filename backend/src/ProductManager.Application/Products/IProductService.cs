using ProductManager.Application.Common;
using ProductManager.Application.Products.Dtos;

namespace ProductManager.Application.Products;

/// <summary>
/// Use cases for the product catalogue. Controllers hold no business logic of their own;
/// they translate HTTP into these calls and back again.
/// </summary>
public interface IProductService
{
    /// <summary>Returns a filtered, sorted page of products.</summary>
    Task<PagedResult<ProductDto>> GetProductsAsync(
        ProductQueryParameters query,
        CancellationToken cancellationToken = default);

    /// <summary>Returns one product with its stock movement history.</summary>
    /// <exception cref="Exceptions.NotFoundException">No live product has this identifier.</exception>
    Task<ProductDetailDto> GetProductByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a product. The database allocates its unique 6-digit identifier on insert.
    /// </summary>
    /// <exception cref="Exceptions.NotFoundException">The requested category does not exist.</exception>
    Task<ProductDetailDto> CreateProductAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Applies an edit to an existing product.</summary>
    Task<ProductDetailDto> UpdateProductAsync(
        int id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes a product, preserving its movement history.</summary>
    Task DeleteProductAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Increases a product's stock.</summary>
    Task<StockOperationResult> AddToStockAsync(
        int id,
        int quantity,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>Decreases a product's stock.</summary>
    /// <exception cref="Domain.Exceptions.InsufficientStockException">Not enough units on hand.</exception>
    Task<StockOperationResult> DecrementStockAsync(
        int id,
        int quantity,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>Finds products whose name contains <paramref name="name"/>, case-insensitively.</summary>
    Task<IReadOnlyList<ProductDto>> SearchByNameAsync(
        string name,
        CancellationToken cancellationToken = default);

    /// <summary>Returns products whose stock sits within an inclusive range.</summary>
    Task<IReadOnlyList<ProductDto>> GetByStockLevelAsync(
        int? minimum,
        int? maximum,
        CancellationToken cancellationToken = default);
}
