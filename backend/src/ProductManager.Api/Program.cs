using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using ProductManager.Api.Configuration;
using ProductManager.Api.ErrorHandling;
using ProductManager.Api.Filters;
using ProductManager.Application;
using ProductManager.Infrastructure;
using ProductManager.Infrastructure.Persistence;
using ProductManager.Infrastructure.Seeding;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext());

builder.Services.AddControllers(options =>
    {
        // Registered globally so a new endpoint cannot ship without its payload being validated.
        options.Filters.Add<FluentValidationFilter>();
    })
    .AddJsonOptions(options =>
    {
        // Enums travel as names ("LowStock"), not ordinals, so the contract stays readable and
        // stays valid if a member is ever inserted.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Product Manager API",
        Version = "v1",
        Description =
            "Manages a product catalogue: CRUD, stock movements, name search and stock-level " +
            "queries. Product identifiers are unique 6-digit numbers allocated from a database " +
            "sequence, so they stay unique across concurrent API instances.",
    });

    // Surfaces the XML documentation from every project in the output folder, so the DTO and
    // controller comments written here become the API reference in Swagger UI.
    foreach (var xmlFile in Directory.EnumerateFiles(AppContext.BaseDirectory, "ProductManager.*.xml"))
    {
        options.IncludeXmlComments(xmlFile, includeControllerXmlComments: true);
    }
});

var corsOptions = builder.Configuration
    .GetSection(CorsOptions.SectionName)
    .Get<CorsOptions>() ?? new CorsOptions();

builder.Services.AddCors(options => options.AddPolicy(
    CorsOptions.PolicyName,
    policy => policy
        .WithOrigins(corsOptions.AllowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<ProductManagerDbContext>("database");

var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();

// Enabled by configuration. Set Swagger:Enabled to false to
// turn it off in a real deployment.
if (app.Configuration.GetValue("Swagger:Enabled", defaultValue: true))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Manager API v1");
        options.DocumentTitle = "Product Manager API";
    });
}

app.UseCors(CorsOptions.PolicyName);
app.MapControllers();
app.MapHealthChecks("/health");

app.MapGet("/", () => Results.Redirect("/swagger"))
    .ExcludeFromDescription();

await InitialiseDatabaseAsync(app);

await app.RunAsync();

// Brings the schema up to date and seeds starter data at startup.
static async Task InitialiseDatabaseAsync(WebApplication app)
{
    if (!app.Configuration.GetValue("Database:AutoMigrateAndSeed", defaultValue: false))
    {
        return;
    }

    using var scope = app.Services.CreateScope();
    var initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();

    await initializer.InitialiseAsync();
    await initializer.SeedAsync();
}

/// <summary>
/// Exposed so the integration test project can drive the real application through
/// WebApplicationFactory rather than reconstructing the pipeline itself.
/// </summary>
public partial class Program
{
    private Program()
    {
    }
}
