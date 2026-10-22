using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProductManager.Application.Categories;
using ProductManager.Application.Products;
using ProductManager.Application.Products.Validators;

namespace ProductManager.Application;

/// <summary>
/// Registers the application layer. Each layer owns its own registration extension so the
/// composition root stays a short, readable list rather than a catalogue of every service.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddValidatorsFromAssemblyContaining<CreateProductRequestValidator>(ServiceLifetime.Singleton);

        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();

        // Injected rather than calling DateTimeOffset.UtcNow directly, so tests can pin "now".
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
