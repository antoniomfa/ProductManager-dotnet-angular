using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProductManager.Infrastructure.Persistence;
using ProductManager.Infrastructure.Seeding;

namespace ProductManager.IntegrationTests.Support;

/// <summary>
/// Boots the real application - the same pipeline, filters, exception handler and JSON settings
/// that production uses - against a throwaway SQL Server database.
/// </summary>
/// <remarks>
/// <para>
/// Each test class gets its own database, created on first use and dropped afterwards, so classes
/// cannot interfere with one another and nothing is left behind on the developer's machine.
/// </para>
/// <para>
/// The database is swapped by replacing the registered <see cref="DbContextOptions"/> rather than
/// by supplying a connection string through configuration. Configuration added in
/// <see cref="ConfigureWebHost"/> is applied after the application's entry point has already run,
/// by which point <c>AddInfrastructure</c> has read the original connection string - so a
/// configuration override is silently ignored and the tests quietly hit the development database.
/// </para>
/// </remarks>
public sealed class ProductManagerApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"ProductManagerTests_{Guid.NewGuid():N}";

    private int _initialised;

    public ProductManagerApiFactory()
    {
        // Environment variables are read before the entry point runs, so unlike ConfigureWebHost
        // these do take effect. Without them the entry point starts in Development, where
        // appsettings.Development.json turns auto-migration on and the schema is built twice.
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("Database__AutoMigrateAndSeed", "false");
    }

    /// <summary>Mirrors the API's own JSON settings so test deserialisation matches the wire format.</summary>
    public static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureServices(services =>
        {
            RemoveDbContextRegistrations(services);

            services.AddDbContext<ProductManagerDbContext>(options =>
                options.UseSqlServer(SqlServerAvailability.TestDatabaseConnectionString(_databaseName)));
        });
    }

    /// <summary>Migrates and seeds the throwaway database once, before any test uses it.</summary>
    /// <remarks>
    /// Uses the application's own <see cref="IDatabaseInitializer"/> so the migration and seeding
    /// code stays covered. The interlocked guard matters because WebApplicationFactory builds more
    /// than one host while resolving the entry point.
    /// </remarks>
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        if (Interlocked.Exchange(ref _initialised, 1) == 0)
        {
            using var scope = host.Services.CreateScope();
            var initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();

            initializer.InitialiseAsync().GetAwaiter().GetResult();
            initializer.SeedAsync().GetAwaiter().GetResult();
        }

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _initialised == 1 && SqlServerAvailability.IsAvailable)
        {
            DropTestDatabase();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Strips the SQL Server registration added by AddInfrastructure before the test one is added.
    /// </summary>
    /// <remarks>
    /// Removing <see cref="DbContextOptions{TContext}"/> alone is not enough on EF Core 9: the
    /// provider is also reached through <c>IDbContextOptionsConfiguration&lt;TContext&gt;</c>, and
    /// leaving that behind means the original connection string wins.
    /// </remarks>
    private static void RemoveDbContextRegistrations(IServiceCollection services)
    {
        var stale = services
            .Where(descriptor =>
                descriptor.ServiceType == typeof(DbContextOptions<ProductManagerDbContext>) ||
                descriptor.ServiceType == typeof(DbContextOptions) ||
                descriptor.ServiceType == typeof(ProductManagerDbContext) ||
                (descriptor.ServiceType.IsGenericType &&
                 descriptor.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>)))
            .ToList();

        foreach (var descriptor in stale)
        {
            services.Remove(descriptor);
        }
    }

    private void DropTestDatabase()
    {
        try
        {
            var options = new DbContextOptionsBuilder<ProductManagerDbContext>()
                .UseSqlServer(SqlServerAvailability.TestDatabaseConnectionString(_databaseName))
                .Options;

            using var context = new ProductManagerDbContext(options);
            context.Database.EnsureDeleted();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // A leftover test database is untidy but harmless, and must never fail the run.
        }
    }
}
