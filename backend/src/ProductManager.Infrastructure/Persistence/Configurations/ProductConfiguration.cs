using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductManager.Domain.Entities;

namespace ProductManager.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", table =>
        {
            // Even a direct SQL write cannot push stock negative or create an identifier outside the 6-digit range.
            table.HasCheckConstraint("CK_Products_StockQuantity_NonNegative", "[StockQuantity] >= 0");
            table.HasCheckConstraint("CK_Products_Price_NonNegative", "[Price] >= 0");
            table.HasCheckConstraint(
                "CK_Products_Id_SixDigits",
                $"[Id] BETWEEN {Product.MinIdentifier} AND {Product.MaxIdentifier}");
        });

        builder.HasKey(product => product.Id);

        // The whole 6-digit identifier requirement lives on this one line. The sequence is a
        // shared, atomic allocator, so concurrent inserts from any number of API instances get
        // distinct values without coordinating. IDENTITY could not be used: it starts at 1 and
        // cannot be constrained to a six-digit range.
        builder.Property(product => product.Id)
            .HasDefaultValueSql($"NEXT VALUE FOR [{ProductManagerDbContext.ProductIdSequenceName}]")
            .ValueGeneratedOnAdd();

        builder.Property(product => product.Name)
            .IsRequired()
            .HasMaxLength(Product.NameMaxLength);

        builder.Property(product => product.Description)
            .HasMaxLength(Product.DescriptionMaxLength);

        builder.Property(product => product.Price)
            .HasPrecision(18, 2);

        builder.Property(product => product.StockQuantity)
            .IsRequired();

        builder.Property(product => product.LowStockThreshold)
            .IsRequired()
            .HasDefaultValue(5);

        builder.Property(product => product.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(product => product.CreatedAt).IsRequired();
        builder.Property(product => product.UpdatedAt).IsRequired();

        builder.HasIndex(product => product.Name);
        builder.HasIndex(product => product.StockQuantity);
        builder.HasIndex(product => new { product.IsDeleted, product.CategoryId });

        builder.HasOne(product => product.Category)
            .WithMany(category => category.Products)
            .HasForeignKey(product => product.CategoryId)
            // Categories with products cannot be deleted out from under them.
            .OnDelete(DeleteBehavior.Restrict);

        builder.Metadata
            .FindNavigation(nameof(Product.StockMovements))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // Deleted products disappear from every query without each call site remembering to
        // filter, while their rows and audit history survive for reporting.
        builder.HasQueryFilter(product => !product.IsDeleted);
    }
}
