using Market.API;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Market.Tests.ConfigurationTests.UnitTests;

/// <summary>
/// The JWT keys from the repository are public, so AddAPI accepts each one only in its own environment:
/// DEV_ONLY… (appsettings.Development.json) in Development, TESTING_ONLY_… (appsettings.Testing.json) in Testing.
/// </summary>
public class JwtKeyGuardUnitTests
{
    private const string DevOnlyKey = "DEV_ONLY_CHANGE_ME_TO_LONG_RANDOM_SECRET";
    private const string TestingOnlyKey = "TESTING_ONLY_LOCAL_KEY_NOT_A_REAL_SECRET";

    [Theory]
    [InlineData("Development", DevOnlyKey)]
    [InlineData("Testing", TestingOnlyKey)]
    public void AddAPI_PublicKeyInItsOwnEnvironment_IsAccepted(string environmentName, string jwtKey)
    {
        // Act
        var exception = Record.Exception(() => AddApi(environmentName, jwtKey));

        // Assert
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("Production", DevOnlyKey)]
    [InlineData("Production", TestingOnlyKey)]
    [InlineData("Testing", DevOnlyKey)]
    [InlineData("Production", "")]
    public void AddAPI_PublicOrMissingKeyInAnotherEnvironment_StopsStartup(string environmentName, string jwtKey)
    {
        // Act + Assert
        var exception = Assert.Throws<InvalidOperationException>(() => AddApi(environmentName, jwtKey));
        Assert.Contains($"'{environmentName}'", exception.Message);
    }

    private static void AddApi(string environmentName, string jwtKey)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = jwtKey })
            .Build();

        new ServiceCollection().AddAPI(configuration, new FakeHostEnvironment(environmentName));
    }
}
