using ProductManager.Domain.Exceptions;

namespace ProductManager.Domain.Entities;

/// <summary>
/// A grouping a product belongs to. Gives the schema a genuine relationship and lets the
/// catalogue be filtered by department.
/// </summary>
public class Category
{
    public const int NameMaxLength = 80;
    public const int DescriptionMaxLength = 400;

    private readonly List<Product> _products = [];

    private Category()
    {
        // Required by EF Core's materialisation.
        Name = null!;
    }

    public Category(string name, string? description = null)
    {
        Name = NormaliseName(name);
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public int Id { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();

    public void Rename(string name) => Name = NormaliseName(name);

    public void Describe(string? description) =>
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static string NormaliseName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidProductStateException("Category name is required.");
        }

        var trimmed = name.Trim();
        return trimmed.Length > NameMaxLength
            ? throw new InvalidProductStateException($"Category name cannot exceed {NameMaxLength} characters.")
            : trimmed;
    }
}
