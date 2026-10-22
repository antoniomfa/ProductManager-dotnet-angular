using Microsoft.EntityFrameworkCore;
using ProductManager.Application.Abstractions;
using ProductManager.Application.Common;
using ProductManager.Application.Products.Dtos;
using ProductManager.Domain.Entities;

namespace ProductManager.Infrastructure.Persistence.Repositories;

/// <inheritdoc cref="IProductRepository" />
internal sealed class ProductRepository : IProductRepository
{
    /// <summary>Escape character for LIKE patterns, so user input cannot act as a wildcard.</summary>
    private const string LikeEscape = "\\";

    private readonly ProductManagerDbContext _context;

    public ProductRepository(ProductManagerDbContext context) => _context = context;

    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Products
            .Include(product => product.Category)
            .FirstOrDefaultAsync(product => product.Id == id, cancellationToken);

    public Task<Product?> GetByIdWithMovementsAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Products
            .Include(product => product.Category)
            .Include(product => product.StockMovements)
            .FirstOrDefaultAsync(product => product.Id == id, cancellationToken);

    public async Task<PagedResult<Product>> GetPageAsync(
        ProductQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var products = ReadOnlyProducts();

        if (query.CategoryId is { } categoryId)
        {
            products = products.Where(product => product.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            products = WhereNameContains(products, query.Search);
        }

        // Counted before paging so the client learns the size of the full result set.
        var totalCount = await products.CountAsync(cancellationToken);

        var items = await ApplySort(products, query.SortBy, query.SortDescending)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Product>(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<IReadOnlyList<Product>> SearchByNameAsync(
        string term,
        int maxResults,
        CancellationToken cancellationToken = default) =>
        await WhereNameContains(ReadOnlyProducts(), term)
            .OrderBy(product => product.Name)
            .Take(maxResults)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Product>> GetByStockLevelAsync(
        int? minimum,
        int? maximum,
        CancellationToken cancellationToken = default)
    {
        var products = ReadOnlyProducts();

        if (minimum is { } lowerBound)
        {
            products = products.Where(product => product.StockQuantity >= lowerBound);
        }

        if (maximum is { } upperBound)
        {
            products = products.Where(product => product.StockQuantity <= upperBound);
        }

        return await products
            .OrderBy(product => product.StockQuantity)
            .ThenBy(product => product.Name)
            .ToListAsync(cancellationToken);
    }

    public void Add(Product product) => _context.Products.Add(product);

    private IQueryable<Product> ReadOnlyProducts() =>
        _context.Products
            .AsNoTracking()
            .Include(product => product.Category);

    private static IQueryable<Product> WhereNameContains(IQueryable<Product> products, string term)
    {
        var pattern = $"%{EscapeLikePattern(term.Trim())}%";

        return products.Where(product => EF.Functions.Like(product.Name, pattern, LikeEscape));
    }

    private static IQueryable<Product> ApplySort(IQueryable<Product> products, string? sortBy, bool descending) =>
        (sortBy?.Trim().ToUpperInvariant()) switch
        {
            "PRICE" => descending
                ? products.OrderByDescending(product => product.Price).ThenBy(product => product.Id)
                : products.OrderBy(product => product.Price).ThenBy(product => product.Id),

            "STOCK" or "STOCKQUANTITY" => descending
                ? products.OrderByDescending(product => product.StockQuantity).ThenBy(product => product.Id)
                : products.OrderBy(product => product.StockQuantity).ThenBy(product => product.Id),

            "CREATED" or "CREATEDAT" => descending
                ? products.OrderByDescending(product => product.CreatedAt).ThenBy(product => product.Id)
                : products.OrderBy(product => product.CreatedAt).ThenBy(product => product.Id),

            "ID" => descending
                ? products.OrderByDescending(product => product.Id)
                : products.OrderBy(product => product.Id),

            // Name is the default, and every branch adds Id as a tie-break so paging stays
            // stable when several rows share a sort value.
            _ => descending
                ? products.OrderByDescending(product => product.Name).ThenBy(product => product.Id)
                : products.OrderBy(product => product.Name).ThenBy(product => product.Id),
        };

    /// <summary>
    /// Neutralises LIKE characters in user input, so a search for "50%" matches the literal text
    /// rather than turning into a wildcard.
    /// </summary>
    private static string EscapeLikePattern(string value) => value
        .Replace(LikeEscape, LikeEscape + LikeEscape, StringComparison.Ordinal)
        .Replace("%", LikeEscape + "%", StringComparison.Ordinal)
        .Replace("_", LikeEscape + "_", StringComparison.Ordinal)
        .Replace("[", LikeEscape + "[", StringComparison.Ordinal);
}
