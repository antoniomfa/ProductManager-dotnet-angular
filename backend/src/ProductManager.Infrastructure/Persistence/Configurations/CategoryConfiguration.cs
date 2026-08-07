using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductManager.Domain.Entities;

namespace ProductManager.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Id)
            .ValueGeneratedOnAdd();

        builder.Property(category => category.Name)
            .IsRequired()
            .HasMaxLength(Category.NameMaxLength);

        builder.Property(category => category.Description)
            .HasMaxLength(Category.DescriptionMaxLength);

        builder.HasIndex(category => category.Name)
            .IsUnique();

        builder.Metadata
            .FindNavigation(nameof(Category.Products))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
