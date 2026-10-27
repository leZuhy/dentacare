using Microsoft.Extensions.Hosting;

namespace Market.Shared.Constants;

/// <summary>
/// Environments of the starter, next to the built-in Development and Production (<see cref="Environments"/>):
/// <list type="bullet">
/// <item><b>Development</b>: local development. SQL Server database Market2026, demo data, Swagger, error details in responses.</item>
/// <item><b>Testing</b>: local, production-like run (launch profile "https-testing"). Its own SQL Server database
/// Market2026_Testing and demo data, but no Swagger and no error details, as in production.</item>
/// <item><b>IntegrationTests</b>: automated tests (Market.Tests). In-memory database, created and seeded on startup.</item>
/// <item><b>Production</b>: starts only with real secrets (e.g. environment variable Jwt__Key).</item>
/// </list>
/// </summary>
public static class EnvironmentNames
{
    public const string Testing = "Testing";
    public const string IntegrationTests = "IntegrationTests";

    public static bool IsTesting(this IHostEnvironment env)
        => env.IsEnvironment(Testing);

    public static bool IsIntegrationTests(this IHostEnvironment env)
        => env.IsEnvironment(IntegrationTests);
}
