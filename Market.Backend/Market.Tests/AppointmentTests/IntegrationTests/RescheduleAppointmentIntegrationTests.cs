using Market.Application.Modules.Auth.Commands.Register;
using Market.Domain.Entities.Appointments;
using Market.Domain.Entities.Catalog;
using Market.Domain.Entities.Clinic;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Market.Tests.AppointmentTests.IntegrationTests;

/// <summary>
/// POST api/appointments/{id}/reschedule (patient moves own appointment to another free slot).
/// Every test makes its own dentist (working hours 08:00-10:00 local on the test day),
/// patient and appointments. The old appointment is written directly into the database.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class RescheduleAppointmentIntegrationTests
{
    private const string DentistPassword = "Dentist123!";

    private readonly CustomWebApplicationFactory<Program> _factory;

    // same zone as Clinic:TimeZoneId in appsettings.json
    private static readonly TimeZoneInfo ClinicZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Sarajevo");

    public RescheduleAppointmentIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private sealed record CreatedResponse(int Id);

    private static string RescheduleUrl(int id) => $"api/appointments/{id}/reschedule";

    private static object Body(DateTime startUtc) => new { StartUtc = startUtc };

    // ===== helpers =====

    /// <summary>Local clinic date, the given number of days from today.</summary>
    private static DateOnly LocalDate(int daysFromToday)
    {
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ClinicZone);
        return DateOnly.FromDateTime(localNow).AddDays(daysFromToday);
    }

    /// <summary>Test day: 7 days from today (more than 24 h away, within 60 days).</summary>
    private static DateOnly TestDate() => LocalDate(7);

    /// <summary>Local clinic time on the given date converted to UTC (through the zone, not "+1").</summary>
    private static DateTime LocalToUtc(DateOnly date, int hour, int minute) =>
        TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(new TimeOnly(hour, minute)), ClinicZone);

    /// <summary>Registers a new patient. Returns a client logged in as that patient and the email.</summary>
    private async Task<(HttpClient Client, string Email)> RegisterPatientAsync()
    {
        var client = _factory.CreateClient();
        var email = $"reschedule-{Guid.NewGuid():N}@test.local";

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

    /// <summary>
    /// New dentist through the admin API, with working hours 08:00-10:00 local
    /// on the weekday of the given date. Returns his id and email (to log in as him).
    /// </summary>
    private async Task<(int Id, string Email)> CreateDentistAsync(DateOnly date)
    {
        var admin = await _factory.GetAuthenticatedClientAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var specializationId = await db.Specializations.Select(x => x.Id).FirstAsync();

        var email = $"reschedule-dentist-{Guid.NewGuid():N}@test.local";
        var response = await admin.PostAsJsonAsync("api/dentists", new
        {
            Firstname = "Pomjeranje",
            Lastname = "Test",
            Email = email,
            Password = DentistPassword,
            SpecializationId = specializationId
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = (await response.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;

        db.WorkingHours.Add(new WorkingHoursEntity
        {
            DentistId = id,
            DayOfWeek = date.DayOfWeek,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(10, 0)
        });
        await db.SaveChangesAsync();

        return (id, email);
    }

    /// <summary>A 30-minute service, 50 KM, with a unique name. Returns its id.</summary>
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

    /// <summary>An appointment of the patient with this email, directly in the database.</summary>
    private async Task<int> AddAppointmentAsync(string patientEmail, int dentistId, DateTime startUtc,
        AppointmentStatus status = AppointmentStatus.Scheduled, int durationMinutes = 30)
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
            EndUtc = startUtc.AddMinutes(durationMinutes),
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
        return await db.Appointments
            .AsNoTracking()
            .Include(x => x.Services)
            .SingleAsync(x => x.Id == id);
    }

    private async Task<Guid> GetScheduleVersionAsync(int dentistId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        return await db.Dentists.Where(x => x.Id == dentistId).Select(x => x.ScheduleVersion).SingleAsync();
    }

    // ===== success =====

    [Fact]
    public async Task Reschedule_ToFreeSlot_Returns204_MovesTimes_AndChangesScheduleVersion()
    {
        // Arrange: 08:00-08:30, move to 09:00
        var date = TestDate();
        var (dentistId, _) = await CreateDentistAsync(date);
        var (client, email) = await RegisterPatientAsync();
        var id = await AddAppointmentAsync(email, dentistId, LocalToUtc(date, 8, 0));
        var versionBefore = await GetScheduleVersionAsync(dentistId);
        var newStart = LocalToUtc(date, 9, 0);

        // Act
        var response = await client.PostAsJsonAsync(RescheduleUrl(id), Body(newStart));

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var saved = await GetAppointmentAsync(id);
        Assert.Equal(newStart, saved.StartUtc);
        Assert.Equal(newStart.AddMinutes(30), saved.EndUtc);
        Assert.Equal(AppointmentStatus.Scheduled, saved.Status);
        Assert.Equal(dentistId, saved.DentistId);
        Assert.Single(saved.Services);   // services and price are not touched

        Assert.NotEqual(versionBefore, await GetScheduleVersionAsync(dentistId));
    }

    [Fact]
    public async Task Reschedule_OverlappingOwnOldTime_Returns204()
    {
        // Arrange: 08:00-08:30, move to 08:15-08:45.
        // Fails if the appointment blocks itself (excludeAppointmentId is missing).
        var date = TestDate();
        var (dentistId, _) = await CreateDentistAsync(date);
        var (client, email) = await RegisterPatientAsync();
        var id = await AddAppointmentAsync(email, dentistId, LocalToUtc(date, 8, 0));
        var newStart = LocalToUtc(date, 8, 15);

        // Act
        var response = await client.PostAsJsonAsync(RescheduleUrl(id), Body(newStart));

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(newStart, (await GetAppointmentAsync(id)).StartUtc);
    }

    [Fact]
    public async Task Reschedule_KeepsOldDuration_NotCurrentServiceDuration()
    {
        // Arrange: the appointment lasts 45 min, the service now lasts 30 min
        var date = TestDate();
        var (dentistId, _) = await CreateDentistAsync(date);
        var (client, email) = await RegisterPatientAsync();
        var id = await AddAppointmentAsync(email, dentistId, LocalToUtc(date, 8, 0), durationMinutes: 45);
        var newStart = LocalToUtc(date, 9, 0);

        // Act
        var response = await client.PostAsJsonAsync(RescheduleUrl(id), Body(newStart));

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(newStart.AddMinutes(45), (await GetAppointmentAsync(id)).EndUtc);
    }

    // ===== IDOR and missing =====

    [Fact]
    public async Task Reschedule_SomebodyElses_Returns404_AndNothingChanges()
    {
        // Arrange: the appointment belongs to patient A, B tries to move it
        var date = TestDate();
        var (dentistId, _) = await CreateDentistAsync(date);
        var (_, emailA) = await RegisterPatientAsync();
        var (clientB, _) = await RegisterPatientAsync();
        var oldStart = LocalToUtc(date, 8, 0);
        var idOfA = await AddAppointmentAsync(emailA, dentistId, oldStart);

        // Act
        var response = await clientB.PostAsJsonAsync(RescheduleUrl(idOfA), Body(LocalToUtc(date, 9, 0)));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(oldStart, (await GetAppointmentAsync(idOfA)).StartUtc);
    }

    [Fact]
    public async Task Reschedule_Unknown_Returns404()
    {
        var (client, _) = await RegisterPatientAsync();

        var response = await client.PostAsJsonAsync(RescheduleUrl(999999), Body(LocalToUtc(TestDate(), 9, 0)));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ===== business rules → 409 =====

    [Fact]
    public async Task Reschedule_LessThan24hBeforeOldStart_Returns409()
    {
        // Arrange: the old appointment starts in 23 hours
        var (dentistId, _) = await CreateDentistAsync(TestDate());
        var (client, email) = await RegisterPatientAsync();
        var oldStart = DateTime.UtcNow.AddHours(23);
        var id = await AddAppointmentAsync(email, dentistId, oldStart);

        // Act
        var response = await client.PostAsJsonAsync(RescheduleUrl(id), Body(LocalToUtc(TestDate(), 9, 0)));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(oldStart, (await GetAppointmentAsync(id)).StartUtc);
    }

    [Theory]
    [InlineData(AppointmentStatus.Cancelled)]
    [InlineData(AppointmentStatus.Completed)]
    public async Task Reschedule_NotScheduled_Returns409(AppointmentStatus status)
    {
        var date = TestDate();
        var (dentistId, _) = await CreateDentistAsync(date);
        var (client, email) = await RegisterPatientAsync();
        var id = await AddAppointmentAsync(email, dentistId, LocalToUtc(date, 8, 0), status);

        var response = await client.PostAsJsonAsync(RescheduleUrl(id), Body(LocalToUtc(date, 9, 0)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Reschedule_SameTime_Returns409()
    {
        var date = TestDate();
        var (dentistId, _) = await CreateDentistAsync(date);
        var (client, email) = await RegisterPatientAsync();
        var start = LocalToUtc(date, 8, 0);
        var id = await AddAppointmentAsync(email, dentistId, start);

        var response = await client.PostAsJsonAsync(RescheduleUrl(id), Body(start));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Reschedule_SlotTakenByAnotherPatient_Returns409_AndNothingChanges()
    {
        // Arrange: another patient already has 09:00 with the same dentist
        var date = TestDate();
        var (dentistId, _) = await CreateDentistAsync(date);
        var (client, email) = await RegisterPatientAsync();
        var (_, otherEmail) = await RegisterPatientAsync();
        var oldStart = LocalToUtc(date, 8, 0);
        var id = await AddAppointmentAsync(email, dentistId, oldStart);
        await AddAppointmentAsync(otherEmail, dentistId, LocalToUtc(date, 9, 0));

        // Act
        var response = await client.PostAsJsonAsync(RescheduleUrl(id), Body(LocalToUtc(date, 9, 0)));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(oldStart, (await GetAppointmentAsync(id)).StartUtc);
    }

    [Fact]
    public async Task Reschedule_PatientBusyWithOtherDentist_Returns409()
    {
        // Arrange: the same patient has 09:00 with dentist B, moves A's appointment to 09:00
        var date = TestDate();
        var (dentistAId, _) = await CreateDentistAsync(date);
        var (dentistBId, _) = await CreateDentistAsync(date);
        var (client, email) = await RegisterPatientAsync();
        var id = await AddAppointmentAsync(email, dentistAId, LocalToUtc(date, 8, 0));
        await AddAppointmentAsync(email, dentistBId, LocalToUtc(date, 9, 0));

        // Act
        var response = await client.PostAsJsonAsync(RescheduleUrl(id), Body(LocalToUtc(date, 9, 0)));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Reschedule_OutsideWorkingHours_Returns409()
    {
        // Working hours are 08:00-10:00, 12:00 is not a slot
        var date = TestDate();
        var (dentistId, _) = await CreateDentistAsync(date);
        var (client, email) = await RegisterPatientAsync();
        var id = await AddAppointmentAsync(email, dentistId, LocalToUtc(date, 8, 0));

        var response = await client.PostAsJsonAsync(RescheduleUrl(id), Body(LocalToUtc(date, 12, 0)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Reschedule_MoreThan60DaysAhead_Returns409()
    {
        var date = TestDate();
        var (dentistId, _) = await CreateDentistAsync(date);
        var (client, email) = await RegisterPatientAsync();
        var id = await AddAppointmentAsync(email, dentistId, LocalToUtc(date, 8, 0));

        var response = await client.PostAsJsonAsync(RescheduleUrl(id), Body(LocalToUtc(LocalDate(61), 9, 0)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ===== format → 400 =====

    [Fact]
    public async Task Reschedule_StartNotUtc_Returns400()
    {
        // Arrange: no "Z" at the end, so the time is not UTC
        var date = TestDate();
        var (dentistId, _) = await CreateDentistAsync(date);
        var (client, email) = await RegisterPatientAsync();
        var id = await AddAppointmentAsync(email, dentistId, LocalToUtc(date, 8, 0));

        // Act
        var response = await client.PostAsJsonAsync(RescheduleUrl(id), new { StartUtc = "2030-01-10T09:00:00" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ===== roles and token =====

    [Fact]
    public async Task Reschedule_AsDentist_Returns403()
    {
        // Even his own appointment: only the patient can move it
        var date = TestDate();
        var (dentistId, dentistEmail) = await CreateDentistAsync(date);
        var (_, patientEmail) = await RegisterPatientAsync();
        var id = await AddAppointmentAsync(patientEmail, dentistId, LocalToUtc(date, 8, 0));
        var dentist = await _factory.GetAuthenticatedClientAsync(dentistEmail, DentistPassword);

        var response = await dentist.PostAsJsonAsync(RescheduleUrl(id), Body(LocalToUtc(date, 9, 0)));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Reschedule_AsAdmin_Returns403()
    {
        var admin = await _factory.GetAuthenticatedClientAsync();

        var response = await admin.PostAsJsonAsync(RescheduleUrl(1), Body(LocalToUtc(TestDate(), 9, 0)));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Reschedule_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(RescheduleUrl(1), Body(LocalToUtc(TestDate(), 9, 0)));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}