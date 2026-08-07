using ProductManager.Domain.Entities;
using ProductManager.Domain.Enums;
using ProductManager.Domain.Exceptions;
using ProductManager.UnitTests.Support;

namespace ProductManager.UnitTests.Domain;

/// <summary>Covers the invariants applied when a product is created or edited.</summary>
public sealed class ProductCreationTests
{
    [Fact]
    public void Create_records_the_opening_stock_as_a_movement()
    {
        var product = ProductFactory.Create(initialStock: 20);

        product.StockQuantity.ShouldBe(20);
        product.StockMovements.Count.ShouldBe(1);
        product.StockMovements.Single().Type.ShouldBe(StockMovementType.InitialStock);
    }

    [Fact]
    public void Create_with_no_opening_stock_records_no_movement()
    {
        var product = ProductFactory.Create(initialStock: 0);

        product.StockQuantity.ShouldBe(0);
        product.StockMovements.ShouldBeEmpty();
    }

    [Fact]
    public void Create_leaves_the_identifier_unset_because_the_database_assigns_it()
    {
        var product = Product.Create(
            "Unsaved Product",
            null,
            10m,
            1,
            1,
            1,
            new FixedTimeProvider());

        // The sequence-backed column default supplies this on insert; nothing in the domain
        // chooses it, which is what keeps it unique across concurrent API instances.
        product.Id.ShouldBe(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_requires_a_name(string name)
    {
        Should.Throw<InvalidProductStateException>(() => ProductFactory.Create(name: name));
    }

    [Fact]
    public void Create_rejects_a_negative_price()
    {
        Should.Throw<InvalidProductStateException>(() => ProductFactory.Create(price: -0.01m));
    }

    [Fact]
    public void Create_rounds_the_price_to_two_decimals_to_match_the_stored_precision()
    {
        ProductFactory.Create(price: 10.005m).Price.ShouldBe(10.01m);
    }

    [Fact]
    public void Create_trims_the_name()
    {
        ProductFactory.Create(name: "  Padded Name  ").Name.ShouldBe("Padded Name");
    }

    [Fact]
    public void Create_treats_a_blank_description_as_absent()
    {
        ProductFactory.Create(description: "   ").Description.ShouldBeNull();
    }

    [Fact]
    public void Create_rejects_a_name_beyond_the_stored_length()
    {
        Should.Throw<InvalidProductStateException>(
            () => ProductFactory.Create(name: new string('a', Product.NameMaxLength + 1)));
    }

    [Fact]
    public void UpdateDetails_changes_the_descriptive_fields_and_leaves_stock_alone()
    {
        var clock = new FixedTimeProvider();
        var product = ProductFactory.Create(initialStock: 20, timeProvider: clock);
        var movementsBefore = product.StockMovements.Count;

        clock.Advance(TimeSpan.FromHours(2));
        product.UpdateDetails("Renamed", "New description", 19.99m, 3, 4, clock);

        product.Name.ShouldBe("Renamed");
        product.Price.ShouldBe(19.99m);
        product.LowStockThreshold.ShouldBe(3);
        product.CategoryId.ShouldBe(4);
        product.UpdatedAt.ShouldBe(FixedTimeProvider.DefaultNow.AddHours(2));

        // Stock only ever moves through the stock methods, so an edit leaves no audit entry.
        product.StockQuantity.ShouldBe(20);
        product.StockMovements.Count.ShouldBe(movementsBefore);
    }

    [Fact]
    public void SoftDelete_marks_the_product_without_discarding_its_history()
    {
        var clock = new FixedTimeProvider();
        var product = ProductFactory.Create(initialStock: 20, timeProvider: clock);

        product.SoftDelete(clock);

        product.IsDeleted.ShouldBeTrue();
        product.StockMovements.ShouldNotBeEmpty();
    }
}
