using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Market.Domain.Entities.Appointments;

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

    [Fact]
    public async Task Delete_DentistWithScheduledAppointment_Returns409()
    {
        // Arrange: a new dentist (the seeded one is shared by other tests) with a scheduled appointment
        var client = await _factory.GetAuthenticatedClientAsync();
        var dentistId = await CreateDentistAsync(client);
        await AddAppointmentAsync(dentistId, AppointmentStatus.Scheduled);

        // Act
        var response = await client.DeleteAsync($"api/dentists/{dentistId}");

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Delete_DentistWithOnlyCancelledAppointment_Succeeds()
    {
        // Arrange: a cancelled appointment doesn't block deleting the dentist
        var client = await _factory.GetAuthenticatedClientAsync();
        var dentistId = await CreateDentistAsync(client);
        await AddAppointmentAsync(dentistId, AppointmentStatus.Cancelled);

        // Act
        var response = await client.DeleteAsync($"api/dentists/{dentistId}");

        // Assert
        response.EnsureSuccessStatusCode();
    }

    private async Task<int> CreateDentistAsync(HttpClient client)
    {
        int specializationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            specializationId = await db.Specializations.Select(x => x.Id).FirstAsync();
        }

        var unique = Guid.NewGuid().ToString("N")[..8];
        var response = await client.PostAsJsonAsync("api/dentists", new
        {
            Firstname = "Test",
            Lastname = "Stomatolog",
            Email = $"dentist-{unique}@test.local",
            Password = "Dentist123!",
            SpecializationId = specializationId
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        return created!.Id;
    }

    private async Task AddAppointmentAsync(int dentistId, AppointmentStatus status)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        var patientId = await db.Patients
            .Where(p => p.User.Email == CustomWebApplicationFactory<Program>.PatientEmail)
            .Select(p => p.Id)
            .SingleAsync();

        var startUtc = DateTime.UtcNow.AddDays(7);

        db.Appointments.Add(new AppointmentEntity
        {
            PatientId = patientId,
            DentistId = dentistId,
            StartUtc = startUtc,
            EndUtc = startUtc.AddMinutes(30),
            Status = status,
            CancelledAtUtc = status == AppointmentStatus.Cancelled ? DateTime.UtcNow : null
        });

        await db.SaveChangesAsync();
    }
}