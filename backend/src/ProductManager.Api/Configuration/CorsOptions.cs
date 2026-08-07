namespace ProductManager.Api.Configuration;

/// <summary>
/// Origins permitted to call the API from a browser. Configured rather than hard-coded so the
/// Angular app's address can differ between local development and a deployed environment.
/// </summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>Named policy applied to the whole API.</summary>
    public const string PolicyName = "ProductManagerClients";

    /// <summary>Exact origins allowed, for example https://localhost:4200.</summary>
    public string[] AllowedOrigins { get; set; } = [];
}
