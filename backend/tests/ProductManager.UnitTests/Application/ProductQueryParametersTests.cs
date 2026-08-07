using ProductManager.Application.Products.Dtos;

namespace ProductManager.UnitTests.Application;

/// <summary>
/// Covers the coercion applied to listing query strings, which come straight from a URL and so
/// cannot be trusted to be sensible.
/// </summary>
public sealed class ProductQueryParametersTests
{
    [Fact]
    public void Defaults_return_the_first_page()
    {
        var parameters = new ProductQueryParameters();

        parameters.Page.ShouldBe(1);
        parameters.PageSize.ShouldBe(20);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void A_page_below_one_is_coerced_to_the_first_page(int page)
    {
        new ProductQueryParameters { Page = page }.Page.ShouldBe(1);
    }

    [Fact]
    public void An_oversized_page_is_capped_so_one_request_cannot_pull_the_whole_catalogue()
    {
        new ProductQueryParameters { PageSize = 10_000 }.PageSize
            .ShouldBe(ProductQueryParameters.MaxPageSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_page_size_below_one_is_coerced_to_one(int pageSize)
    {
        new ProductQueryParameters { PageSize = pageSize }.PageSize.ShouldBe(1);
    }

    [Fact]
    public void A_page_size_within_the_limit_is_left_alone()
    {
        new ProductQueryParameters { PageSize = 50 }.PageSize.ShouldBe(50);
    }
}
