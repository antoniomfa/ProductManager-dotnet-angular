using Microsoft.EntityFrameworkCore;
using ProductManager.Domain.Entities;

namespace ProductManager.Application.Abstractions;

/// <summary>
/// The database, as the application layer sees it.
/// </summary>
/// <remarks>
/// There is no repository over the top of this on purpose. EF Core's <see cref="DbSet{TEntity}"/>
/// is already a repository and <c>SaveChangesAsync</c> is already a unit of work, so wrapping them
/// would add a layer without adding anything.
/// <para>
/// This interface exists only so the application layer does not have to reference the
/// Infrastructure project, which would point the dependency the wrong way. It exposes the sets and
/// nothing else — no provider, no connection string, no configuration.
/// </para>
/// </remarks>
public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }

    DbSet<Category> Categories { get; }

    DbSet<StockMovement> StockMovements { get; }

    /// <summary>Writes all pending changes in one transaction.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
