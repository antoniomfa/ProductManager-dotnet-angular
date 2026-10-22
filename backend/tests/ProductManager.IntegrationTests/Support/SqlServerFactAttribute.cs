using Microsoft.Data.SqlClient;

namespace ProductManager.IntegrationTests.Support;

/// <summary>
/// A fact that needs a real SQL Server instance, and reports itself as skipped rather than
/// failed when one is not reachable.
/// </summary>
/// <remarks>
/// The integration suite runs against SQL Server rather than SQLite because the schema depends on
/// two SQL Server features with no SQLite equivalent: the sequence that supplies product
/// identifiers, and the <c>rowversion</c> concurrency token. Testing against a different provider
/// would mean testing a different schema. These attributes keep <c>dotnet test</c> green on a
/// machine with no SQL Server instead of failing noisily.
/// </remarks>
public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (!SqlServerAvailability.IsAvailable)
        {
            Skip = SqlServerAvailability.SkipReason;
        }
    }
}

/// <inheritdoc cref="SqlServerFactAttribute" />
public sealed class SqlServerTheoryAttribute : TheoryAttribute
{
    public SqlServerTheoryAttribute()
    {
        if (!SqlServerAvailability.IsAvailable)
        {
            Skip = SqlServerAvailability.SkipReason;
        }
    }
}

/// <summary>Probes once per test run for a usable local SQL Server.</summary>
internal static class SqlServerAvailability
{
    public const string MasterConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5";

    public const string SkipReason =
        "Skipped: no SQL Server reachable at (localdb)\\MSSQLLocalDB. Install or start LocalDB (sqllocaldb start MSSQLLocalDB).";

    private static readonly Lazy<bool> _probe = new(() =>
    {
        try
        {
            using var connection = new SqlConnection(MasterConnectionString);
            connection.Open();
            return true;
        }
        catch (SqlException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    });

    public static bool IsAvailable => _probe.Value;

    /// <summary>Connection string for a throwaway database, named uniquely so parallel runs cannot clash.</summary>
    public static string TestDatabaseConnectionString(string databaseName) =>
        $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True";
}
