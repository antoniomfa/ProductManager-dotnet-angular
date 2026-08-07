namespace ProductManager.Domain.Enums;

/// <summary>
/// Derived view of a product's stock level, used to drive at-a-glance indicators in clients
/// so every consumer applies the same thresholds rather than reimplementing them.
/// </summary>
public enum StockStatus
{
    /// <summary>No units available.</summary>
    OutOfStock = 0,

    /// <summary>At or below the product's configured low-stock threshold.</summary>
    LowStock = 1,

    /// <summary>Comfortably above the low-stock threshold.</summary>
    InStock = 2,
}
