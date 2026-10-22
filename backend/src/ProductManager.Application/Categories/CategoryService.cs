using ProductManager.Application.Abstractions;

namespace ProductManager.Application.Categories;

/// <inheritdoc cref="ICategoryService" />
public sealed class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categories;

    public CategoryService(ICategoryRepository categories) => _categories = categories;

    public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        _categories.GetAllWithProductCountsAsync(cancellationToken);
}
