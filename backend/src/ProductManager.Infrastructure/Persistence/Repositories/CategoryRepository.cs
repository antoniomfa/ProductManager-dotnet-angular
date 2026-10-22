using Microsoft.EntityFrameworkCore;
using ProductManager.Application.Abstractions;
using ProductManager.Application.Categories;

namespace ProductManager.Infrastructure.Persistence.Repositories;

/// <inheritdoc cref="ICategoryRepository" />
internal sealed class CategoryRepository : ICategoryRepository
{
    private readonly ProductManagerDbContext _context;

    public CategoryRepository(ProductManagerDbContext context) => _context = context;

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Categories.AnyAsync(category => category.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CategoryDto>> GetAllWithProductCountsAsync(
        CancellationToken cancellationToken = default) =>
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
