using System.Net;
using ProductManager.Application.Categories;
using ProductManager.Application.Products.Dtos;
using ProductManager.IntegrationTests.Support;

namespace ProductManager.IntegrationTests.Endpoints;

/// <summary>Covers the search and stock-level query endpoints.</summary>
public sealed class ProductQueryEndpointsTests : ApiTestBase
{
    public ProductQueryEndpointsTests(ProductManagerApiFactory factory)
        : base(factory)
    {
    }

    [SqlServerFact]
    public async Task Search_matches_on_a_partial_name()
    {
        await CreateProductAsync(name: "Quicksilver Desk Lamp");

        var matches = await GetAsync<IReadOnlyList<ProductDto>>("/api/products/search?name=silver");

        matches.ShouldContain(product => product.Name == "Quicksilver Desk Lamp");
    }

    [SqlServerFact]
    public async Task Search_ignores_case()
    {
        await CreateProductAsync(name: "Zephyr Cable Tidy");

        var matches = await GetAsync<IReadOnlyList<ProductDto>>("/api/products/search?name=ZEPHYR");

        matches.ShouldContain(product => product.Name == "Zephyr Cable Tidy");
    }

    [SqlServerFact]
    public async Task Search_results_carry_the_available_stock()
    {
        await CreateProductAsync(name: "Kestrel Cable Clip", initialStock: 17);

        var matches = await GetAsync<IReadOnlyList<ProductDto>>("/api/products/search?name=Kestrel");

        matches.ShouldContain(product => product.Name == "Kestrel Cable Clip" && product.StockQuantity == 17);
    }

    [SqlServerFact]
    public async Task Search_with_no_matches_returns_an_empty_list_rather_than_an_error()
    {
        var matches = await GetAsync<IReadOnlyList<ProductDto>>(
            "/api/products/search?name=nothingmatchesthisstring");

        matches.ShouldBeEmpty();
    }

    [SqlServerFact]
    public async Task Search_requires_a_term()
    {
        var response = await Client.GetAsync(new Uri("/api/products/search?name=", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [SqlServerFact]
    public async Task Search_treats_a_wildcard_character_as_literal_text()
    {
        // Passed through unescaped, "%" would match every product in the catalogue.
        var matches = await GetAsync<IReadOnlyList<ProductDto>>("/api/products/search?name=%25");

        matches.ShouldBeEmpty();
    }

    [SqlServerFact]
    public async Task Stock_level_returns_only_products_inside_the_inclusive_range()
    {
        await CreateProductAsync(name: "Range Probe Empty", initialStock: 0);
        await CreateProductAsync(name: "Range Probe Low", initialStock: 5);
        await CreateProductAsync(name: "Range Probe High", initialStock: 500);

        var matches = await GetAsync<IReadOnlyList<ProductDto>>("/api/products/stock-level?min=0&max=5");

        matches.ShouldContain(product => product.Name == "Range Probe Empty");
        matches.ShouldContain(product => product.Name == "Range Probe Low");
        matches.ShouldNotContain(product => product.Name == "Range Probe High");
        matches.ShouldAllBe(product => product.StockQuantity >= 0 && product.StockQuantity <= 5);
    }

    [SqlServerFact]
    public async Task Stock_level_bounds_are_inclusive_at_both_ends()
    {
        await CreateProductAsync(name: "Boundary Lower", initialStock: 7);
        await CreateProductAsync(name: "Boundary Upper", initialStock: 9);

        var matches = await GetAsync<IReadOnlyList<ProductDto>>("/api/products/stock-level?min=7&max=9");

        matches.ShouldContain(product => product.Name == "Boundary Lower");
        matches.ShouldContain(product => product.Name == "Boundary Upper");
    }

    [SqlServerFact]
    public async Task Stock_level_allows_an_open_ended_upper_bound()
    {
        await CreateProductAsync(name: "Plentiful Probe", initialStock: 900);

        var matches = await GetAsync<IReadOnlyList<ProductDto>>("/api/products/stock-level?min=800");

        matches.ShouldContain(product => product.Name == "Plentiful Probe");
        matches.ShouldAllBe(product => product.StockQuantity >= 800);
    }

    [SqlServerFact]
    public async Task Stock_level_rejects_a_minimum_above_the_maximum()
    {
        var response = await Client.GetAsync(new Uri("/api/products/stock-level?min=50&max=10", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [SqlServerFact]
    public async Task Stock_level_rejects_a_negative_bound()
    {
        var response = await Client.GetAsync(new Uri("/api/products/stock-level?min=-1", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [SqlServerFact]
    public async Task Categories_are_seeded_and_report_their_product_counts()
    {
        var categories = await GetAsync<IReadOnlyList<CategoryDto>>("/api/categories");

        categories.ShouldNotBeEmpty();
        categories.ShouldAllBe(category => category.ProductCount >= 0);
        categories.ShouldContain(category => category.ProductCount > 0);
    }

    [SqlServerFact]
    public async Task Seeded_products_are_spread_across_stock_states()
    {
        var outOfStock = await GetAsync<IReadOnlyList<ProductDto>>("/api/products/stock-level?min=0&max=0");

        // The seed data deliberately includes empty shelves so every UI state is reachable
        // without a reviewer having to create data first.
        outOfStock.ShouldNotBeEmpty();
    }
}
