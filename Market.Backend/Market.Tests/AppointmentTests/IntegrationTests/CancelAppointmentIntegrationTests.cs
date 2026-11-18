using Market.Application.Modules.Appointments.Queries.GetMyById;
using Market.Application.Modules.Auth.Commands.Register;
using Market.Domain.Entities.Appointments;
using Market.Domain.Entities.Catalog;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Market.Tests.AppointmentTests.IntegrationTests;

/// <summary>
/// POST api/appointments/{id}/cancel (patient or dentist cancels their own appointment).
/// Every test makes its own patients, dentists and appointments (shared database).
/// Appointments are written directly into the database, so any time and status is possible.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class CancelAppointmentIntegrationTests
{
    private const string DentistPassword = "Dentist123!";

    private readonly CustomWebApplicationFactory<Program> _factory;

    public CancelAppointmentIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private sealed record CreatedResponse(int Id);

    private static string CancelUrl(int id) => $"api/appointments/{id}/cancel";

    // ===== helpers =====

    /// <summary>08:00 UTC, the given number of days from today.</summary>
    private static DateTime DaysFromNow(int days) => DateTime.UtcNow.Date.AddDays(days).AddHours(8);

    /// <summary>Registers a new patient. Returns a client logged in as that patient and the email.</summary>
    private async Task<(HttpClient Client, string Email)> RegisterPatientAsync()
    {
        var client = _factory.CreateClient();
        var email = $"cancel-{Guid.NewGuid():N}@test.local";

        var response = await client.PostAsJsonAsync("api/auth/register", new
        {
            Firstname = "Test",
            Lastname = "Pacijent",
            Email = email,
            Password = "Lozinka123",
            Address = "Titova 10",
            City = "Sarajevo"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var tokens = await response.Content.ReadFromJsonAsync<RegisterCommandDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        return (client, email);
    }

    /// <summary>New dentist through the admin API. Returns his id and email (to log in as him).</summary>
    private async Task<(int Id, string Email)> CreateDentistAsync()
    {
        var admin = await _factory.GetAuthenticatedClientAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var specializationId = await db.Specializations.Select(x => x.Id).FirstAsync();

        var email = $"cancel-dentist-{Guid.NewGuid():N}@test.local";
        var response = await admin.PostAsJsonAsync("api/dentists", new
        {
            Firstname = "Otkazivanje",
            Lastname = "Test",
            Email = email,
            Password = DentistPassword,
            SpecializationId = specializationId
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var id = (await response.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;
        return (id, email);
    }

    /// <summary>A service with a unique name. Returns its id.</summary>
    private async Task<int> AddServiceAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        var categoryId = await db.ServiceCategories.Select(x => x.Id).FirstAsync();
        var service = new DentalServiceEntity
        {
            CategoryId = categoryId,
            Name = $"Usluga {Guid.NewGuid():N}",
            PriceBam = 50m,
            DurationMinutes = 30,
            IsActive = true
        };
        db.DentalServices.Add(service);
        await db.SaveChangesAsync();

        return service.Id;
    }

    /// <summary>A 30-minute appointment of the patient with this email, directly in the database.</summary>
    private async Task<int> AddAppointmentAsync(string patientEmail, int dentistId, DateTime startUtc,
        AppointmentStatus status = AppointmentStatus.Scheduled)
    {
        var serviceId = await AddServiceAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        var patientId = await db.Patients
            .Where(p => p.User.Email == patientEmail)
            .Select(p => p.Id)
            .SingleAsync();

        var appointment = new AppointmentEntity
        {
            PatientId = patientId,
            DentistId = dentistId,
            StartUtc = startUtc,
            EndUtc = startUtc.AddMinutes(30),
            Status = status,
            CancelledAtUtc = status == AppointmentStatus.Cancelled ? DateTime.UtcNow : null,
            Services = new List<AppointmentServiceEntity>
            {
                new() { DentalServiceId = serviceId, PriceAtTimeBam = 50m }
            }
        };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        return appointment.Id;
    }

    /// <summary>Reads the appointment straight from the database (to check what was saved).</summary>
    private async Task<AppointmentEntity> GetAppointmentAsync(int id)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        return await db.Appointments.AsNoTracking().SingleAsync(x => x.Id == id);
    }

    private async Task<int> GetPatientUserIdAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        return await db.Patients.Where(p => p.User.Email == email).Select(p => p.UserId).SingleAsync();
    }

    private async Task<int> GetDentistUserIdAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        return await db.Dentists.Where(d => d.User.Email == email).Select(d => d.UserId).SingleAsync();
    }

    // ===== success =====

    [Fact]
    public async Task Cancel_OwnAsPatient_Returns204_AndSavesWhoAndWhen()
    {
        // Arrange
        var (dentistId, _) = await CreateDentistAsync();
        var (client, email) = await RegisterPatientAsync();
        var id = await AddAppointmentAsync(email, dentistId, DaysFromNow(3));

        // Act
        var response = await client.PostAsync(CancelUrl(id), null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var saved = await GetAppointmentAsync(id);
        Assert.Equal(AppointmentStatus.Cancelled, saved.Status);
        Assert.NotNull(saved.CancelledAtUtc);
        Assert.Equal(await GetPatientUserIdAsync(email), saved.CancelledByUserId);
    }

    [Fact]
    public async Task Cancel_OwnAsDentist_Returns204_AndSavesDentistAsCanceller()
    {
        // Arrange
        var (dentistId, dentistEmail) = await CreateDentistAsync();
        var (patientClient, patientEmail) = await RegisterPatientAsync();
        var id = await AddAppointmentAsync(patientEmail, dentistId, DaysFromNow(3));
        var dentistClient = await _factory.GetAuthenticatedClientAsync(dentistEmail, DentistPassword);

        // Act
        var response = await dentistClient.PostAsync(CancelUrl(id), null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var saved = await GetAppointmentAsync(id);
        Assert.Equal(AppointmentStatus.Cancelled, saved.Status);
        Assert.Equal(await GetDentistUserIdAsync(dentistEmail), saved.CancelledByUserId);

        // The patient sees in the details that the dentist cancelled
        var details = await patientClient.GetFromJsonAsync<GetMyAppointmentByIdQueryDto>($"api/appointments/my/{id}");
        Assert.True(details!.CancelledByDentist);
    }

    // ===== IDOR: somebody else's appointment → 404 =====

    [Fact]
    public async Task Cancel_SomebodyElsesAsPatient_Returns404_AndNothingChanges()
    {
        // Arrange: the appointment belongs to patient A, B tries to cancel it
        var (dentistId, _) = await CreateDentistAsync();
        var (_, emailA) = await RegisterPatientAsync();
        var (clientB, _) = await RegisterPatientAsync();
        var idOfA = await AddAppointmentAsync(emailA, dentistId, DaysFromNow(3));

        // Act
        var response = await clientB.PostAsync(CancelUrl(idOfA), null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(AppointmentStatus.Scheduled, (await GetAppointmentAsync(idOfA)).Status);
    }

    [Fact]
    public async Task Cancel_OtherDentistsAppointment_Returns404()
    {
        // Arrange: the appointment is with dentist A, dentist B tries to cancel it
        var (dentistAId, _) = await CreateDentistAsync();
        var (_, dentistBEmail) = await CreateDentistAsync();
        var (_, patientEmail) = await RegisterPatientAsync();
        var id = await AddAppointmentAsync(patientEmail, dentistAId, DaysFromNow(3));
        var dentistB = await _factory.GetAuthenticatedClientAsync(dentistBEmail, DentistPassword);

        // Act
        var response = await dentistB.PostAsync(CancelUrl(id), null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(AppointmentStatus.Scheduled, (await GetAppointmentAsync(id)).Status);
    }

    [Fact]
    public async Task Cancel_Unknown_Returns404()
    {
        var (client, _) = await RegisterPatientAsync();

        var response = await client.PostAsync(CancelUrl(999999), null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ===== business rules → 409 =====

    [Fact]
    public async Task Cancel_LessThan24hBefore_Returns409_AndStaysScheduled()
    {
        // Arrange: starts in 23 hours
        var (dentistId, _) = await CreateDentistAsync();
        var (client, email) = await RegisterPatientAsync();
        var id = await AddAppointmentAsync(email, dentistId, DateTime.UtcNow.AddHours(23));

        // Act
        var response = await client.PostAsync(CancelUrl(id), null);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(AppointmentStatus.Scheduled, (await GetAppointmentAsync(id)).Status);
    }

    [Theory]
    [InlineData(AppointmentStatus.Cancelled)]
    [InlineData(AppointmentStatus.Completed)]
    public async Task Cancel_NotScheduled_Returns409(AppointmentStatus status)
    {
        // Arrange: 3 days ahead, so only the status rule can fail
        var (dentistId, _) = await CreateDentistAsync();
        var (client, email) = await RegisterPatientAsync();
        var id = await AddAppointmentAsync(email, dentistId, DaysFromNow(3), status);

        // Act
        var response = await client.PostAsync(CancelUrl(id), null);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ===== roles and token =====

    [Fact]
    public async Task Cancel_AsAdmin_Returns403()
    {
        var admin = await _factory.GetAuthenticatedClientAsync();

        var response = await admin.PostAsync(CancelUrl(1), null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync(CancelUrl(1), null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}