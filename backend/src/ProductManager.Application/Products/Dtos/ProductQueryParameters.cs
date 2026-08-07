namespace ProductManager.Application.Products.Dtos;

/// <summary>
/// Filtering, sorting and paging options for the product listing endpoint.
/// All properties are optional; the defaults return the first page ordered by name.
/// </summary>
public sealed record ProductQueryParameters
{
    /// <summary>Upper bound on page size, to stop a client requesting the entire catalogue in one call.</summary>
    public const int MaxPageSize = 100;

    private readonly int _page = 1;
    private readonly int _pageSize = 20;

    /// <summary>1-based page number. Values below 1 are coerced to 1.</summary>
    public int Page
    {
        get => _page;
        init => _page = value < 1 ? 1 : value;
    }

    /// <summary>Items per page, clamped to <see cref="MaxPageSize"/>.</summary>
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value switch
        {
            < 1 => 1,
            > MaxPageSize => MaxPageSize,
            _ => value,
        };
    }

    /// <summary>Field to order by: name, price, stock, created or id. Unknown values fall back to name.</summary>
    public string? SortBy { get; init; }

    public bool SortDescending { get; init; }

    /// <summary>Restricts results to a single category.</summary>
    public int? CategoryId { get; init; }

    /// <summary>Free-text filter applied to the product name.</summary>
    public string? Search { get; init; }
}
