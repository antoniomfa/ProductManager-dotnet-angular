namespace ProductManager.Application.Common;

/// <summary>
/// A single page of results plus the metadata a client needs to render pagination controls
/// without issuing a second count request.
/// </summary>
/// <typeparam name="T">The element type of the page.</typeparam>
public sealed record PagedResult<T>
{
    public PagedResult(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public IReadOnlyList<T> Items { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;

    /// <summary>Projects each item through <paramref name="selector"/>, preserving the page metadata.</summary>
    public PagedResult<TOut> Map<TOut>(Func<T, TOut> selector) =>
        new([.. Items.Select(selector)], Page, PageSize, TotalCount);
}
