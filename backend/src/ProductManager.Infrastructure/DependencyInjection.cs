using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductManager.Application.Abstractions;
using ProductManager.Infrastructure.Persistence;
using ProductManager.Infrastructure.Seeding;

namespace ProductManager.Infrastructure;

/// <summary>Registers the database and the services that depend on it.</summary>
public static class DependencyInjection
{
    public const string ConnectionStringName = "ProductManagerDatabase";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured.");

        services.AddDbContext<ProductManagerDbContext>(options =>
            options.UseSqlServer(connectionString, sqlServer =>
            {
                sqlServer.MigrationsAssembly(typeof(ProductManagerDbContext).Assembly.FullName);

                // Retries brief network blips and deadlocks inside the provider, instead of
                // letting them surface as a 500.
                sqlServer.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
            }));

        // The application layer talks to the interface; this is the only place the concrete
        // context is named.
        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ProductManagerDbContext>());

        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();

        return services;
    }
}
