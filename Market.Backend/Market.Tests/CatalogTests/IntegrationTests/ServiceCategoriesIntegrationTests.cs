using System.Net;

namespace Market.Tests.CatalogTests.IntegrationTests;

/// <summary>
/// Service categories are available to the Admin only.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class ServiceCategoriesIntegrationTests
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public ServiceCategoriesIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task List_AsAdmin_Returns200()
    {
        // Arrange
        var client = await _factory.GetAuthenticatedClientAsync(
            CustomWebApplicationFactory<Program>.AdminEmail,
            CustomWebApplicationFactory<Program>.AdminPassword);

        // Act
        var response = await client.GetAsync("api/service-categories");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(CustomWebApplicationFactory<Program>.DentistEmail, CustomWebApplicationFactory<Program>.DentistPassword)]
    [InlineData(CustomWebApplicationFactory<Program>.PatientEmail, CustomWebApplicationFactory<Program>.PatientPassword)]
    public async Task List_AsNonAdmin_Returns403(string email, string password)
    {
        // Arrange
        var client = await _factory.GetAuthenticatedClientAsync(email, password);

        // Act
        var response = await client.GetAsync("api/service-categories");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
