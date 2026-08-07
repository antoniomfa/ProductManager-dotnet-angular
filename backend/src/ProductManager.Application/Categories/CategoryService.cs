using Microsoft.EntityFrameworkCore;
using ProductManager.Application.Abstractions;

namespace ProductManager.Application.Categories;

/// <inheritdoc cref="ICategoryService" />
public sealed class CategoryService : ICategoryService
{
    private readonly IApplicationDbContext _context;

    public CategoryService(IApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        await _context.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                // The products navigation carries the soft-delete query filter, so deleted
                // products are left out of this count automatically.
                ProductCount = category.Products.Count,
            })
            .ToListAsync(cancellationToken);
}
