namespace ProductManager.Application.Categories;

/// <summary>
/// Read-only access to categories.
/// </summary>
public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
}
