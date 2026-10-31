using System.Net;
using System.Net.Http.Json;

namespace Market.Tests.ClinicTests.IntegrationTests;

/// <summary>
/// Dentists are managed by the Admin only. A specialization used by a dentist cannot be deleted.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class DentistsIntegrationTests
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public DentistsIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private sealed record CreatedResponse(int Id);

    [Fact]
    public async Task List_AsAdmin_Returns200()
    {
        // Arrange
        var client = await _factory.GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("api/dentists");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(CustomWebApplicationFactory<Program>.DentistEmail, CustomWebApplicationFactory<Program>.DentistPassword)]
    [InlineData(CustomWebApplicationFactory<Program>.PatientEmail, CustomWebApplicationFactory<Program>.PatientPassword)]
    public async Task List_AsNonAdmin_Returns403(string email, string password)
    {
        var client = await _factory.GetAuthenticatedClientAsync(email, password);

        var response = await client.GetAsync("api/dentists");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteSpecialization_UsedByDentist_Returns409()
    {
        // Arrange: a new specialization (seeded ones are shared by other tests) and a dentist using it
        var client = await _factory.GetAuthenticatedClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];

        var specResponse = await client.PostAsJsonAsync("api/specializations", new { Name = $"Test {unique}" });
        specResponse.EnsureSuccessStatusCode();
        var specialization = await specResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        var dentistResponse = await client.PostAsJsonAsync("api/dentists", new
        {
            Firstname = "Test",
            Lastname = "Stomatolog",
            Email = $"dentist-{unique}@test.local",
            Password = "Dentist123!",
            SpecializationId = specialization!.Id
        });
        Assert.Equal(HttpStatusCode.Created, dentistResponse.StatusCode);

        // Act
        var response = await client.DeleteAsync($"api/specializations/{specialization.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}