using ProductManager.Application.Categories;

namespace ProductManager.Application.Abstractions;

/// <summary>Read access to categories.</summary>
public interface ICategoryRepository
{
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Returns every category with the number of live products in it, ordered by name.</summary>
    Task<IReadOnlyList<CategoryDto>> GetAllWithProductCountsAsync(CancellationToken cancellationToken = default);
}
