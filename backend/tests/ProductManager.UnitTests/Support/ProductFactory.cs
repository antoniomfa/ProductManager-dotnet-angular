using ProductManager.Domain.Entities;

namespace ProductManager.UnitTests.Support;

/// <summary>
/// Builds valid products for tests, so each test only states the field it actually cares about
/// and stays readable when the constructor grows another parameter.
/// </summary>
internal static class ProductFactory
{
    public const int DefaultId = 123456;

    public static Product Create(
        string name = "Aurora Over-Ear Headphones",
        string? description = "Closed-back wireless headphones.",
        decimal price = 249.99m,
        int initialStock = 20,
        int lowStockThreshold = 5,
        int categoryId = 1,
        int id = DefaultId,
        TimeProvider? timeProvider = null)
    {
        var product = Product.Create(
            name,
            description,
            price,
            initialStock,
            lowStockThreshold,
            categoryId,
            timeProvider ?? new FixedTimeProvider());

        AssignId(product, id);
        return product;
    }

    /// <summary>
    /// Stamps an identifier onto an unsaved product.
    /// </summary>
    /// <remarks>
    /// In production the database assigns this from a sequence on insert, so there is deliberately
    /// no public setter — the identifier is not the application's to choose. Tests still need a
    /// stable, realistic value to assert against, and reflection keeps that need confined here
    /// rather than widening the production API to accommodate it.
    /// </remarks>
    private static void AssignId(Product product, int id) =>
        typeof(Product)
            .GetProperty(nameof(Product.Id))!
            .SetValue(product, id);
}
