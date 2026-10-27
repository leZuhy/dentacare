using Market.Application.Modules.Catalog.CatalogHome.Queries.GetCatalogHome;
using Market.Shared.Dtos;
using System.Net;
using System.Net.Http.Json;

namespace Market.Tests.CatalogTests.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class CatalogIntegrationTests
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public CatalogIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_CatalogHome_Anonymous_ReturnsSeededData()
    {
        // Arrange
        var client = _factory.CreateClient(); // no Bearer token

        // Act
        var home = await client.GetFromJsonAsync<GetCatalogHomeQueryDto>("/Catalog/home?categoryLimit=3&productLimit=5");

        // Assert
        Assert.NotNull(home);
        Assert.Equal(3, home.Categories.Count);
        Assert.Equal(5, home.NewestProducts.Count);
        Assert.NotEmpty(home.Promotions);
        Assert.Equal(home.Promotions.OrderBy(p => p.SortOrder).Select(p => p.Id), home.Promotions.Select(p => p.Id));
        Assert.True(home.Stats.TotalProducts >= home.NewestProducts.Count);
    }

    [Fact]
    public async Task Post_Promotion_EndBeforeStart_ShouldReturnFieldErrorOnEndsAtUtc()
    {
        // Arrange
        var client = await _factory.GetAuthenticatedClientAsync(); // admin
        var request = new
        {
            Title = "Pogrešan period",
            ImageUrl = "https://example.com/banner.jpg",
            IsActive = true,
            StartsAtUtc = new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc),
            EndsAtUtc = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            SortOrder = 1
        };

        // Act
        var response = await client.PostAsJsonAsync("/Promotions", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        var fieldError = Assert.Single(error!.Errors!);
        Assert.Equal("endsAtUtc", fieldError.Field);
    }

    [Fact]
    public async Task Get_Promotions_NonAdmin_ShouldReturnForbidden()
    {
        // Arrange
        var client = await _factory.GetAuthenticatedClientAsync(
            CustomWebApplicationFactory<Program>.EmployeeEmail,
            CustomWebApplicationFactory<Program>.EmployeePassword);

        // Act
        var response = await client.GetAsync("/Promotions");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
