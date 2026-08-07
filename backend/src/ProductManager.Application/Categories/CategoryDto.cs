namespace ProductManager.Application.Categories;

/// <summary>Category representation, used to populate selection controls in clients.</summary>
public sealed record CategoryDto
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    /// <summary>Number of live (non-deleted) products in this category.</summary>
    public required int ProductCount { get; init; }
}
