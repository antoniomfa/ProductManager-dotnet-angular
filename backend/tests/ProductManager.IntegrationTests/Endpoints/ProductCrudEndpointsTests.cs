using System.Net;
using ProductManager.Application.Common;
using ProductManager.Application.Products.Dtos;
using ProductManager.Domain.Entities;
using ProductManager.IntegrationTests.Support;

namespace ProductManager.IntegrationTests.Endpoints;

/// <summary>Covers the five CRUD endpoints end to end, over real HTTP.</summary>
public sealed class ProductCrudEndpointsTests : ApiTestBase
{
    public ProductCrudEndpointsTests(ProductManagerApiFactory factory)
        : base(factory)
    {
    }

    [SqlServerFact]
    public async Task GET_products_returns_a_seeded_page_including_stock_for_every_item()
    {
        var page = await GetAsync<PagedResult<ProductDto>>("/api/products");

        page.TotalCount.ShouldBeGreaterThan(0);
        page.Items.ShouldNotBeEmpty();

        // The brief requires available stock on every listing response.
        page.Items.ShouldAllBe(product => product.StockQuantity >= 0);
        page.Items.ShouldAllBe(product => product.Id >= Product.MinIdentifier && product.Id <= Product.MaxIdentifier);
        page.Items.ShouldAllBe(product => !string.IsNullOrWhiteSpace(product.CategoryName));
    }

    [SqlServerFact]
    public async Task GET_products_honours_paging()
    {
        var firstPage = await GetAsync<PagedResult<ProductDto>>("/api/products?page=1&pageSize=5");
        var secondPage = await GetAsync<PagedResult<ProductDto>>("/api/products?page=2&pageSize=5");

        firstPage.Items.Count.ShouldBe(5);
        firstPage.Page.ShouldBe(1);
        firstPage.HasPreviousPage.ShouldBeFalse();

        secondPage.Page.ShouldBe(2);
        secondPage.HasPreviousPage.ShouldBeTrue();

        // Distinct pages must not overlap.
        firstPage.Items.Select(product => product.Id)
            .Intersect(secondPage.Items.Select(product => product.Id))
            .ShouldBeEmpty();
    }

    [SqlServerFact]
    public async Task POST_products_allocates_a_six_digit_identifier_and_returns_its_location()
    {
        var response = await PostJsonAsync("/api/products", ProductRequestFactory.Create(initialStock: 7));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();

        var created = await ReadAsync<ProductDetailDto>(response);
        created.ShouldNotBeNull();
        created.Id.ShouldBeInRange(Product.MinIdentifier, Product.MaxIdentifier);
        created.StockQuantity.ShouldBe(7);

        // The opening stock is recorded, so the audit trail reconciles from the very first unit.
        created.StockMovements.ShouldHaveSingleItem();

        var fetched = await GetAsync<ProductDetailDto>($"/api/products/{created.Id}");
        fetched.Name.ShouldBe(created.Name);
    }

    [SqlServerFact]
    public async Task POST_products_rejects_an_invalid_payload_with_per_field_errors()
    {
        var invalid = ProductRequestFactory.Create() with
        {
            Name = string.Empty,
            Price = -1m,
            CategoryId = 0,
        };

        var response = await PostJsonAsync("/api/products", invalid);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var problem = await ReadAsync<ValidationProblemResponse>(response);
        problem.ShouldNotBeNull();
        problem.Errors.ShouldNotBeNull();
        problem.Errors.Keys.ShouldContain("Name");
        problem.Errors.Keys.ShouldContain("Price");
        problem.Errors.Keys.ShouldContain("CategoryId");
    }

    [SqlServerFact]
    public async Task POST_products_rejects_an_unknown_category()
    {
        var response = await PostJsonAsync("/api/products", ProductRequestFactory.Create(categoryId: 9_999));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [SqlServerFact]
    public async Task GET_product_by_unknown_id_returns_a_problem_document()
    {
        var response = await Client.GetAsync(new Uri("/api/products/999999", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        var problem = await ReadAsync<ValidationProblemResponse>(response);
        problem.ShouldNotBeNull();
        problem.Status.ShouldBe(404);
    }

    [SqlServerFact]
    public async Task PUT_products_updates_the_stored_values()
    {
        var created = await CreateProductAsync();

        var update = ProductRequestFactory.Update(name: "Renamed By Test", price: 77.25m);
        var response = await PutJsonAsync($"/api/products/{created.Id}", update);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var updated = await ReadAsync<ProductDetailDto>(response);
        updated.ShouldNotBeNull();
        updated.Name.ShouldBe("Renamed By Test");
        updated.Price.ShouldBe(77.25m);

        // Confirmed by re-reading, so the change is proven to have been persisted.
        var refetched = await GetAsync<ProductDetailDto>($"/api/products/{created.Id}");
        refetched.Name.ShouldBe("Renamed By Test");
    }

    [SqlServerFact]
    public async Task PUT_products_can_apply_a_stock_take()
    {
        var created = await CreateProductAsync(initialStock: 10);

        var update = ProductRequestFactory.Update(stockQuantity: 4);
        var response = await PutJsonAsync($"/api/products/{created.Id}", update);

        var updated = await ReadAsync<ProductDetailDto>(response);
        updated.ShouldNotBeNull();
        updated.StockQuantity.ShouldBe(4);
        updated.StockMovements.ShouldContain(movement => movement.QuantityChange == -6);
    }

    [SqlServerFact]
    public async Task PUT_products_on_an_unknown_id_returns_not_found()
    {
        var response = await PutJsonAsync("/api/products/999999", ProductRequestFactory.Update());

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [SqlServerFact]
    public async Task DELETE_products_removes_it_from_every_read_endpoint()
    {
        var created = await CreateProductAsync(name: "Doomed Product");

        var deleteResponse = await Client.DeleteAsync(new Uri($"/api/products/{created.Id}", UriKind.Relative));
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getResponse = await Client.GetAsync(new Uri($"/api/products/{created.Id}", UriKind.Relative));
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var search = await GetAsync<IReadOnlyList<ProductDto>>("/api/products/search?name=Doomed");
        search.ShouldNotContain(product => product.Id == created.Id);
    }

    [SqlServerFact]
    public async Task DELETE_products_on_an_unknown_id_returns_not_found()
    {
        var response = await Client.DeleteAsync(new Uri("/api/products/999999", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
