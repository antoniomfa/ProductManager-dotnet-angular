using System.Net;
using ProductManager.Application.Products.Dtos;
using ProductManager.Domain.Entities;
using ProductManager.IntegrationTests.Support;

namespace ProductManager.IntegrationTests.SqlServer;

/// <summary>
/// Verifies the identifier requirement: unique 6-digit numbers that stay unique when several
/// callers create products at the same time.
/// </summary>
/// <remarks>
/// Driven through the HTTP API rather than against the sequence directly, so what is proven is
/// the behaviour a client actually gets. The guarantee comes from the database serialising
/// <c>NEXT VALUE FOR</c> in the column default, not from anything in the application process —
/// which is exactly why concurrent requests cannot collide however many API instances are running.
/// </remarks>
public sealed class ProductIdentifierTests : ApiTestBase
{
    private const int ConcurrentRequests = 40;

    public ProductIdentifierTests(ProductManagerApiFactory factory)
        : base(factory)
    {
    }

    [SqlServerFact]
    public async Task Concurrent_creates_never_receive_the_same_identifier()
    {
        var creations = Enumerable.Range(0, ConcurrentRequests)
            .Select(index => CreateAsync($"Concurrency Probe {index}"))
            .ToList();

        var identifiers = await Task.WhenAll(creations);

        identifiers.Length.ShouldBe(ConcurrentRequests);

        // The whole point of allocating in the database: no two callers get the same value.
        identifiers.Distinct().Count().ShouldBe(ConcurrentRequests);
    }

    [SqlServerFact]
    public async Task Every_allocated_identifier_is_a_six_digit_number()
    {
        var identifiers = await Task.WhenAll(
            Enumerable.Range(0, 10).Select(index => CreateAsync($"Six Digit Probe {index}")));

        identifiers.ShouldAllBe(id => id >= Product.MinIdentifier && id <= Product.MaxIdentifier);
        identifiers.ShouldAllBe(id => id.ToString(System.Globalization.CultureInfo.InvariantCulture).Length == 6);
    }

    [SqlServerFact]
    public async Task Identifiers_advance_rather_than_being_reissued()
    {
        var first = await CreateAsync("Advancing Probe A");
        var second = await CreateAsync("Advancing Probe B");

        second.ShouldBeGreaterThan(first);
    }

    [SqlServerFact]
    public async Task A_deleted_product_does_not_release_its_identifier_for_reuse()
    {
        var doomed = await CreateAsync("Identifier Reuse Probe");

        var deleteResponse = await Client.DeleteAsync(
            new Uri($"/api/products/{doomed}", UriKind.Relative));
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var next = await CreateAsync("Identifier Reuse Probe Successor");

        // Reissuing a deleted product's identifier would corrupt its retained stock history.
        next.ShouldNotBe(doomed);
        next.ShouldBeGreaterThan(doomed);
    }

    private async Task<int> CreateAsync(string name)
    {
        var response = await PostJsonAsync("/api/products", ProductRequestFactory.Create(name: name));
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await ReadAsync<ProductDetailDto>(response);
        created.ShouldNotBeNull();
        return created.Id;
    }
}
