namespace ProductManager.Infrastructure.Seeding;

/// <summary>
/// Brings the database up to the current schema and populates it with starter data.
/// </summary>
public interface IDatabaseInitializer
{
    /// <summary>Applies any pending migrations, creating the database if it does not exist.</summary>
    Task InitialiseAsync(CancellationToken cancellationToken = default);


    /// <summary>
    /// Inserts reference and sample data. Idempotent: safe to run on every startup, and a no-op
    /// once the catalogue already holds products.
    /// </summary>
    Task SeedAsync(CancellationToken cancellationToken = default);
}
