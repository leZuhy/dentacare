using System.Net;
using System.Net.Http.Json;

namespace Market.Tests.CatalogTests.IntegrationTests;

/// <summary>
/// Dental services are managed by the Admin only. A service is deactivated instead of deleted,
/// and a category that has services cannot be deleted.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class DentalServicesIntegrationTests
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public DentalServicesIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private sealed record CreatedResponse(int Id);
    private sealed record ListItem(int Id, bool IsActive);
    private sealed record PageResponse(List<ListItem> Items, int TotalItems);

    // A new category for every test, so seeded services and other tests don't interfere
    private static async Task<int> CreateCategoryAsync(HttpClient client)
    {
        var unique = Guid.NewGuid().ToString("N")[..8];

        var response = await client.PostAsJsonAsync("api/service-categories", new { Name = $"Test {unique}" });
        response.EnsureSuccessStatusCode();
        var category = await response.Content.ReadFromJsonAsync<CreatedResponse>();

        return category!.Id;
    }

    // A random name, because seeded services ("Plomba", "Pregled"...) already exist in the shared database
    private static Task<HttpResponseMessage> PostServiceAsync(HttpClient client, int categoryId)
    {
        var unique = Guid.NewGuid().ToString("N")[..8];

        return client.PostAsJsonAsync("api/dental-services", new
        {
            CategoryId = categoryId,
            Name = $"Usluga {unique}",
            PriceBam = 80m,
            DurationMinutes = 45
        });
    }

    [Fact]
    public async Task Create_ValidService_Returns201()
    {
        // Arrange
        var client = await _factory.GetAuthenticatedClientAsync();
        var categoryId = await CreateCategoryAsync(client);

        // Act
        var response = await PostServiceAsync(client, categoryId);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_UnknownCategory_Returns400()
    {
        // Arrange
        var client = await _factory.GetAuthenticatedClientAsync();

        // Act
        var response = await PostServiceAsync(client, 999999);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_WithService_Returns409()
    {
        // Arrange: a category with one service
        var client = await _factory.GetAuthenticatedClientAsync();
        var categoryId = await CreateCategoryAsync(client);
        (await PostServiceAsync(client, categoryId)).EnsureSuccessStatusCode();

        // Act
        var response = await client.DeleteAsync($"api/service-categories/{categoryId}");

        // Assert: a category in use cannot be deleted
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task List_FilterInactive_ReturnsOnlyDeactivatedService()
    {
        // Arrange: two services in a new category, one of them deactivated
        var client = await _factory.GetAuthenticatedClientAsync();
        var categoryId = await CreateCategoryAsync(client);

        (await PostServiceAsync(client, categoryId)).EnsureSuccessStatusCode();

        var inactiveResponse = await PostServiceAsync(client, categoryId);
        inactiveResponse.EnsureSuccessStatusCode();
        var inactive = await inactiveResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        var putResponse = await client.PutAsJsonAsync($"api/dental-services/{inactive!.Id}", new
        {
            CategoryId = categoryId,
            Name = $"Usluga {Guid.NewGuid().ToString("N")[..8]}",
            PriceBam = 80m,
            DurationMinutes = 45,
            IsActive = false
        });
        putResponse.EnsureSuccessStatusCode();

        // Act
        var list = await client.GetFromJsonAsync<PageResponse>(
            $"api/dental-services?categoryId={categoryId}&isActive=false&paging.page=1&paging.pageSize=10");

        // Assert: only the deactivated service is returned
        Assert.NotNull(list);
        Assert.Single(list.Items);
        Assert.Equal(inactive.Id, list.Items[0].Id);
        Assert.False(list.Items[0].IsActive);
    }

    [Theory]
    [InlineData(CustomWebApplicationFactory<Program>.DentistEmail, CustomWebApplicationFactory<Program>.DentistPassword)]
    [InlineData(CustomWebApplicationFactory<Program>.PatientEmail, CustomWebApplicationFactory<Program>.PatientPassword)]
    public async Task List_AsNonAdmin_Returns403(string email, string password)
    {
        // Arrange
        var client = await _factory.GetAuthenticatedClientAsync(email, password);

        // Act
        var response = await client.GetAsync("api/dental-services");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}