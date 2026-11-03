using System.Net;
using System.Net.Http.Json;

namespace Market.Tests.ClinicTests.IntegrationTests;

/// <summary>
/// Working hours are managed by the Admin only. Overlap rules are checked through the API,
/// and deleting a dentist also deletes their working hours.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class WorkingHoursIntegrationTests
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public WorkingHoursIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private sealed record CreatedResponse(int Id);

    // A new dentist for every test, so the seeded dentist's hours (Mon-Fri 08-16) and other tests don't interfere
    private static async Task<int> CreateDentistAsync(HttpClient client)
    {
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
        dentistResponse.EnsureSuccessStatusCode();
        var dentist = await dentistResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        return dentist!.Id;
    }

    private static Task<HttpResponseMessage> PostMondayAsync(HttpClient client, int dentistId, string start, string end)
    {
        return client.PostAsJsonAsync("api/working-hours", new
        {
            DentistId = dentistId,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = start,
            EndTime = end
        });
    }

    [Fact]
    public async Task Create_OverlappingInterval_Returns400()
    {
        // Arrange
        var client = await _factory.GetAuthenticatedClientAsync();
        var dentistId = await CreateDentistAsync(client);
        (await PostMondayAsync(client, dentistId, "08:00:00", "12:00:00")).EnsureSuccessStatusCode();

        // Act
        var response = await PostMondayAsync(client, dentistId, "11:00:00", "16:00:00");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_TouchingInterval_Returns201()
    {
        var client = await _factory.GetAuthenticatedClientAsync();
        var dentistId = await CreateDentistAsync(client);
        (await PostMondayAsync(client, dentistId, "08:00:00", "12:00:00")).EnsureSuccessStatusCode();

        var response = await PostMondayAsync(client, dentistId, "12:00:00", "16:00:00");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_SameIntervalForOtherDentist_Returns201()
    {
        // Arrange: dentist A works Monday 08-12
        var client = await _factory.GetAuthenticatedClientAsync();
        var dentistA = await CreateDentistAsync(client);
        var dentistB = await CreateDentistAsync(client);
        (await PostMondayAsync(client, dentistA, "08:00:00", "12:00:00")).EnsureSuccessStatusCode();

        // Act: dentist B gets the same interval
        var response = await PostMondayAsync(client, dentistB, "08:00:00", "12:00:00");

        // Assert: the overlap rule is per dentist
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDentist_AlsoDeletesWorkingHours()
    {
        // Arrange
        var client = await _factory.GetAuthenticatedClientAsync();
        var dentistId = await CreateDentistAsync(client);
        var createResponse = await PostMondayAsync(client, dentistId, "08:00:00", "12:00:00");
        createResponse.EnsureSuccessStatusCode();
        var workingHours = await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        // Act
        var deleteResponse = await client.DeleteAsync($"api/dentists/{dentistId}");

        // Assert: the dentist is deleted without an error, and their working hours are gone too
        deleteResponse.EnsureSuccessStatusCode();
        var getResponse = await client.GetAsync($"api/working-hours/{workingHours!.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Theory]
    [InlineData(CustomWebApplicationFactory<Program>.DentistEmail, CustomWebApplicationFactory<Program>.DentistPassword)]
    [InlineData(CustomWebApplicationFactory<Program>.PatientEmail, CustomWebApplicationFactory<Program>.PatientPassword)]
    public async Task List_AsNonAdmin_Returns403(string email, string password)
    {
        var client = await _factory.GetAuthenticatedClientAsync(email, password);

        var response = await client.GetAsync("api/working-hours");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}