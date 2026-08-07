using ProductManager.Domain.Enums;
using ProductManager.Domain.Exceptions;
using ProductManager.UnitTests.Support;

namespace ProductManager.UnitTests.Domain;

/// <summary>
/// Covers the rules the aggregate enforces on its own stock, independently of any database.
/// </summary>
public sealed class ProductStockTests
{
    [Fact]
    public void AddStock_increases_the_quantity_and_records_a_movement()
    {
        var clock = new FixedTimeProvider();
        var product = ProductFactory.Create(initialStock: 20, timeProvider: clock);

        product.AddStock(8, "Delivery GRN-5512", clock);

        product.StockQuantity.ShouldBe(28);

        var movement = product.StockMovements.Last();
        movement.Type.ShouldBe(StockMovementType.Increment);
        movement.QuantityChange.ShouldBe(8);
        movement.ResultingStock.ShouldBe(28);
        movement.Reason.ShouldBe("Delivery GRN-5512");
    }

    [Fact]
    public void RemoveStock_decreases_the_quantity_and_records_a_negative_movement()
    {
        var clock = new FixedTimeProvider();
        var product = ProductFactory.Create(initialStock: 20, timeProvider: clock);

        product.RemoveStock(5, "Customer order #4821", clock);

        product.StockQuantity.ShouldBe(15);

        var movement = product.StockMovements.Last();
        movement.Type.ShouldBe(StockMovementType.Decrement);
        movement.QuantityChange.ShouldBe(-5);
        movement.ResultingStock.ShouldBe(15);
    }

    [Fact]
    public void RemoveStock_rejects_more_than_is_available_and_leaves_stock_untouched()
    {
        var clock = new FixedTimeProvider();
        var product = ProductFactory.Create(initialStock: 3, timeProvider: clock);
        var movementsBefore = product.StockMovements.Count;

        var exception = Should.Throw<InsufficientStockException>(() => product.RemoveStock(4, null, clock));

        exception.Available.ShouldBe(3);
        exception.Requested.ShouldBe(4);

        // The failure must not have half-applied: no stock lost, no phantom audit entry.
        product.StockQuantity.ShouldBe(3);
        product.StockMovements.Count.ShouldBe(movementsBefore);
    }

    [Fact]
    public void RemoveStock_allows_taking_the_last_unit()
    {
        var clock = new FixedTimeProvider();
        var product = ProductFactory.Create(initialStock: 3, timeProvider: clock);

        product.RemoveStock(3, null, clock);

        product.StockQuantity.ShouldBe(0);
        product.StockStatus.ShouldBe(StockStatus.OutOfStock);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Stock_movements_require_a_positive_quantity(int quantity)
    {
        var clock = new FixedTimeProvider();
        var product = ProductFactory.Create(timeProvider: clock);

        Should.Throw<InvalidProductStateException>(() => product.AddStock(quantity, null, clock));
        Should.Throw<InvalidProductStateException>(() => product.RemoveStock(quantity, null, clock));
    }

    [Fact]
    public void AdjustStockTo_records_the_signed_difference()
    {
        var clock = new FixedTimeProvider();
        var product = ProductFactory.Create(initialStock: 20, timeProvider: clock);

        product.AdjustStockTo(12, "Stock take", clock);

        product.StockQuantity.ShouldBe(12);

        var movement = product.StockMovements.Last();
        movement.Type.ShouldBe(StockMovementType.Adjustment);
        movement.QuantityChange.ShouldBe(-8);
        movement.ResultingStock.ShouldBe(12);
    }

    [Fact]
    public void AdjustStockTo_the_current_level_records_nothing()
    {
        var clock = new FixedTimeProvider();
        var product = ProductFactory.Create(initialStock: 20, timeProvider: clock);
        var movementsBefore = product.StockMovements.Count;

        product.AdjustStockTo(20, "No-op stock take", clock);

        product.StockMovements.Count.ShouldBe(movementsBefore);
    }

    [Fact]
    public void AdjustStockTo_rejects_a_negative_level()
    {
        var clock = new FixedTimeProvider();
        var product = ProductFactory.Create(timeProvider: clock);

        Should.Throw<InvalidProductStateException>(() => product.AdjustStockTo(-1, null, clock));
    }

    [Theory]
    [InlineData(0, 5, StockStatus.OutOfStock)]
    [InlineData(3, 5, StockStatus.LowStock)]
    [InlineData(5, 5, StockStatus.LowStock)]
    [InlineData(6, 5, StockStatus.InStock)]
    [InlineData(1, 0, StockStatus.InStock)]
    public void StockStatus_is_derived_from_the_quantity_and_threshold(
        int stock,
        int threshold,
        StockStatus expected)
    {
        var product = ProductFactory.Create(initialStock: stock, lowStockThreshold: threshold);

        product.StockStatus.ShouldBe(expected);
    }

    [Fact]
    public void Movements_accumulate_in_the_order_they_were_applied()
    {
        var clock = new FixedTimeProvider();
        var product = ProductFactory.Create(initialStock: 10, timeProvider: clock);

        clock.Advance(TimeSpan.FromMinutes(5));
        product.AddStock(5, "Restock", clock);

        clock.Advance(TimeSpan.FromMinutes(5));
        product.RemoveStock(3, "Sale", clock);

        product.StockMovements.Select(movement => movement.ResultingStock).ShouldBe([10, 15, 12]);
        product.UpdatedAt.ShouldBe(FixedTimeProvider.DefaultNow.AddMinutes(10));
    }
}
