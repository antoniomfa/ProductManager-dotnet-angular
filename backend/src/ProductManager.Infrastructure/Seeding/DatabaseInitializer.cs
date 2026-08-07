using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductManager.Domain.Entities;
using ProductManager.Infrastructure.Persistence;

namespace ProductManager.Infrastructure.Seeding;

/// <inheritdoc cref="IDatabaseInitializer" />
public sealed class DatabaseInitializer : IDatabaseInitializer
{
    private readonly ProductManagerDbContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        ProductManagerDbContext context,
        TimeProvider timeProvider,
        ILogger<DatabaseInitializer> logger)
    {
        _context = context;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task InitialiseAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.MigrateAsync(cancellationToken);

        _logger.LogInformation("Database schema is up to date.");
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedCategoriesAsync(cancellationToken);
        await SeedProductsAsync(cancellationToken);
    }

    private async Task SeedCategoriesAsync(CancellationToken cancellationToken)
    {
        var existingNames = await _context.Categories
            .Select(category => category.Name)
            .ToListAsync(cancellationToken);

        var missing = SeedCatalogue.Categories
            .Where(seed => !existingNames.Contains(seed.Name, StringComparer.OrdinalIgnoreCase))
            .Select(seed => new Category(seed.Name, seed.Description))
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        _context.Categories.AddRange(missing);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {CategoryCount} categories.", missing.Count);
    }

    private async Task SeedProductsAsync(CancellationToken cancellationToken)
    {
        if (await _context.Products.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Catalogue already contains products; skipping product seeding.");
            return;
        }

        var categoryIdsByName = await _context.Categories
            .ToDictionaryAsync(category => category.Name, category => category.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var products = new List<Product>(SeedCatalogue.Products.Count);

        foreach (var seed in SeedCatalogue.Products)
        {
            if (!categoryIdsByName.TryGetValue(seed.CategoryName, out var categoryId))
            {
                _logger.LogWarning(
                    "Skipping seed product '{ProductName}': category '{CategoryName}' was not found.",
                    seed.Name,
                    seed.CategoryName);
                continue;
            }

            // Identifiers come from the same sequence-backed column default that runtime creates
            // use, so seeded products are genuinely 6-digit and the sequence is left consistent.
            products.Add(Product.Create(
                seed.Name,
                seed.Description,
                seed.Price,
                seed.Stock,
                seed.LowStockThreshold,
                categoryId,
                _timeProvider));
        }

        _context.Products.AddRange(products);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {ProductCount} products.", products.Count);
    }
}
