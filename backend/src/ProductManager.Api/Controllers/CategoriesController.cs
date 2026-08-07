using Microsoft.AspNetCore.Mvc;
using ProductManager.Application.Categories;

namespace ProductManager.Api.Controllers;

/// <summary>
/// Read-only access to product categories.
/// </summary>
/// <remarks>
/// Beyond the required product endpoints, but the client needs it: without a category list the
/// create and edit forms would have to hard-code identifiers that only exist in seed data.
/// </remarks>
[ApiController]
[Route("api/categories")]
[Produces("application/json")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categories;

    public CategoriesController(ICategoryService categories) => _categories = categories;

    /// <summary>Returns every category with its live product count.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">All categories, ordered by name.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetCategories(CancellationToken cancellationToken)
    {
        var categories = await _categories.GetCategoriesAsync(cancellationToken);

        return Ok(categories);
    }
}
