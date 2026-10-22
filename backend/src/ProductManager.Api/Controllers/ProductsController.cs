using Microsoft.AspNetCore.Mvc;
using ProductManager.Application.Common;
using ProductManager.Application.Products;
using ProductManager.Application.Products.Dtos;

namespace ProductManager.Api.Controllers;

/// <summary>
/// CRUD and stock operations for the product.
/// </summary>
/// <remarks>
/// Binds the request, delegates to <see cref="IProductService"/>, and picks a status code. Business rules live in the service and
/// domain, and failures are translated centrally by the global exception handler.
/// </remarks>
[ApiController]
[Route("api/products")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService) => _productService = productService;

    /// <summary>Returns a page of products, each including its available stock.</summary>
    /// <param name="query">Optional paging, sorting and filtering options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">A page of products with the total result count.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts(
        [FromQuery] ProductQueryParameters query,
        CancellationToken cancellationToken)
    {
        var products = await _productService.GetProductsAsync(query, cancellationToken);

        return Ok(products);
    }

    /// <summary>Returns a single product with its stock movement history.</summary>
    /// <param name="id">The product's 6-digit identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The requested product.</response>
    /// <response code="404">No product exists with this identifier.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDetailDto>> GetProductById(
        int id,
        CancellationToken cancellationToken)
    {
        var product = await _productService.GetProductByIdAsync(id, cancellationToken);

        return Ok(product);
    }

    /// <summary>Creates a product and allocates a unique 6-digit identifier for it.</summary>
    /// <param name="request">The product to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="201">The product was created.</response>
    /// <response code="400">The payload failed validation.</response>
    /// <response code="404">The referenced category does not exist.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDetailDto>> CreateProduct(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _productService.CreateProductAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetProductById),
            new { id = created.Id },
            created);
    }

    /// <summary>Updates an existing product.</summary>
    /// <param name="id">The product's 6-digit identifier.</param>
    /// <param name="request">The new values for the product.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The updated product.</response>
    /// <response code="400">The payload failed validation.</response>
    /// <response code="404">No product exists with this identifier, or the category is unknown.</response>
    /// <response code="409">The product was changed concurrently by another request.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDetailDto>> UpdateProduct(
        int id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _productService.UpdateProductAsync(id, request, cancellationToken);

        return Ok(updated);
    }

    /// <summary>
    /// Deletes a product.
    /// </summary>
    /// <param name="id">The product's 6-digit identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="204">The product was deleted.</response>
    /// <response code="404">No product exists with this identifier.</response>
    /// <response code="409">The product was changed concurrently by another request.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteProduct(int id, CancellationToken cancellationToken)
    {
        await _productService.DeleteProductAsync(id, cancellationToken);

        return NoContent();
    }

    /// <summary>Decreases a product's stock by the given quantity.</summary>
    /// <param name="id">The product's 6-digit identifier.</param>
    /// <param name="quantity">Units to remove. Must be greater than zero.</param>
    /// <param name="request">Optional body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The stock level before and after the change.</response>
    /// <response code="400">The quantity was zero or negative.</response>
    /// <response code="404">No product exists with this identifier.</response>
    /// <response code="409">
    /// The product does not hold enough stock, or kept being changed concurrently by other requests.
    /// </response>
    [HttpPost("{id:int}/decrement-stock/{quantity:int}")]
    [ProducesResponseType(typeof(StockOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockOperationResult>> DecrementStock(
        int id,
        int quantity,
        [FromBody] StockAdjustmentRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _productService.DecrementStockAsync(id, quantity, request?.Reason, cancellationToken);

        return Ok(result);
    }

    /// <summary>Increases a product's stock by the given quantity.</summary>
    /// <param name="id">The product's 6-digit identifier.</param>
    /// <param name="quantity">Units to add. Must be greater than zero.</param>
    /// <param name="request">Optional body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The stock level before and after the change.</response>
    /// <response code="400">The quantity was zero or negative.</response>
    /// <response code="404">No product exists with this identifier.</response>
    /// <response code="409">The product kept being changed concurrently by other requests.</response>
    [HttpPost("{id:int}/add-to-stock/{quantity:int}")]
    [ProducesResponseType(typeof(StockOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockOperationResult>> AddToStock(
        int id,
        int quantity,
        [FromBody] StockAdjustmentRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _productService.AddToStockAsync(id, quantity, request?.Reason, cancellationToken);

        return Ok(result);
    }

    /// <summary>Finds products whose name contains the given text, case-insensitively.</summary>
    /// <param name="name">Partial name to match. Required.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Matching products, each including its available stock.</response>
    /// <response code="400">No search term was supplied.</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> SearchProducts(
        [FromQuery] string? name,
        CancellationToken cancellationToken)
    {
        var matches = await _productService.SearchByNameAsync(name ?? string.Empty, cancellationToken);

        return Ok(matches);
    }

    /// <summary>Returns products whose stock sits within an inclusive range.</summary>
    /// <param name="min">Lower bound, inclusive. Omit for no lower bound.</param>
    /// <param name="max">Upper bound, inclusive. Omit for no upper bound.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Matching products, ordered by stock level.</response>
    /// <response code="400">A bound was negative, or the minimum exceeded the maximum.</response>
    [HttpGet("stock-level")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetProductsByStockLevel(
        [FromQuery] int? min,
        [FromQuery] int? max,
        CancellationToken cancellationToken)
    {
        var matches = await _productService.GetByStockLevelAsync(min, max, cancellationToken);

        return Ok(matches);
    }
}
