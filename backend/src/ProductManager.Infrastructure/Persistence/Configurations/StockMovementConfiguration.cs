using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductManager.Domain.Entities;

namespace ProductManager.Infrastructure.Persistence.Configurations;

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");

        builder.HasKey(movement => movement.Id);

        builder.Property(movement => movement.Id)
            .ValueGeneratedOnAdd();

        builder.Property(movement => movement.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(movement => movement.QuantityChange).IsRequired();
        builder.Property(movement => movement.ResultingStock).IsRequired();
        builder.Property(movement => movement.OccurredAt).IsRequired();

        builder.Property(movement => movement.Reason)
            .HasMaxLength(StockMovement.ReasonMaxLength);

        builder.HasOne(movement => movement.Product)
            .WithMany(product => product.StockMovements)
            .HasForeignKey(movement => movement.ProductId)
            // History follows its product only if the row is ever hard-deleted; normal deletes
            // are soft, so the trace is retained.
            .OnDelete(DeleteBehavior.Cascade);

        // Serves the "history for one product, newest first" access pattern directly.
        builder.HasIndex(movement => new { movement.ProductId, movement.OccurredAt });

        // Mirrors the filter on Product. Without it EF warns that a required navigation can be
        // filtered away, and a movement could otherwise outlive the product it describes in
        // query results.
        builder.HasQueryFilter(movement => !movement.Product!.IsDeleted);
    }
}
