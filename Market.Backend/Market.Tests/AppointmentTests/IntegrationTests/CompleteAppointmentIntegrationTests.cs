using Market.Domain.Entities.Appointments;
using Market.Domain.Entities.Catalog;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace Market.Tests.AppointmentTests.IntegrationTests;

/// <summary>
/// POST api/appointments/{id}/complete (the dentist completes own appointment).
/// Every test makes its own patients, dentists, services and appointments (shared database).
/// Appointments are written directly into the database, so any time and status is possible.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class CompleteAppointmentIntegrationTests
{
    private const string DentistPassword = "Dentist123!";
    private const string PatientPassword = "Lozinka123";

    private readonly CustomWebApplicationFactory<Program> _factory;

    public CompleteAppointmentIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private sealed record CreatedResponse(int Id);

    private static string CompleteUrl(int id) => $"api/appointments/{id}/complete";

    private static object Body(string findings = "Karijes na zubu 36.", string? therapy = "Plomba",
        params int[] serviceIds) => new
    {
        Findings = findings,
        Therapy = therapy,
        AdditionalDentalServiceIds = serviceIds
    };

    // ===== helpers =====

    /// <summary>Started one hour ago (so it can be completed now).</summary>
    private static DateTime StartedHourAgo() => DateTime.UtcNow.AddHours(-1);

    private async Task<string> RegisterPatientAsync()
    {
        var client = _factory.CreateClient();
        var email = $"complete-{Guid.NewGuid():N}@test.local";

        var response = await client.PostAsJsonAsync("api/auth/register", new
        {
            Firstname = "Test",
            Lastname = "Pacijent",
            Email = email,
            Password = PatientPassword,
            Address = "Titova 10",
            City = "Sarajevo"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return email;
    }

    /// <summary>New dentist through the admin API. Returns his id and a client logged in as him.</summary>
    private async Task<(int Id, HttpClient Client)> CreateDentistAsync()
    {
        var admin = await _factory.GetAuthenticatedClientAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var specializationId = await db.Specializations.Select(x => x.Id).FirstAsync();

        var email = $"complete-dentist-{Guid.NewGuid():N}@test.local";
        var response = await admin.PostAsJsonAsync("api/dentists", new
        {
            Firstname = "Zavrsetak",
            Lastname = "Test",
            Email = email,
            Password = DentistPassword,
            SpecializationId = specializationId
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var id = (await response.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;
        var client = await _factory.GetAuthenticatedClientAsync(email, DentistPassword);
        return (id, client);
    }

    /// <summary>A service with a unique name. Returns its id.</summary>
    private async Task<int> AddServiceAsync(decimal priceBam = 50m, bool isActive = true)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        var categoryId = await db.ServiceCategories.Select(x => x.Id).FirstAsync();
        var service = new DentalServiceEntity
        {
            CategoryId = categoryId,
            Name = $"Usluga {Guid.NewGuid():N}",
            PriceBam = priceBam,
            DurationMinutes = 30,
            IsActive = isActive
        };
        db.DentalServices.Add(service);
        await db.SaveChangesAsync();

        return service.Id;
    }

    /// <summary>A 30-minute appointment of the patient, with the booked service at 50 KM.</summary>
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

    /// <summary>The appointment with its services and medical record, straight from the database.</summary>
    private async Task<AppointmentEntity> GetAppointmentAsync(int id)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        return await db.Appointments
            .AsNoTracking()
            .Include(x => x.Services)
            .Include(x => x.MedicalRecord)
            .SingleAsync(x => x.Id == id);
    }

    private async Task SetServicePriceAsync(int serviceId, decimal priceBam)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var service = await db.DentalServices.SingleAsync(x => x.Id == serviceId);
        service.PriceBam = priceBam;
        await db.SaveChangesAsync();
    }

    /// <summary>Checks that nothing was saved: still Scheduled, no record, only the booked service.</summary>
    private async Task AssertUnchangedAsync(int id)
    {
        var saved = await GetAppointmentAsync(id);
        Assert.Equal(AppointmentStatus.Scheduled, saved.Status);
        Assert.Null(saved.CompletedAtUtc);
        Assert.Null(saved.MedicalRecord);
        Assert.Single(saved.Services);
    }

    // ===== success =====

    [Fact]
    public async Task Complete_Own_Returns204_SavesStatusRecordAndCopiedPrices()
    {
        // Arrange: one extra service twice (two procedures) and one more service
        var (dentistId, dentist) = await CreateDentistAsync();
        var patientEmail = await RegisterPatientAsync();
        var id = await AddAppointmentAsync(patientEmail, dentistId, StartedHourAgo());
        var fillingId = await AddServiceAsync(priceBam: 80m);
        var xRayId = await AddServiceAsync(priceBam: 20.50m);

        // Act
        var response = await dentist.PostAsJsonAsync(CompleteUrl(id),
            Body("  Karijes na 36 i 37.  ", "  ", fillingId, fillingId, xRayId));

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var saved = await GetAppointmentAsync(id);
        Assert.Equal(AppointmentStatus.Completed, saved.Status);
        Assert.NotNull(saved.CompletedAtUtc);

        // Record: text trimmed, empty therapy saved as null
        Assert.NotNull(saved.MedicalRecord);
        Assert.Equal("Karijes na 36 i 37.", saved.MedicalRecord!.Findings);
        Assert.Null(saved.MedicalRecord.Therapy);

        // Booked service stays, 3 new rows with copied prices: 50 + 80 + 80 + 20.50
        Assert.Equal(4, saved.Services.Count);
        Assert.Equal(2, saved.Services.Count(s => s.DentalServiceId == fillingId));
        Assert.All(saved.Services.Where(s => s.DentalServiceId == fillingId), s => Assert.Equal(80m, s.PriceAtTimeBam));
        Assert.Equal(230.50m, saved.Services.Sum(s => s.PriceAtTimeBam));
    }

    [Fact]
    public async Task Complete_PriceChangesLater_SavedPriceStaysTheSame()
    {
        // Arrange
        var (dentistId, dentist) = await CreateDentistAsync();
        var patientEmail = await RegisterPatientAsync();
        var id = await AddAppointmentAsync(patientEmail, dentistId, StartedHourAgo());
        var serviceId = await AddServiceAsync(priceBam: 80m);

        // Act: complete, then change the price list
        var response = await dentist.PostAsJsonAsync(CompleteUrl(id), Body("Nalaz", "Terapija", serviceId));
        await SetServicePriceAsync(serviceId, 120m);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var saved = await GetAppointmentAsync(id);
        Assert.Equal(80m, saved.Services.Single(s => s.DentalServiceId == serviceId).PriceAtTimeBam);
        Assert.Equal("Terapija", saved.MedicalRecord!.Therapy);
    }

    [Fact]
    public async Task Complete_WithoutExtraServices_KeepsOnlyBookedService()
    {
        var (dentistId, dentist) = await CreateDentistAsync();
        var patientEmail = await RegisterPatientAsync();
        var id = await AddAppointmentAsync(patientEmail, dentistId, StartedHourAgo());

        var response = await dentist.PostAsJsonAsync(CompleteUrl(id), Body());

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var saved = await GetAppointmentAsync(id);
        Assert.Equal(AppointmentStatus.Completed, saved.Status);
        Assert.Single(saved.Services);
    }

    // ===== IDOR: somebody else's appointment → 404 =====

    [Fact]
    public async Task Complete_OtherDentistsAppointment_Returns404_AndNothingChanges()
    {
        // Arrange: the appointment is with dentist A, dentist B tries to complete it
        var (dentistAId, _) = await CreateDentistAsync();
        var (_, dentistB) = await CreateDentistAsync();
        var patientEmail = await RegisterPatientAsync();
        var id = await AddAppointmentAsync(patientEmail, dentistAId, StartedHourAgo());

        // Act
        var response = await dentistB.PostAsJsonAsync(CompleteUrl(id), Body());

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertUnchangedAsync(id);
    }

    [Fact]
    public async Task Complete_Unknown_Returns404()
    {
        var (_, dentist) = await CreateDentistAsync();

        var response = await dentist.PostAsJsonAsync(CompleteUrl(999999), Body());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ===== business rules → 409 =====

    [Theory]
    [InlineData(AppointmentStatus.Cancelled)]
    [InlineData(AppointmentStatus.Completed)]
    public async Task Complete_NotScheduled_Returns409(AppointmentStatus status)
    {
        var (dentistId, dentist) = await CreateDentistAsync();
        var patientEmail = await RegisterPatientAsync();
        var id = await AddAppointmentAsync(patientEmail, dentistId, StartedHourAgo(), status);

        var response = await dentist.PostAsJsonAsync(CompleteUrl(id), Body());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Complete_BeforeStart_Returns409_AndNothingChanges()
    {
        // Arrange: starts in one hour
        var (dentistId, dentist) = await CreateDentistAsync();
        var patientEmail = await RegisterPatientAsync();
        var id = await AddAppointmentAsync(patientEmail, dentistId, DateTime.UtcNow.AddHours(1));

        // Act
        var response = await dentist.PostAsJsonAsync(CompleteUrl(id), Body());

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertUnchangedAsync(id);
    }

    [Fact]
    public async Task Complete_InactiveService_Returns409_AndNothingChanges()
    {
        // Arrange: one active and one inactive extra service
        var (dentistId, dentist) = await CreateDentistAsync();
        var patientEmail = await RegisterPatientAsync();
        var id = await AddAppointmentAsync(patientEmail, dentistId, StartedHourAgo());
        var activeId = await AddServiceAsync();
        var inactiveId = await AddServiceAsync(isActive: false);

        // Act
        var response = await dentist.PostAsJsonAsync(CompleteUrl(id), Body("Nalaz", null, activeId, inactiveId));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertUnchangedAsync(id);
    }

    [Fact]
    public async Task Complete_UnknownService_Returns409()
    {
        var (dentistId, dentist) = await CreateDentistAsync();
        var patientEmail = await RegisterPatientAsync();
        var id = await AddAppointmentAsync(patientEmail, dentistId, StartedHourAgo());

        var response = await dentist.PostAsJsonAsync(CompleteUrl(id), Body("Nalaz", null, 999999));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertUnchangedAsync(id);
    }

    [Fact]
    public async Task Complete_Twice_SecondReturns409_AndKeepsFirstRecord()
    {
        // Arrange: completed once
        var (dentistId, dentist) = await CreateDentistAsync();
        var patientEmail = await RegisterPatientAsync();
        var id = await AddAppointmentAsync(patientEmail, dentistId, StartedHourAgo());
        var first = await dentist.PostAsJsonAsync(CompleteUrl(id), Body("Prvi nalaz"));
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        // Act
        var second = await dentist.PostAsJsonAsync(CompleteUrl(id), Body("Drugi nalaz"));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var saved = await GetAppointmentAsync(id);
        Assert.Equal("Prvi nalaz", saved.MedicalRecord!.Findings);
    }

    // ===== validation → 400 =====

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Complete_EmptyFindings_Returns400_AndNothingChanges(string findings)
    {
        var (dentistId, dentist) = await CreateDentistAsync();
        var patientEmail = await RegisterPatientAsync();
        var id = await AddAppointmentAsync(patientEmail, dentistId, StartedHourAgo());

        var response = await dentist.PostAsJsonAsync(CompleteUrl(id), Body(findings));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertUnchangedAsync(id);
    }

    // ===== roles and token =====

    [Fact]
    public async Task Complete_AsPatient_Returns403()
    {
        // Even the patient of this appointment can't complete it
        var (dentistId, _) = await CreateDentistAsync();
        var patientEmail = await RegisterPatientAsync();
        var id = await AddAppointmentAsync(patientEmail, dentistId, StartedHourAgo());
        var patient = await _factory.GetAuthenticatedClientAsync(patientEmail, PatientPassword);

        var response = await patient.PostAsJsonAsync(CompleteUrl(id), Body());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertUnchangedAsync(id);
    }

    [Fact]
    public async Task Complete_AsAdmin_Returns403()
    {
        var admin = await _factory.GetAuthenticatedClientAsync();

        var response = await admin.PostAsJsonAsync(CompleteUrl(1), Body());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Complete_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(CompleteUrl(1), Body());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
