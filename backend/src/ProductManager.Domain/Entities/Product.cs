using ProductManager.Domain.Enums;
using ProductManager.Domain.Exceptions;

namespace ProductManager.Domain.Entities;

/// <summary>
/// The catalogue's aggregate root. Stock is mutated only through the methods on this type,
/// so the running <see cref="StockMovements"/> audit trail can never drift out of step with
/// <see cref="StockQuantity"/>, and stock can never go negative.
/// </summary>
public class Product
{
    /// <summary>Lowest valid product identifier. Together with <see cref="MaxIdentifier"/> this pins IDs to exactly six digits.</summary>
    public const int MinIdentifier = 100_000;

    /// <summary>Highest valid product identifier.</summary>
    public const int MaxIdentifier = 999_999;

    public const int NameMaxLength = 150;
    public const int DescriptionMaxLength = 1000;

    private readonly List<StockMovement> _stockMovements = [];

    private Product()
    {
        // Required by EF Core's materialisation.
        Name = null!;
    }

    private Product(
        string name,
        string? description,
        decimal price,
        int stockQuantity,
        int lowStockThreshold,
        int categoryId,
        DateTimeOffset createdAt)
    {
        Name = ValidateName(name);
        Description = NormaliseDescription(description);
        Price = ValidatePrice(price);
        StockQuantity = ValidateStock(stockQuantity);
        LowStockThreshold = ValidateLowStockThreshold(lowStockThreshold);
        CategoryId = categoryId;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    /// <summary>
    /// Unique 6-digit identifier, allocated by the database from a sequence on insert.
    /// Zero until the product has been saved.
    /// </summary>
    public int Id { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public decimal Price { get; private set; }

    /// <summary>Units currently available to sell. Never negative.</summary>
    public int StockQuantity { get; private set; }

    /// <summary>At or below this level the product reports <see cref="StockStatus.LowStock"/>.</summary>
    public int LowStockThreshold { get; private set; }

    public int CategoryId { get; private set; }

    public Category? Category { get; private set; }

    /// <summary>
    /// Soft-delete marker. Deleted products are hidden by a global query filter but retain
    /// their movement history, so stock reporting over past periods stays accurate.
    /// </summary>
    public bool IsDeleted { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<StockMovement> StockMovements => _stockMovements.AsReadOnly();

    /// <summary>Classification of the current stock level against the product's threshold.</summary>
    public StockStatus StockStatus => StockQuantity switch
    {
        <= 0 => StockStatus.OutOfStock,
        var quantity when quantity <= LowStockThreshold => StockStatus.LowStock,
        _ => StockStatus.InStock,
    };

    /// <summary>
    /// Creates a product.
    /// </summary>
    /// <remarks>
    /// No identifier is accepted or generated here. The database allocates it from a sequence
    /// via a column default, which is what keeps identifiers unique across concurrent API
    /// instances — a guarantee no in-process scheme can make.
    /// </remarks>
    public static Product Create(
        string name,
        string? description,
        decimal price,
        int initialStock,
        int lowStockThreshold,
        int categoryId,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        var now = timeProvider.GetUtcNow();
        var product = new Product(name, description, price, initialStock, lowStockThreshold, categoryId, now);

        if (initialStock > 0)
        {
            product._stockMovements.Add(new StockMovement(
                StockMovementType.InitialStock,
                initialStock,
                initialStock,
                "Initial stock recorded at product creation.",
                now));
        }

        return product;
    }

    /// <summary>
    /// Applies an edit to the product's descriptive fields. Stock is deliberately excluded:
    /// it moves only through <see cref="AddStock"/>, <see cref="RemoveStock"/> and
    /// <see cref="AdjustStockTo"/> so that every change leaves an audit trail.
    /// </summary>
    public void UpdateDetails(
        string name,
        string? description,
        decimal price,
        int lowStockThreshold,
        int categoryId,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        Name = ValidateName(name);
        Description = NormaliseDescription(description);
        Price = ValidatePrice(price);
        LowStockThreshold = ValidateLowStockThreshold(lowStockThreshold);
        CategoryId = categoryId;
        UpdatedAt = timeProvider.GetUtcNow();
    }

    /// <summary>Increases available stock and records the movement.</summary>
    public void AddStock(int quantity, string? reason, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        RequirePositiveQuantity(quantity);

        StockQuantity += quantity;
        RecordMovement(StockMovementType.Increment, quantity, reason, timeProvider);
    }

    /// <summary>
    /// Decreases available stock and records the movement.
    /// </summary>
    /// <exception cref="InsufficientStockException">
    /// Thrown when <paramref name="quantity"/> exceeds the units on hand. Stock is left untouched.
    /// </exception>
    public void RemoveStock(int quantity, string? reason, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        RequirePositiveQuantity(quantity);

        if (quantity > StockQuantity)
        {
            throw new InsufficientStockException(Id, StockQuantity, quantity);
        }

        StockQuantity -= quantity;
        RecordMovement(StockMovementType.Decrement, -quantity, reason, timeProvider);
    }

    /// <summary>
    /// Overwrites stock with an absolute figure, as a stock take would. Records the signed
    /// difference so the audit trail still reconciles.
    /// </summary>
    public void AdjustStockTo(int newQuantity, string? reason, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        var validated = ValidateStock(newQuantity);
        var delta = validated - StockQuantity;

        if (delta == 0)
        {
            return;
        }

        StockQuantity = validated;
        RecordMovement(StockMovementType.Adjustment, delta, reason, timeProvider);
    }

    /// <summary>Marks the product deleted without removing its row or its movement history.</summary>
    public void SoftDelete(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        IsDeleted = true;
        UpdatedAt = timeProvider.GetUtcNow();
    }

    private void RecordMovement(StockMovementType type, int signedChange, string? reason, TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();

        _stockMovements.Add(new StockMovement(type, signedChange, StockQuantity, reason, now));
        UpdatedAt = now;
    }

    private static void RequirePositiveQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new InvalidProductStateException("Stock movement quantity must be greater than zero.");
        }
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidProductStateException("Product name is required.");
        }

        var trimmed = name.Trim();
        return trimmed.Length > NameMaxLength
            ? throw new InvalidProductStateException($"Product name cannot exceed {NameMaxLength} characters.")
            : trimmed;
    }

    private static string? NormaliseDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var trimmed = description.Trim();
        return trimmed.Length > DescriptionMaxLength
            ? throw new InvalidProductStateException($"Product description cannot exceed {DescriptionMaxLength} characters.")
            : trimmed;
    }

    private static decimal ValidatePrice(decimal price) =>
        price < 0m
            ? throw new InvalidProductStateException("Product price cannot be negative.")
            : decimal.Round(price, 2, MidpointRounding.AwayFromZero);

    private static int ValidateStock(int stock) =>
        stock < 0
            ? throw new InvalidProductStateException("Stock quantity cannot be negative.")
            : stock;

    private static int ValidateLowStockThreshold(int threshold) =>
        threshold < 0
            ? throw new InvalidProductStateException("Low-stock threshold cannot be negative.")
            : threshold;
}
