using FluentValidation;
using FluentValidation.Results;
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

    /// <summary>How many times a stock change is attempted before a concurrency conflict is reported.</summary>
    private const int MaxStockWriteAttempts = 3;

    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IProductRepository products,
        ICategoryRepository categories,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ILogger<ProductService> logger)
    {
        _products = products;
        _categories = categories;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<PagedResult<ProductDto>> GetProductsAsync(
        ProductQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var page = await _products.GetPageAsync(query, cancellationToken);

        return page.Map(product => product.ToDto());
    }

    public async Task<ProductDetailDto> GetProductByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _products.GetByIdWithMovementsAsync(id, cancellationToken)
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

        _products.Add(product);

        // The identifier is assigned by the database's sequence-backed column default, so it is
        // only known once this returns.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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

        var product = await _products.GetByIdWithMovementsAsync(id, cancellationToken)
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

        // A conflict is reported rather than retried: replaying an edit over someone else's would
        // silently discard their change.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated product {ProductId}.", id);

        return await GetProductByIdAsync(id, cancellationToken);
    }

    public async Task DeleteProductAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _products.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Product", id);

        product.SoftDelete(_timeProvider);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

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

        var matches = await _products.SearchByNameAsync(name, SearchResultLimit, cancellationToken);

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

        var matches = await _products.GetByStockLevelAsync(minimum, maximum, cancellationToken);

        return matches.ToDtoList();
    }

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
            var product = await _products.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException("Product", id);

            var previousStock = product.StockQuantity;
            change(product);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return BuildStockResult(product, previousStock);
            }
            catch (ConcurrencyConflictException) when (attempt < MaxStockWriteAttempts)
            {
                _logger.LogInformation(
                    "Stock change on product {ProductId} conflicted with another request; retrying (attempt {Attempt}).",
                    id,
                    attempt);

                _unitOfWork.DiscardChanges();
            }
        }
    }

    private async Task EnsureCategoryExistsAsync(int categoryId, CancellationToken cancellationToken)
    {
        if (!await _categories.ExistsAsync(categoryId, cancellationToken))
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
