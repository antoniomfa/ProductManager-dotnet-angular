using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ProductManager.Application.Abstractions;
using ProductManager.Application.Exceptions;
using ProductManager.Application.Products;
using ProductManager.Application.Products.Dtos;
using ProductManager.Domain.Entities;
using ProductManager.Domain.Exceptions;
using ProductManager.UnitTests.Support;

namespace ProductManager.UnitTests.Application;

/// <summary>
/// Covers the orchestration in <see cref="ProductService"/>: what it loads, what it saves, and how
/// it reacts to missing data and concurrency conflicts. Persistence itself is covered by the
/// integration suite.
/// </summary>
public sealed class ProductServiceTests
{
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly ICategoryRepository _categories = Substitute.For<ICategoryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FixedTimeProvider _clock = new();
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        _categories.ExistsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);

        _service = new ProductService(
            _products,
            _categories,
            _unitOfWork,
            _clock,
            NullLogger<ProductService>.Instance);
    }

    [Fact]
    public async Task GetProductById_throws_not_found_for_an_unknown_product()
    {
        await Should.ThrowAsync<NotFoundException>(() => _service.GetProductByIdAsync(999999));
    }

    [Fact]
    public async Task CreateProduct_adds_and_saves_the_product()
    {
        var request = new CreateProductRequest { Name = "Desk Lamp", Price = 19.99m, InitialStock = 4, CategoryId = 1 };
        _products.GetByIdWithMovementsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => ProductFactory.Create(name: "Desk Lamp", id: call.Arg<int>()));

        var created = await _service.CreateProductAsync(request);

        created.Name.ShouldBe("Desk Lamp");
        _products.Received(1).Add(Arg.Is<Product>(product => product.Name == "Desk Lamp" && product.StockQuantity == 4));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateProduct_with_an_unknown_category_saves_nothing()
    {
        _categories.ExistsAsync(42, Arg.Any<CancellationToken>()).Returns(false);
        var request = new CreateProductRequest { Name = "Desk Lamp", Price = 19.99m, CategoryId = 42 };

        await Should.ThrowAsync<NotFoundException>(() => _service.CreateProductAsync(request));

        _products.DidNotReceive().Add(Arg.Any<Product>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteProduct_soft_deletes_and_saves()
    {
        var product = GivenProduct(initialStock: 5);

        await _service.DeleteProductAsync(product.Id);

        product.IsDeleted.ShouldBeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DecrementStock_reports_the_level_before_and_after()
    {
        var product = GivenProduct(initialStock: 10);

        var result = await _service.DecrementStockAsync(product.Id, 4);

        result.PreviousStock.ShouldBe(10);
        result.NewStock.ShouldBe(6);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DecrementStock_beyond_the_available_quantity_saves_nothing()
    {
        var product = GivenProduct(initialStock: 3);

        await Should.ThrowAsync<InsufficientStockException>(() => _service.DecrementStockAsync(product.Id, 5));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Stock_changes_reject_a_non_positive_quantity_before_loading_anything(int quantity)
    {
        await Should.ThrowAsync<ValidationException>(() => _service.AddToStockAsync(ProductFactory.DefaultId, quantity));
        await Should.ThrowAsync<ValidationException>(() => _service.DecrementStockAsync(ProductFactory.DefaultId, quantity));

        await _products.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
    }

    [Fact]
    public async Task A_stock_change_is_retried_on_fresh_data_after_a_concurrency_conflict()
    {
        var stale = ProductFactory.Create(initialStock: 10);
        var fresh = ProductFactory.Create(initialStock: 7);
        _products.GetByIdAsync(ProductFactory.DefaultId, Arg.Any<CancellationToken>()).Returns(stale, fresh);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(
            _ => throw Conflict(),
            _ => Task.CompletedTask);

        var result = await _service.DecrementStockAsync(ProductFactory.DefaultId, 2);

        // The second attempt works from the level another request left behind, not the stale one.
        result.PreviousStock.ShouldBe(7);
        result.NewStock.ShouldBe(5);
        _unitOfWork.Received(1).DiscardChanges();
        await _unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_stock_change_gives_up_after_three_conflicting_attempts()
    {
        _products.GetByIdAsync(ProductFactory.DefaultId, Arg.Any<CancellationToken>())
            .Returns(_ => ProductFactory.Create(initialStock: 10));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Throws(Conflict());

        await Should.ThrowAsync<ConcurrencyConflictException>(() => _service.AddToStockAsync(ProductFactory.DefaultId, 1));

        await _unitOfWork.Received(3).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_update_that_conflicts_is_reported_rather_than_retried()
    {
        _products.GetByIdWithMovementsAsync(ProductFactory.DefaultId, Arg.Any<CancellationToken>())
            .Returns(ProductFactory.Create());
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Throws(Conflict());
        var request = new UpdateProductRequest { Name = "Renamed", Price = 10m, CategoryId = 1 };

        await Should.ThrowAsync<ConcurrencyConflictException>(
            () => _service.UpdateProductAsync(ProductFactory.DefaultId, request));

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchByName_requires_a_term(string term)
    {
        await Should.ThrowAsync<ValidationException>(() => _service.SearchByNameAsync(term));
    }

    [Theory]
    [InlineData(-1, null)]
    [InlineData(null, -1)]
    [InlineData(10, 5)]
    public async Task GetByStockLevel_rejects_an_invalid_range(int? minimum, int? maximum)
    {
        await Should.ThrowAsync<ValidationException>(() => _service.GetByStockLevelAsync(minimum, maximum));

        await _products.DidNotReceiveWithAnyArgs().GetByStockLevelAsync(default, default, default);
    }

    private Product GivenProduct(int initialStock)
    {
        var product = ProductFactory.Create(initialStock: initialStock, timeProvider: _clock);
        _products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        return product;
    }

    private static ConcurrencyConflictException Conflict() =>
        new("Product", ProductFactory.DefaultId, new InvalidOperationException("Simulated conflict."));
}
