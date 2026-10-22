using ProductManager.Application.Exceptions;

namespace ProductManager.Application.Abstractions;

/// <summary>Commits the changes made through the repositories in one transaction.</summary>
public interface IUnitOfWork
{
    /// <summary>Writes all pending changes.</summary>
    /// <exception cref="ConcurrencyConflictException">
    /// Another request changed one of the affected rows since it was read. Nothing was written.
    /// </exception>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Forgets all pending changes and loaded entities, so the next read is fresh.</summary>
    void DiscardChanges();
}
