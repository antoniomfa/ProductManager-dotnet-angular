using ProductManager.Application.Products.Dtos;

namespace ProductManager.IntegrationTests.Support;

/// <summary>
/// Builds valid request payloads, so each test states only the field it is exercising.
/// </summary>
internal static class ProductRequestFactory
{
    private static int _sequence;

    /// <summary>A name unique to this test run, for tests that need to find their own product back.</summary>
    public static string UniqueName(string prefix = "Test Product") =>
        $"{prefix} {Interlocked.Increment(ref _sequence):D5}";

    public static CreateProductRequest Create(
        string? name = null,
        decimal price = 49.99m,
        int initialStock = 10,
        int lowStockThreshold = 3,
        int categoryId = 1) => new()
        {
            Name = name ?? UniqueName(),
            Description = "Created by the integration suite.",
            Price = price,
            InitialStock = initialStock,
            LowStockThreshold = lowStockThreshold,
            CategoryId = categoryId,
        };

    public static UpdateProductRequest Update(
        string? name = null,
        decimal price = 59.99m,
        int lowStockThreshold = 4,
        int categoryId = 1,
        int? stockQuantity = null) => new()
        {
            Name = name ?? UniqueName("Updated Product"),
            Description = "Updated by the integration suite.",
            Price = price,
            LowStockThreshold = lowStockThreshold,
            CategoryId = categoryId,
            StockQuantity = stockQuantity,
        };
}
