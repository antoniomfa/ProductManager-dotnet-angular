using ProductManager.Application.Common;
using ProductManager.Application.Products.Dtos;
using ProductManager.Domain.Entities;

namespace ProductManager.Application.Abstractions;

/// <summary>
/// Loads and stores products. Deleted products are never returned.
/// </summary>
/// <remarks>
/// Products returned by the <c>GetById</c> methods are tracked, so changes made to them are written
/// by the next <see cref="IUnitOfWork.SaveChangesAsync"/>. The list queries return read-only
/// snapshots.
/// </remarks>
public interface IProductRepository
{
    /// <summary>Returns the product with its category, or null when it does not exist.</summary>
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>As <see cref="GetByIdAsync"/>, but also loads the stock movement history.</summary>
    Task<Product?> GetByIdWithMovementsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Returns one filtered, sorted page of products.</summary>
    Task<PagedResult<Product>> GetPageAsync(
        ProductQueryParameters query,
        CancellationToken cancellationToken = default);

    /// <summary>Returns products whose name contains <paramref name="term"/>, ordered by name.</summary>
    Task<IReadOnlyList<Product>> SearchByNameAsync(
        string term,
        int maxResults,
        CancellationToken cancellationToken = default);

    /// <summary>Returns products whose stock is within the inclusive range, ordered by stock.</summary>
    Task<IReadOnlyList<Product>> GetByStockLevelAsync(
        int? minimum,
        int? maximum,
        CancellationToken cancellationToken = default);

    /// <summary>Queues a new product for insertion on the next save.</summary>
    void Add(Product product);
}
