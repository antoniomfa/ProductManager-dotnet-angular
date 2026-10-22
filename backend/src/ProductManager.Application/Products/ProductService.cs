using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductManager.Application.Abstractions;
using ProductManager.Application.Common;
using ProductManager.Application.Exceptions;
using ProductManager.Application.Products.Dtos;
using ProductManager.Application.Products.Mapping;
using ProductManager.Domain.Entities;

namespace ProductManager.Application.Products;

/// <inheritdoc cref="IProductService" />
public sealed class ProductService : IProductService
{
    /// <summary>Ceiling on search results, so a one-character query cannot return the whole catalogue.</summary>
    private const int SearchResultLimit = 100;

    /// <summary>Escape character for LIKE patterns, so user input cannot act as a wildcard.</summary>
    private const string LikeEscape = "\\";

    /// <summary>How many times a stock change is attempted before a concurrency conflict is reported.</summary>
    private const int MaxStockWriteAttempts = 3;

    private readonly IApplicationDbContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IApplicationDbContext context,
        TimeProvider timeProvider,
        ILogger<ProductService> logger)
    {
        _context = context;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<PagedResult<ProductDto>> GetProductsAsync(
        ProductQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var products = _context.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .AsQueryable();

        if (query.CategoryId is { } categoryId)
        {
            products = products.Where(product => product.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{EscapeLikePattern(query.Search.Trim())}%";

            products = products.Where(product => EF.Functions.Like(product.Name, pattern, LikeEscape));
        }

        // Counted before paging so the client learns the size of the full result set.
        var totalCount = await products.CountAsync(cancellationToken);

        var items = await ApplySort(products, query.SortBy, query.SortDescending)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductDto>(items.ToDtoList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<ProductDetailDto> GetProductByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await FindWithMovementsAsync(id, cancellationToken)
            ?? throw new NotFoundException("Product", id);

        return product.ToDetailDto();
    }

    public async Task<ProductDetailDto> CreateProductAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);

        var product = Product.Create(
            request.Name,
            request.Description,
            request.Price,
            request.InitialStock,
            request.LowStockThreshold,
            request.CategoryId,
            _timeProvider);

        _context.Products.Add(product);

        // The identifier is assigned by the database's sequence-backed column default, so it is
        // only known once this returns.
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created product {ProductId} with initial stock {InitialStock}.",
            product.Id,
            product.StockQuantity);

        // Re-read so the response carries the category name and database-assigned movement IDs.
        return await GetProductByIdAsync(product.Id, cancellationToken);
    }

    public async Task<ProductDetailDto> UpdateProductAsync(
        int id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = await FindWithMovementsAsync(id, cancellationToken)
            ?? throw new NotFoundException("Product", id);

        await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);

        product.UpdateDetails(
            request.Name,
            request.Description,
            request.Price,
            request.LowStockThreshold,
            request.CategoryId,
            _timeProvider);

        if (request.StockQuantity.HasValue)
        {
            product.AdjustStockTo(
                request.StockQuantity.Value,
                "Stock take applied through product update.",
                _timeProvider);
        }

        await SaveOrThrowConflictAsync(id, cancellationToken);

        _logger.LogInformation("Updated product {ProductId}.", id);

        return await GetProductByIdAsync(id, cancellationToken);
    }

    public async Task DeleteProductAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await FindAsync(id, cancellationToken)
            ?? throw new NotFoundException("Product", id);

        product.SoftDelete(_timeProvider);

        await SaveOrThrowConflictAsync(id, cancellationToken);

        _logger.LogInformation("Soft-deleted product {ProductId}.", id);
    }

    public Task<StockOperationResult> AddToStockAsync(
        int id,
        int quantity,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        RequirePositiveQuantity(quantity);

        return ChangeStockAsync(
            id,
            product => product.AddStock(quantity, reason, _timeProvider),
            cancellationToken);
    }

    public Task<StockOperationResult> DecrementStockAsync(
        int id,
        int quantity,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        RequirePositiveQuantity(quantity);

        return ChangeStockAsync(
            id,
            product => product.RemoveStock(quantity, reason, _timeProvider),
            cancellationToken);
    }

    public async Task<IReadOnlyList<ProductDto>> SearchByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw BuildValidationException(nameof(name), "A search term is required.");
        }

        var pattern = $"%{EscapeLikePattern(name.Trim())}%";

        var matches = await _context.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Where(product => EF.Functions.Like(product.Name, pattern, LikeEscape))
            .OrderBy(product => product.Name)
            .Take(SearchResultLimit)
            .ToListAsync(cancellationToken);

        return matches.ToDtoList();
    }

    public async Task<IReadOnlyList<ProductDto>> GetByStockLevelAsync(
        int? minimum,
        int? maximum,
        CancellationToken cancellationToken = default)
    {
        if (minimum is < 0)
        {
            throw BuildValidationException("min", "Minimum stock level cannot be negative.");
        }

        if (maximum is < 0)
        {
            throw BuildValidationException("max", "Maximum stock level cannot be negative.");
        }

        if (minimum.HasValue && maximum.HasValue && minimum > maximum)
        {
            throw BuildValidationException("min", "Minimum stock level cannot be greater than the maximum.");
        }

        var products = _context.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .AsQueryable();

        if (minimum is { } lowerBound)
        {
            products = products.Where(product => product.StockQuantity >= lowerBound);
        }

        if (maximum is { } upperBound)
        {
            products = products.Where(product => product.StockQuantity <= upperBound);
        }

        var matches = await products
            .OrderBy(product => product.StockQuantity)
            .ThenBy(product => product.Name)
            .ToListAsync(cancellationToken);

        return matches.ToDtoList();
    }

    /// <summary>Loads a tracked product, or null when it does not exist or was deleted.</summary>
    private Task<Product?> FindAsync(int id, CancellationToken cancellationToken) =>
        _context.Products
            .Include(product => product.Category)
            .FirstOrDefaultAsync(product => product.Id == id, cancellationToken);

    /// <summary>As <see cref="FindAsync"/>, but also loads the stock movement history.</summary>
    private Task<Product?> FindWithMovementsAsync(int id, CancellationToken cancellationToken) =>
        _context.Products
            .Include(product => product.Category)
            .Include(product => product.StockMovements)
            .FirstOrDefaultAsync(product => product.Id == id, cancellationToken);

    /// <summary>
    /// Applies a stock change and saves it, retrying when another request changed the product in
    /// between.
    /// </summary>
    /// <remarks>
    /// A stock change is a delta, so replaying it on freshly read figures is safe: the domain
    /// re-checks the rules (for example, enough units on hand) against the current level each time.
    /// </remarks>
    private async Task<StockOperationResult> ChangeStockAsync(
        int id,
        Action<Product> change,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var product = await FindAsync(id, cancellationToken)
                ?? throw new NotFoundException("Product", id);

            var previousStock = product.StockQuantity;
            change(product);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);

                return BuildStockResult(product, previousStock);
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxStockWriteAttempts)
            {
                _logger.LogInformation(
                    "Stock change on product {ProductId} conflicted with another request; retrying (attempt {Attempt}).",
                    id,
                    attempt);

                _context.ChangeTracker.Clear();
            }
            catch (DbUpdateConcurrencyException exception)
            {
                throw new ConcurrencyConflictException("Product", id, exception);
            }
        }
    }

    /// <summary>
    /// Saves an edit, reporting a conflict rather than overwriting a change made by another request.
    /// </summary>
    private async Task SaveOrThrowConflictAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException("Product", id, exception);
        }
    }

    private async Task EnsureCategoryExistsAsync(int categoryId, CancellationToken cancellationToken)
    {
        var exists = await _context.Categories
            .AnyAsync(category => category.Id == categoryId, cancellationToken);

        if (!exists)
        {
            throw new NotFoundException("Category", categoryId);
        }
    }

    private static StockOperationResult BuildStockResult(Product product, int previousStock) => new()
    {
        ProductId = product.Id,
        Name = product.Name,
        PreviousStock = previousStock,
        NewStock = product.StockQuantity,
        StockStatus = product.StockStatus,
    };

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

    private static void RequirePositiveQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw BuildValidationException("quantity", "Quantity must be greater than zero.");
        }
    }

    private static ValidationException BuildValidationException(string property, string message) =>
        new([new ValidationFailure(property, message)]);
}
