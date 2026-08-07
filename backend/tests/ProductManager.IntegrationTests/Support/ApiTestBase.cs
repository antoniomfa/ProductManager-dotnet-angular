using System.Net;
using System.Net.Http.Json;
using ProductManager.Application.Products.Dtos;

namespace ProductManager.IntegrationTests.Support;

/// <summary>
/// Shared plumbing for endpoint tests: a client pointed at the in-process API, plus helpers that
/// keep each test about the behaviour under test rather than about serialisation.
/// </summary>
/// <remarks>
/// Each test class takes its own factory, so every class gets a freshly migrated and seeded
/// database and no test can be affected by writes made in another. The client is created lazily
/// so that a test skipped for want of SQL Server never triggers database creation.
/// </remarks>
public abstract class ApiTestBase : IClassFixture<ProductManagerApiFactory>
{
    private readonly ProductManagerApiFactory _factory;
    private HttpClient? _client;

    protected ApiTestBase(ProductManagerApiFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    protected HttpClient Client => _client ??= _factory.CreateClient();

    protected static Task<T?> ReadAsync<T>(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return response.Content.ReadFromJsonAsync<T>(ProductManagerApiFactory.JsonOptions);
    }

    protected async Task<T> GetAsync<T>(string url)
        where T : class
    {
        var response = await Client.GetAsync(new Uri(url, UriKind.Relative));
        response.EnsureSuccessStatusCode();

        var payload = await ReadAsync<T>(response);
        payload.ShouldNotBeNull();
        return payload;
    }

    protected Task<HttpResponseMessage> PostJsonAsync<TBody>(string url, TBody body) =>
        Client.PostAsJsonAsync(new Uri(url, UriKind.Relative), body, ProductManagerApiFactory.JsonOptions);

    protected Task<HttpResponseMessage> PutJsonAsync<TBody>(string url, TBody body) =>
        Client.PutAsJsonAsync(new Uri(url, UriKind.Relative), body, ProductManagerApiFactory.JsonOptions);

    /// <summary>Creates a product through the API and asserts it was accepted.</summary>
    protected async Task<ProductDetailDto> CreateProductAsync(
        string? name = null,
        int initialStock = 10,
        int lowStockThreshold = 3)
    {
        var response = await PostJsonAsync(
            "/api/products",
            ProductRequestFactory.Create(
                name: name,
                initialStock: initialStock,
                lowStockThreshold: lowStockThreshold));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await ReadAsync<ProductDetailDto>(response);
        created.ShouldNotBeNull();
        return created;
    }
}
