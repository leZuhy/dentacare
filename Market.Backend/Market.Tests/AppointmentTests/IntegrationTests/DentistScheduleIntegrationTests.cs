using Market.Application.Modules.Appointments.Queries.GetDentistSchedule;
using Market.Domain.Entities.Appointments;
using Market.Domain.Entities.Catalog;
using Market.Domain.Entities.Clinic;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Market.Application.Modules.Auth.Commands.Register;

namespace Market.Tests.AppointmentTests.IntegrationTests;

/// <summary>
/// GET api/appointments/schedule?date=... (the dentist's schedule for one local day).
/// Every test makes its own dentist, so other tests' data never shows up.
/// Dates are fixed winter days in 2027 (clinic time = UTC+1), written directly into the database.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class DentistScheduleIntegrationTests
{
    private const string DentistPassword = "Dentist123!";

    // Monday, winter (UTC+1): 00:00 local = 17.01. 23:00 UTC
    private static readonly DateOnly Monday = new(2027, 1, 18);
    private static readonly DateOnly Saturday = new(2027, 1, 23);

    private readonly CustomWebApplicationFactory<Program> _factory;

    public DentistScheduleIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private sealed record CreatedResponse(int Id);

    private static string ScheduleUrl(DateOnly date) => $"api/appointments/schedule?date={date:yyyy-MM-dd}";

    private static DateTime Utc(int month, int day, int hour, int minute = 0) =>
        new(2027, month, day, hour, minute, 0, DateTimeKind.Utc);

    // ===== helpers =====

    private async Task<(HttpClient Client, string Email)> RegisterPatientAsync()
    {
        var client = _factory.CreateClient();
        var email = $"schedule-{Guid.NewGuid():N}@test.local";

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

    private async Task<(int Id, string Email)> CreateDentistAsync()
    {
        var admin = await _factory.GetAuthenticatedClientAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var specializationId = await db.Specializations.Select(x => x.Id).FirstAsync();

        var email = $"schedule-dentist-{Guid.NewGuid():N}@test.local";
        var response = await admin.PostAsJsonAsync("api/dentists", new
        {
            Firstname = "Raspored",
            Lastname = "Test",
            Email = email,
            Password = DentistPassword,
            SpecializationId = specializationId
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var id = (await response.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;
        return (id, email);
    }

    /// <summary>New dentist + a client logged in as him.</summary>
    private async Task<(int Id, HttpClient Client)> CreateLoggedInDentistAsync()
    {
        var (id, email) = await CreateDentistAsync();
        var client = await _factory.GetAuthenticatedClientAsync(email, DentistPassword);
        return (id, client);
    }

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

    private async Task AddWorkingHoursAsync(int dentistId, DayOfWeek day, int startHour, int endHour)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        db.WorkingHours.Add(new WorkingHoursEntity
        {
            DentistId = dentistId,
            DayOfWeek = day,
            StartTime = new TimeOnly(startHour, 0),
            EndTime = new TimeOnly(endHour, 0)
        });
        await db.SaveChangesAsync();
    }

    // ===== appointments =====

    [Fact]
    public async Task Schedule_ReturnsOwnAppointmentsOfThatDay_SortedByStart()
    {
        // Arrange: added in the wrong order on purpose; one on Tuesday must not show up
        var (dentistId, dentist) = await CreateLoggedInDentistAsync();
        var (_, patientEmail) = await RegisterPatientAsync();
        var later = await AddAppointmentAsync(patientEmail, dentistId, Utc(1, 18, 9));    // 10:00 local
        var earlier = await AddAppointmentAsync(patientEmail, dentistId, Utc(1, 18, 7));  // 08:00 local
        await AddAppointmentAsync(patientEmail, dentistId, Utc(1, 19, 8));                // Tuesday

        // Act
        var schedule = await dentist.GetFromJsonAsync<GetDentistScheduleQueryDto>(ScheduleUrl(Monday));

        // Assert
        Assert.Equal(Monday, schedule!.Date);
        Assert.Equal([earlier, later], schedule.Appointments.Select(x => x.Id));

        var first = schedule.Appointments[0];
        Assert.Equal("Test Pacijent", first.PatientFullName);
        Assert.Single(first.DentalServiceNames);
        Assert.Equal(50m, first.TotalPriceBam);
    }

    [Fact]
    public async Task Schedule_DoesNotShowOtherDentistsAppointments()
    {
        // Arrange: same day, but the appointment is with dentist A; B asks for his schedule (IDOR)
        var (dentistAId, _) = await CreateDentistAsync();
        var (_, dentistB) = await CreateLoggedInDentistAsync();
        var (_, patientEmail) = await RegisterPatientAsync();
        await AddAppointmentAsync(patientEmail, dentistAId, Utc(1, 18, 9));

        // Act
        var schedule = await dentistB.GetFromJsonAsync<GetDentistScheduleQueryDto>(ScheduleUrl(Monday));

        // Assert
        Assert.Empty(schedule!.Appointments);
    }

    [Fact]
    public async Task Schedule_DayBoundaries_UseClinicTime_NotUtc()
    {
        // Arrange
        var (dentistId, dentist) = await CreateLoggedInDentistAsync();
        var (_, patientEmail) = await RegisterPatientAsync();
        // 18.01. 00:30 local = 17.01. 23:30 UTC -> Monday, although the UTC date is Sunday
        var justAfterMidnight = await AddAppointmentAsync(patientEmail, dentistId, Utc(1, 17, 23, 30));
        // 17.01. 23:30 local = 17.01. 22:30 UTC -> Sunday
        await AddAppointmentAsync(patientEmail, dentistId, Utc(1, 17, 22, 30));

        // Act
        var schedule = await dentist.GetFromJsonAsync<GetDentistScheduleQueryDto>(ScheduleUrl(Monday));

        // Assert
        Assert.Equal([justAfterMidnight], schedule!.Appointments.Select(x => x.Id));
    }

    [Fact]
    public async Task Schedule_CancelledAppointment_IsReturnedWithStatus()
    {
        // Arrange
        var (dentistId, dentist) = await CreateLoggedInDentistAsync();
        var (_, patientEmail) = await RegisterPatientAsync();
        await AddAppointmentAsync(patientEmail, dentistId, Utc(1, 18, 9), AppointmentStatus.Cancelled);

        // Act
        var schedule = await dentist.GetFromJsonAsync<GetDentistScheduleQueryDto>(ScheduleUrl(Monday));

        // Assert
        var appointment = Assert.Single(schedule!.Appointments);
        Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
    }

    // ===== working hours =====

    [Fact]
    public async Task Schedule_ReturnsWorkingHoursOfThatWeekday_Sorted()
    {
        // Arrange: afternoon added first; Tuesday must not show up
        var (dentistId, dentist) = await CreateLoggedInDentistAsync();
        await AddWorkingHoursAsync(dentistId, DayOfWeek.Monday, 13, 17);
        await AddWorkingHoursAsync(dentistId, DayOfWeek.Monday, 8, 12);
        await AddWorkingHoursAsync(dentistId, DayOfWeek.Tuesday, 8, 16);

        // Act
        var schedule = await dentist.GetFromJsonAsync<GetDentistScheduleQueryDto>(ScheduleUrl(Monday));

        // Assert
        Assert.Equal(2, schedule!.WorkingHours.Count);
        Assert.Equal(new TimeOnly(8, 0), schedule.WorkingHours[0].StartTime);
        Assert.Equal(new TimeOnly(12, 0), schedule.WorkingHours[0].EndTime);
        Assert.Equal(new TimeOnly(13, 0), schedule.WorkingHours[1].StartTime);
    }

    [Fact]
    public async Task Schedule_DayWithoutWorkingHours_ReturnsEmptyLists()
    {
        var (dentistId, dentist) = await CreateLoggedInDentistAsync();
        await AddWorkingHoursAsync(dentistId, DayOfWeek.Monday, 8, 16);

        var schedule = await dentist.GetFromJsonAsync<GetDentistScheduleQueryDto>(ScheduleUrl(Saturday));

        Assert.Empty(schedule!.WorkingHours);
        Assert.Empty(schedule.Appointments);
    }

    // ===== access and validation =====

    [Fact]
    public async Task Schedule_AsPatient_Returns403()
    {
        var (patient, _) = await RegisterPatientAsync();

        var response = await patient.GetAsync(ScheduleUrl(Monday));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Schedule_Anonymous_Returns401()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync(ScheduleUrl(Monday));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Schedule_WithoutDate_Returns400()
    {
        var (_, dentist) = await CreateLoggedInDentistAsync();

        var response = await dentist.GetAsync("api/appointments/schedule");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}