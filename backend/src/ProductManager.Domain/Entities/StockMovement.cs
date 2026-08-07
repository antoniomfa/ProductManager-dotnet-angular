using ProductManager.Domain.Enums;

namespace ProductManager.Domain.Entities;

/// <summary>
/// An immutable audit record of a single change to a product's stock level.
/// Movements are only ever appended, never edited, so the running history can always be
/// reconciled against the product's current <see cref="Product.StockQuantity"/>.
/// </summary>
public class StockMovement
{
    public const int ReasonMaxLength = 250;

    private StockMovement()
    {
        // Required by EF Core's materialisation.
    }

    internal StockMovement(
        StockMovementType type,
        int quantityChange,
        int resultingStock,
        string? reason,
        DateTimeOffset occurredAt)
    {
        Type = type;
        QuantityChange = quantityChange;
        ResultingStock = resultingStock;
        Reason = Truncate(reason);
        OccurredAt = occurredAt;
    }

    public long Id { get; private set; }

    public int ProductId { get; private set; }

    public Product? Product { get; private set; }

    public StockMovementType Type { get; private set; }

    /// <summary>Signed delta: positive for additions, negative for removals.</summary>
    public int QuantityChange { get; private set; }

    /// <summary>The product's stock level immediately after this movement was applied.</summary>
    public int ResultingStock { get; private set; }

    public string? Reason { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    private static string? Truncate(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return null;
        }

        var trimmed = reason.Trim();
        return trimmed.Length <= ReasonMaxLength ? trimmed : trimmed[..ReasonMaxLength];
    }
}
