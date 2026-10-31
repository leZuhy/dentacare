using System.Net;

namespace Market.Tests.AuthTests.IntegrationTests;

/// <summary>
/// Checks that the role from the access token reaches the authorization policies
/// (JwtTokenService writes AppClaimTypes.Role, PolicyNames.Admin requires the Admin role).
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class RoleAuthorizationIntegrationTests
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public RoleAuthorizationIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_AdminOnlyEndpoint_AsAdmin_Returns200()
    {
        // Arrange
        var client = await _factory.GetAuthenticatedClientAsync(
            CustomWebApplicationFactory<Program>.AdminEmail,
            CustomWebApplicationFactory<Program>.AdminPassword);

        // Act
        var response = await client.GetAsync("Currencies/rates");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(CustomWebApplicationFactory<Program>.DentistEmail, CustomWebApplicationFactory<Program>.DentistPassword)]
    [InlineData(CustomWebApplicationFactory<Program>.PatientEmail, CustomWebApplicationFactory<Program>.PatientPassword)]
    public async Task Get_AdminOnlyEndpoint_AsNonAdmin_Returns403(string email, string password)
    {
        // Arrange
        var client = await _factory.GetAuthenticatedClientAsync(email, password);

        // Act
        var response = await client.GetAsync("Currencies/rates");

        // Assert: logged in (not 401), but not allowed (403)
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}