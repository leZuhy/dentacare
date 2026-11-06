using System.Net;
using System.Net.Http.Json;

namespace Market.Tests.LoyaltyTests.IntegrationTests;

/// <summary>
/// Loyalty tiers are managed by the Admin only. The seeded tiers (Bronze 0/0 %, Silver 500/5 %, Gold 1500/10 %)
/// are in the shared database, so every test that creates a tier uses its own fixed points and discount,
/// growing together, so that the "higher tier, higher discount" rule holds in any test order.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class LoyaltyTiersIntegrationTests
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public LoyaltyTiersIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // Bronze from StaticDataSeeder (HasData, fixed Id)
    private const int BaseTierId = 1;

    private sealed record CreatedResponse(int Id);

    private static Task<HttpResponseMessage> PostTierAsync(HttpClient client, int minPoints, decimal discountPercent)
    {
        var unique = Guid.NewGuid().ToString("N")[..8];

        return client.PostAsJsonAsync("api/loyalty-tiers", new
        {
            Name = $"Nivo {unique}",
            MinPoints = minPoints,
            DiscountPercent = discountPercent
        });
    }

    [Fact]
    public async Task Create_ValidTier_Returns201()
    {
        // Arrange
        var client = await _factory.GetAuthenticatedClientAsync();

        // Act
        var response = await PostTierAsync(client, 100_000, 20m);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_SmallerDiscountThanGold_Returns400()
    {
        // Arrange
        var client = await _factory.GetAuthenticatedClientAsync();

        // Act: more points than Gold (1500), but a smaller discount than its 10 %
        var response = await PostTierAsync(client, 200_000, 8m);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_BaseTier_Returns409()
    {
        // Arrange
        var client = await _factory.GetAuthenticatedClientAsync();

        // Act
        var response = await client.DeleteAsync($"api/loyalty-tiers/{BaseTierId}");

        // Assert: every new patient gets the base tier, so it can't be deleted
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Tier_ThenGetReturns404()
    {
        // Arrange
        var client = await _factory.GetAuthenticatedClientAsync();
        var createResponse = await PostTierAsync(client, 300_000, 30m);
        createResponse.EnsureSuccessStatusCode();
        var tier = await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        // Act
        var deleteResponse = await client.DeleteAsync($"api/loyalty-tiers/{tier!.Id}");

        // Assert: deleted without an error, and it no longer exists
        deleteResponse.EnsureSuccessStatusCode();
        var getResponse = await client.GetAsync($"api/loyalty-tiers/{tier.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Theory]
    [InlineData(CustomWebApplicationFactory<Program>.DentistEmail, CustomWebApplicationFactory<Program>.DentistPassword)]
    [InlineData(CustomWebApplicationFactory<Program>.PatientEmail, CustomWebApplicationFactory<Program>.PatientPassword)]
    public async Task List_AsNonAdmin_Returns403(string email, string password)
    {
        // Arrange
        var client = await _factory.GetAuthenticatedClientAsync(email, password);

        // Act
        var response = await client.GetAsync("api/loyalty-tiers");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}