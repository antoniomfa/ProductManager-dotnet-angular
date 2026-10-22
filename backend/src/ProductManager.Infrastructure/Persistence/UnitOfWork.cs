using Microsoft.EntityFrameworkCore;
using ProductManager.Application.Abstractions;
using ProductManager.Application.Exceptions;

namespace ProductManager.Infrastructure.Persistence;

/// <inheritdoc cref="IUnitOfWork" />
internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly ProductManagerDbContext _context;

    public UnitOfWork(ProductManagerDbContext context) => _context = context;

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            // Translated here so the application layer never has to know about EF Core.
            var entry = exception.Entries.FirstOrDefault();
            var resource = entry?.Metadata.ClrType.Name ?? "Resource";
            var key = entry?.Properties.FirstOrDefault(property => property.Metadata.IsPrimaryKey())?.CurrentValue;

            throw new ConcurrencyConflictException(resource, key ?? "unknown", exception);
        }
    }

    public void DiscardChanges() => _context.ChangeTracker.Clear();
}
