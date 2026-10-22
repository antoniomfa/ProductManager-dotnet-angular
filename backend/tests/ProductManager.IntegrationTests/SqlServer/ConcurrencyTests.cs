using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProductManager.Application.Products.Dtos;
using ProductManager.Infrastructure.Persistence;
using ProductManager.IntegrationTests.Support;

namespace ProductManager.IntegrationTests.SqlServer;

/// <summary>
/// Verifies that concurrent writes to the same product cannot overwrite each other, which is what
/// matters when several API instances share one database.
/// </summary>
/// <remarks>
/// The concurrent tests do not assert how many requests succeed, because that depends on timing.
/// They assert the invariants that must hold whatever the timing: no request is lost silently,
/// stock never goes below zero, and the movement history reconciles with the stock level.
/// </remarks>
public sealed class ConcurrencyTests : ApiTestBase
{
    private const int ConcurrentRequests = 25;

    public ConcurrencyTests(ProductManagerApiFactory factory)
        : base(factory)
    {
    }

    [SqlServerFact]
    public async Task Concurrent_decrements_never_sell_more_than_is_in_stock()
    {
        const int initialStock = 10;
        var product = await CreateProductAsync(initialStock: initialStock);

        var responses = await SendConcurrentlyAsync($"/api/products/{product.Id}/decrement-stock/1");

        responses.ShouldAllBe(status => status == HttpStatusCode.OK || status == HttpStatusCode.Conflict);

        var succeeded = responses.Count(status => status == HttpStatusCode.OK);
        succeeded.ShouldBeGreaterThan(0);
        succeeded.ShouldBeLessThanOrEqualTo(initialStock);

        var refetched = await GetAsync<ProductDetailDto>($"/api/products/{product.Id}");
        refetched.StockQuantity.ShouldBe(initialStock - succeeded);
        refetched.StockMovements.Sum(movement => movement.QuantityChange).ShouldBe(refetched.StockQuantity);
    }

    [SqlServerFact]
    public async Task Concurrent_additions_are_never_lost()
    {
        var product = await CreateProductAsync(initialStock: 0);

        var responses = await SendConcurrentlyAsync($"/api/products/{product.Id}/add-to-stock/1");

        responses.ShouldAllBe(status => status == HttpStatusCode.OK || status == HttpStatusCode.Conflict);

        var succeeded = responses.Count(status => status == HttpStatusCode.OK);
        succeeded.ShouldBeGreaterThan(0);

        // Without the concurrency token, two requests reading the same level would both write
        // level + 1, and the stock would end up lower than the number of successful additions.
        var refetched = await GetAsync<ProductDetailDto>($"/api/products/{product.Id}");
        refetched.StockQuantity.ShouldBe(succeeded);
        refetched.StockMovements.Sum(movement => movement.QuantityChange).ShouldBe(refetched.StockQuantity);
    }

    [SqlServerFact]
    public async Task A_write_based_on_a_stale_read_is_rejected()
    {
        var product = await CreateProductAsync(initialStock: 10);

        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ProductManagerDbContext>();

        var staleCopy = await context.Products.SingleAsync(candidate => candidate.Id == product.Id);

        // Another request changes the product after the stale copy was read.
        var response = await Client.PostAsync(
            new Uri($"/api/products/{product.Id}/add-to-stock/5", UriKind.Relative),
            content: null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        staleCopy.AddStock(1, reason: null, TimeProvider.System);

        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => context.SaveChangesAsync());
    }

    private async Task<HttpStatusCode[]> SendConcurrentlyAsync(string url)
    {
        var requests = Enumerable.Range(0, ConcurrentRequests)
            .Select(async _ =>
            {
                using var response = await Client.PostAsync(new Uri(url, UriKind.Relative), content: null);
                return response.StatusCode;
            });

        return await Task.WhenAll(requests);
    }
}
