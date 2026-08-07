namespace ProductManager.Application.Products.Validators;

/// <summary>
/// Limits and predicates shared by the create and update validators, so the two cannot drift apart.
/// </summary>
public static class ProductValidationRules
{
    /// <summary>Guards against values that would overflow the money column's precision.</summary>
    public const decimal MaxPrice = 9_999_999.99m;

    /// <summary>Sanity ceiling on stock figures; well above any realistic catalogue quantity.</summary>
    public const int MaxStock = 1_000_000;

    /// <summary>
    /// True when the value has no more than two decimal places. Prices are stored as
    /// decimal(18,2), so anything finer would be silently rounded on save.
    /// </summary>
    public static bool HasAtMostTwoDecimals(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero) == value;
}
