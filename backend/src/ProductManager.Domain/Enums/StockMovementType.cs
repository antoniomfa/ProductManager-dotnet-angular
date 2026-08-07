namespace ProductManager.Domain.Enums;

/// <summary>
/// Classifies why a product's stock level changed. Persisted as an integer so the
/// numeric values are part of the database contract and must not be reordered.
/// </summary>
public enum StockMovementType
{
    /// <summary>Stock recorded when the product was first created.</summary>
    InitialStock = 1,

    /// <summary>Stock added through the add-to-stock endpoint.</summary>
    Increment = 2,

    /// <summary>Stock removed through the decrement-stock endpoint.</summary>
    Decrement = 3,

    /// <summary>Stock overwritten to an absolute value, typically by an update or stock take.</summary>
    Adjustment = 4,
}
