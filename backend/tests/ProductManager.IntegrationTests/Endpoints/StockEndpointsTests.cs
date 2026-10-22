using System.Net;
using ProductManager.Application.Products.Dtos;
using ProductManager.Domain.Enums;
using ProductManager.IntegrationTests.Support;

namespace ProductManager.IntegrationTests.Endpoints;

/// <summary>Covers the two dedicated stock endpoints and the audit trail they produce.</summary>
public sealed class StockEndpointsTests : ApiTestBase
{
    public StockEndpointsTests(ProductManagerApiFactory factory)
        : base(factory)
    {
    }

    [SqlServerFact]
    public async Task Adding_stock_returns_ok_and_reports_both_sides_of_the_change()
    {
        var product = await CreateProductAsync(initialStock: 10);

        var response = await Client.PostAsync(
            new Uri($"/api/products/{product.Id}/add-to-stock/8", UriKind.Relative),
            content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await ReadAsync<StockOperationResult>(response);
        result.ShouldNotBeNull();
        result.PreviousStock.ShouldBe(10);
        result.NewStock.ShouldBe(18);
    }

    [SqlServerFact]
    public async Task Decrementing_stock_returns_ok_and_lowers_the_level()
    {
        var product = await CreateProductAsync(initialStock: 10);

        var response = await Client.PostAsync(
            new Uri($"/api/products/{product.Id}/decrement-stock/4", UriKind.Relative),
            content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await ReadAsync<StockOperationResult>(response);
        result.ShouldNotBeNull();
        result.PreviousStock.ShouldBe(10);
        result.NewStock.ShouldBe(6);
    }

    [SqlServerFact]
    public async Task Stock_changes_are_visible_to_the_next_read()
    {
        var product = await CreateProductAsync(initialStock: 10);

        await PostJsonAsync($"/api/products/{product.Id}/add-to-stock/5", new StockAdjustmentRequest());
        await PostJsonAsync($"/api/products/{product.Id}/decrement-stock/2", new StockAdjustmentRequest());

        var refetched = await GetAsync<ProductDetailDto>($"/api/products/{product.Id}");
        refetched.StockQuantity.ShouldBe(13);
    }

    [SqlServerFact]
    public async Task A_reason_supplied_in_the_body_is_stored_against_the_movement()
    {
        var product = await CreateProductAsync(initialStock: 10);

        await PostJsonAsync(
            $"/api/products/{product.Id}/decrement-stock/3",
            new StockAdjustmentRequest { Reason = "Customer order #4821" });

        var refetched = await GetAsync<ProductDetailDto>($"/api/products/{product.Id}");

        refetched.StockMovements.ShouldContain(movement =>
            movement.Reason == "Customer order #4821" && movement.QuantityChange == -3);
    }

    [SqlServerFact]
    public async Task Decrementing_more_than_is_held_is_refused_with_the_available_quantity()
    {
        var product = await CreateProductAsync(initialStock: 3);

        var response = await Client.PostAsync(
            new Uri($"/api/products/{product.Id}/decrement-stock/10", UriKind.Relative),
            content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var problem = await ReadAsync<ValidationProblemResponse>(response);
        problem.ShouldNotBeNull();
        problem.AvailableStock.ShouldBe(3);
        problem.RequestedQuantity.ShouldBe(10);

        // The refusal must leave the product exactly as it was.
        var refetched = await GetAsync<ProductDetailDto>($"/api/products/{product.Id}");
        refetched.StockQuantity.ShouldBe(3);
    }

    [SqlServerTheory]
    [InlineData("add-to-stock")]
    [InlineData("decrement-stock")]
    public async Task A_zero_quantity_is_rejected(string operation)
    {
        var product = await CreateProductAsync();

        var response = await Client.PostAsync(
            new Uri($"/api/products/{product.Id}/{operation}/0", UriKind.Relative),
            content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [SqlServerTheory]
    [InlineData("add-to-stock")]
    [InlineData("decrement-stock")]
    public async Task An_unknown_product_is_reported_as_not_found(string operation)
    {
        var response = await Client.PostAsync(
            new Uri($"/api/products/999999/{operation}/1", UriKind.Relative),
            content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [SqlServerFact]
    public async Task Taking_the_last_unit_moves_the_product_to_out_of_stock()
    {
        var product = await CreateProductAsync(initialStock: 5, lowStockThreshold: 2);

        var response = await Client.PostAsync(
            new Uri($"/api/products/{product.Id}/decrement-stock/5", UriKind.Relative),
            content: null);

        var result = await ReadAsync<StockOperationResult>(response);
        result.ShouldNotBeNull();
        result.NewStock.ShouldBe(0);
        result.StockStatus.ShouldBe(StockStatus.OutOfStock);
    }

    [SqlServerFact]
    public async Task Dropping_to_the_threshold_reports_low_stock()
    {
        var product = await CreateProductAsync(initialStock: 10, lowStockThreshold: 4);

        var response = await Client.PostAsync(
            new Uri($"/api/products/{product.Id}/decrement-stock/6", UriKind.Relative),
            content: null);

        var result = await ReadAsync<StockOperationResult>(response);
        result.ShouldNotBeNull();
        result.NewStock.ShouldBe(4);
        result.StockStatus.ShouldBe(StockStatus.LowStock);
    }

    [SqlServerFact]
    public async Task Every_movement_is_recorded_in_order_so_the_history_reconciles()
    {
        var product = await CreateProductAsync(initialStock: 10);

        await PostJsonAsync($"/api/products/{product.Id}/add-to-stock/5", new StockAdjustmentRequest());
        await PostJsonAsync($"/api/products/{product.Id}/decrement-stock/7", new StockAdjustmentRequest());

        var refetched = await GetAsync<ProductDetailDto>($"/api/products/{product.Id}");

        refetched.StockMovements.Count.ShouldBe(3);

        // Movements come back newest first, so the most recent must agree with the current level.
        refetched.StockMovements[0].ResultingStock.ShouldBe(refetched.StockQuantity);
        refetched.StockMovements.Sum(movement => movement.QuantityChange).ShouldBe(refetched.StockQuantity);
    }
}
