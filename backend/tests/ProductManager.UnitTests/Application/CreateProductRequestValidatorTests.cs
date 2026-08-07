using ProductManager.Application.Products.Dtos;
using ProductManager.Application.Products.Validators;

namespace ProductManager.UnitTests.Application;

/// <summary>
/// Covers the structural rules applied to a create payload before any database work happens.
/// </summary>
public sealed class CreateProductRequestValidatorTests
{
    private readonly CreateProductRequestValidator _validator = new();

    private static CreateProductRequest Valid => new()
    {
        Name = "Aurora Over-Ear Headphones",
        Description = "Closed-back wireless headphones.",
        Price = 249.99m,
        InitialStock = 20,
        LowStockThreshold = 5,
        CategoryId = 1,
    };

    [Fact]
    public void A_well_formed_request_passes()
    {
        _validator.Validate(Valid).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Name_is_required(string name)
    {
        ShouldFail(Valid with { Name = name }, nameof(CreateProductRequest.Name));
    }

    [Fact]
    public void Name_has_a_maximum_length()
    {
        ShouldFail(Valid with { Name = new string('a', 151) }, nameof(CreateProductRequest.Name));
    }

    [Fact]
    public void Price_cannot_be_negative()
    {
        ShouldFail(Valid with { Price = -0.01m }, nameof(CreateProductRequest.Price));
    }

    [Fact]
    public void Price_cannot_carry_more_precision_than_the_column_stores()
    {
        // Allowed through, it would be silently rounded on save and the client would never know.
        ShouldFail(Valid with { Price = 10.005m }, nameof(CreateProductRequest.Price));
    }

    [Fact]
    public void Price_of_zero_is_allowed_for_giveaway_items()
    {
        _validator.Validate(Valid with { Price = 0m }).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Initial_stock_cannot_be_negative()
    {
        ShouldFail(Valid with { InitialStock = -1 }, nameof(CreateProductRequest.InitialStock));
    }

    [Fact]
    public void Low_stock_threshold_cannot_be_negative()
    {
        ShouldFail(Valid with { LowStockThreshold = -1 }, nameof(CreateProductRequest.LowStockThreshold));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void A_category_must_be_selected(int categoryId)
    {
        ShouldFail(Valid with { CategoryId = categoryId }, nameof(CreateProductRequest.CategoryId));
    }

    private void ShouldFail(CreateProductRequest request, string expectedProperty)
    {
        var result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(failure => failure.PropertyName == expectedProperty);
    }
}
