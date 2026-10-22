using Microsoft.EntityFrameworkCore;
using ProductManager.Application.Abstractions;
using ProductManager.Domain.Entities;
using ProductManager.Infrastructure.Persistence.Configurations;

namespace ProductManager.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the product catalogue, built code-first by migrations.
/// </summary>
public class ProductManagerDbContext : DbContext, IApplicationDbContext
{
    /// <summary>Name of the sequence that hands out 6-digit product identifiers.</summary>
    public const string ProductIdSequenceName = "ProductIdSequence";

    /// <summary>Name of the shadow property used as the optimistic concurrency token.</summary>
    public const string RowVersionProperty = "RowVersion";

    public ProductManagerDbContext(DbContextOptions<ProductManagerDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new StockMovementConfiguration());

        // The single atomic source of product identifiers, wired to Products.Id as a column
        // default. Allocation happens in the database rather than in any one API process, so N
        // instances can insert concurrently without coordinating and still never collide.
        // No CYCLE: running out must fail loudly rather than silently start reissuing
        // identifiers already in use.
        modelBuilder
            .HasSequence<int>(ProductIdSequenceName)
            .StartsAt(Product.MinIdentifier)
            .IncrementsBy(1)
            .HasMin(Product.MinIdentifier)
            .HasMax(Product.MaxIdentifier);
    }
}
