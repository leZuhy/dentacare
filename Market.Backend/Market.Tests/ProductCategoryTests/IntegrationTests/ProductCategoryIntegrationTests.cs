using System.Net;
using System.Net.Http.Json;

namespace Market.Tests.ProductCategoryTests.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class ProductCategoryIntegrationTests
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public ProductCategoryIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_CreateProductCategory_ShouldReturnCreated()
    {
        // Arrange
        var client = await _factory.GetAuthenticatedClientAsync(); // admin
        var request = new
        {
            Name = "Integration Test Category"
        };

        // Act
        var response = await client.PostAsJsonAsync("/ProductCategories", request);

        // Assert
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        Assert.NotNull(result);
        Assert.True(result.ContainsKey("id"));

        var categoryId = result["id"];
        Assert.NotEqual(0, categoryId);
    }

    [Fact]
    public async Task Post_CreateProductCategory_Anonymous_ShouldReturnUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient(); // no Bearer token
        var request = new
        {
            Name = "Anonymous Category"
        };

        // Act
        var response = await client.PostAsJsonAsync("/ProductCategories", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_CreateProductCategory_NonAdmin_ShouldReturnForbidden()
    {
        // Arrange
        var client = await _factory.GetAuthenticatedClientAsync(
            CustomWebApplicationFactory<Program>.EmployeeEmail,
            CustomWebApplicationFactory<Program>.EmployeePassword);
        var request = new
        {
            Name = "Employee Category"
        };

        // Act
        var response = await client.PostAsJsonAsync("/ProductCategories", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
