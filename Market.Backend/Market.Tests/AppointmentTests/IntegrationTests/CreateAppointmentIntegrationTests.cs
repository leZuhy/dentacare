using Market.Application.Modules.Appointments.Commands.Create;
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
/// POST api/appointments (booking).
/// The database is shared by all tests, so every test creates its own dentist
/// (working hours 08:00-10:00 local), service (30 min, 50 KM) and patient.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class CreateAppointmentIntegrationTests
{
    private const string Url = "api/appointments";

    private readonly CustomWebApplicationFactory<Program> _factory;

    // same zone as Clinic:TimeZoneId in appsettings.json
    private static readonly TimeZoneInfo ClinicZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Sarajevo");

    public CreateAppointmentIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private sealed record CreatedResponse(int Id);

    // ===== helpers =====

    /// <summary>
    /// Local clinic date 7 days from today (always in the future, within 60 days).
    /// </summary>
    private static DateOnly TestDate()
    {
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ClinicZone);
        return DateOnly.FromDateTime(localNow).AddDays(7);
    }

    /// <summary>
    /// Local clinic time on the given date converted to UTC (through the zone, not "+1").
    /// </summary>
    private static DateTime LocalToUtc(DateOnly date, int hour, int minute) =>
        TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(new TimeOnly(hour, minute)), ClinicZone);

    /// <summary>
    /// Registers a new patient. Returns a client logged in as that patient and the patient's email.
    /// </summary>
    private async Task<(HttpClient Client, string Email)> RegisterPatientAsync()
    {
        var client = _factory.CreateClient();
        var email = $"booking-{Guid.NewGuid():N}@test.local";

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
    /// New dentist (through the admin API) with working hours 08:00-10:00 local on the day of the given date.
    /// </summary>
    private async Task<int> CreateDentistAsync(DateOnly date)
    {
        var admin = await _factory.GetAuthenticatedClientAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        var specializationId = await db.Specializations.Select(x => x.Id).FirstAsync();

        var response = await admin.PostAsJsonAsync("api/dentists", new
        {
            Firstname = "Zakazivanje",
            Lastname = "Test",
            Email = $"booking-dentist-{Guid.NewGuid():N}@test.local",
            Password = "Dentist123!",
            SpecializationId = specializationId
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dentistId = (await response.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;

        db.WorkingHours.Add(new WorkingHoursEntity
        {
            DentistId = dentistId,
            DayOfWeek = date.DayOfWeek,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(10, 0)
        });
        await db.SaveChangesAsync();

        return dentistId;
    }

    /// <summary>
    /// A 30-minute service, 50 KM, with a unique name.
    /// </summary>
    private async Task<int> AddServiceAsync(bool isActive = true)
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
            IsActive = isActive
        };
        db.DentalServices.Add(service);
        await db.SaveChangesAsync();

        return service.Id;
    }
    /// <summary>
    /// A cancelled 30-minute appointment of the seeded demo patient, directly in the database.
    /// </summary>
    private async Task AddCancelledAppointmentAsync(int dentistId, DateTime startUtc)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        var patientId = await db.Patients
            .Where(p => p.User.Email == CustomWebApplicationFactory<Program>.PatientEmail)
            .Select(p => p.Id)
            .SingleAsync();

        db.Appointments.Add(new AppointmentEntity
        {
            PatientId = patientId,
            DentistId = dentistId,
            StartUtc = startUtc,
            EndUtc = startUtc.AddMinutes(30),
            Status = AppointmentStatus.Cancelled,
            CancelledAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private static object Body(int dentistId, int serviceId, DateTime startUtc) => new
    {
        DentistId = dentistId,
        DentalServiceId = serviceId,
        StartUtc = startUtc
    };

    // ===== tests =====

    [Fact]
    public async Task Create_FreeSlot_Returns201_AndSavesAppointmentForTokenPatient()
    {
        // Arrange
        var date = TestDate();
        var dentistId = await CreateDentistAsync(date);
        var serviceId = await AddServiceAsync();
        var (client, email) = await RegisterPatientAsync();
        var start = LocalToUtc(date, 8, 0);

        // Act
        var response = await client.PostAsJsonAsync(Url, Body(dentistId, serviceId, start));

        // Assert: response
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<CreateAppointmentCommandDto>();
        Assert.Equal(start, dto!.StartUtc);
        Assert.Equal(start.AddMinutes(30), dto.EndUtc);
        Assert.Equal(50m, dto.PriceBam);

        // Assert: database (patient from the token, price copied, status Scheduled)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var saved = await db.Appointments
            .Include(x => x.Patient).ThenInclude(x => x.User)
            .Include(x => x.Services)
            .SingleAsync(x => x.Id == dto.Id);

        Assert.Equal(email, saved.Patient.User.Email);
        Assert.Equal(dentistId, saved.DentistId);
        Assert.Equal(AppointmentStatus.Scheduled, saved.Status);
        var service = Assert.Single(saved.Services);
        Assert.Equal(serviceId, service.DentalServiceId);
        Assert.Equal(50m, service.PriceAtTimeBam);
    }

    [Fact]
    public async Task Create_SameSlotByAnotherPatient_Returns409()
    {
        // Arrange: first patient books 08:00
        var date = TestDate();
        var dentistId = await CreateDentistAsync(date);
        var serviceId = await AddServiceAsync();
        var start = LocalToUtc(date, 8, 0);

        var (first, _) = await RegisterPatientAsync();
        var firstResponse = await first.PostAsJsonAsync(Url, Body(dentistId, serviceId, start));
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var (second, _) = await RegisterPatientAsync();

        // Act: second patient wants the same slot
        var response = await second.PostAsJsonAsync(Url, Body(dentistId, serviceId, start));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_OverlappingStart_Returns409_TouchingIsAllowed()
    {
        // Arrange: first patient books 08:00-08:30
        var date = TestDate();
        var dentistId = await CreateDentistAsync(date);
        var serviceId = await AddServiceAsync();

        var (first, _) = await RegisterPatientAsync();
        var firstResponse = await first.PostAsJsonAsync(Url, Body(dentistId, serviceId, LocalToUtc(date, 8, 0)));
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var (second, _) = await RegisterPatientAsync();

        // Act
        var overlapping = await second.PostAsJsonAsync(Url, Body(dentistId, serviceId, LocalToUtc(date, 8, 15)));
        var touching = await second.PostAsJsonAsync(Url, Body(dentistId, serviceId, LocalToUtc(date, 8, 30)));

        // Assert: 08:15-08:45 overlaps 08:00-08:30; 08:30-09:00 only touches it
        Assert.Equal(HttpStatusCode.Conflict, overlapping.StatusCode);
        Assert.Equal(HttpStatusCode.Created, touching.StatusCode);
    }

    [Fact]
    public async Task Create_PatientAlreadyBookedWithAnotherDentist_Returns409()
    {
        // Arrange: two dentists, the patient books 08:00 with the first one
        var date = TestDate();
        var firstDentistId = await CreateDentistAsync(date);
        var secondDentistId = await CreateDentistAsync(date);
        var serviceId = await AddServiceAsync();

        var (client, _) = await RegisterPatientAsync();
        var firstResponse = await client.PostAsJsonAsync(Url, Body(firstDentistId, serviceId, LocalToUtc(date, 8, 0)));
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        // Act: the same patient, the other dentist, 08:15 (free there)
        var response = await client.PostAsJsonAsync(Url, Body(secondDentistId, serviceId, LocalToUtc(date, 8, 15)));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_CancelledAppointment_DoesNotBlock()
    {
        // Arrange: a cancelled appointment at 08:00
        var date = TestDate();
        var dentistId = await CreateDentistAsync(date);
        var serviceId = await AddServiceAsync();
        var start = LocalToUtc(date, 8, 0);
        await AddCancelledAppointmentAsync(dentistId, start);

        var (client, _) = await RegisterPatientAsync();

        // Act
        var response = await client.PostAsJsonAsync(Url, Body(dentistId, serviceId, start));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_OutsideWorkingHours_Returns409()
    {
        // Arrange: working hours are 08:00-10:00, the request is 11:00
        var date = TestDate();
        var dentistId = await CreateDentistAsync(date);
        var serviceId = await AddServiceAsync();
        var (client, _) = await RegisterPatientAsync();

        // Act
        var response = await client.PostAsJsonAsync(Url, Body(dentistId, serviceId, LocalToUtc(date, 11, 0)));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_InThePast_Returns409()
    {
        // Arrange: yesterday 08:00, the dentist works on that day of the week
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ClinicZone);
        var yesterday = DateOnly.FromDateTime(localNow).AddDays(-1);
        var dentistId = await CreateDentistAsync(yesterday);
        var serviceId = await AddServiceAsync();
        var (client, _) = await RegisterPatientAsync();

        // Act
        var response = await client.PostAsJsonAsync(Url, Body(dentistId, serviceId, LocalToUtc(yesterday, 8, 0)));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_MoreThan60DaysAhead_Returns409()
    {
        // Arrange: 61 days from today (MaxBookingDaysAhead = 60 in appsettings.json)
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ClinicZone);
        var date = DateOnly.FromDateTime(localNow).AddDays(61);
        var dentistId = await CreateDentistAsync(date);
        var serviceId = await AddServiceAsync();
        var (client, _) = await RegisterPatientAsync();

        // Act
        var response = await client.PostAsJsonAsync(Url, Body(dentistId, serviceId, LocalToUtc(date, 8, 0)));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_UnknownDentist_Returns404()
    {
        // Arrange
        var serviceId = await AddServiceAsync();
        var (client, _) = await RegisterPatientAsync();

        // Act
        var response = await client.PostAsJsonAsync(Url, Body(999_999, serviceId, LocalToUtc(TestDate(), 8, 0)));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_InactiveService_Returns404()
    {
        // Arrange
        var date = TestDate();
        var dentistId = await CreateDentistAsync(date);
        var serviceId = await AddServiceAsync(isActive: false);
        var (client, _) = await RegisterPatientAsync();

        // Act
        var response = await client.PostAsJsonAsync(Url, Body(dentistId, serviceId, LocalToUtc(date, 8, 0)));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_StartWithoutZ_Returns400()
    {
        // Arrange: a string without "Z" is read as DateTimeKind.Unspecified
        var (client, _) = await RegisterPatientAsync();

        // Act
        var response = await client.PostAsJsonAsync(Url, new
        {
            DentistId = 1,
            DentalServiceId = 1,
            StartUtc = "2026-11-20T08:00:00"
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutToken_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(Url, Body(1, 1, LocalToUtc(TestDate(), 8, 0)));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(CustomWebApplicationFactory<Program>.AdminEmail, CustomWebApplicationFactory<Program>.AdminPassword)]
    [InlineData(CustomWebApplicationFactory<Program>.DentistEmail, CustomWebApplicationFactory<Program>.DentistPassword)]
    public async Task Create_AsAdminOrDentist_Returns403(string email, string password)
    {
        // Arrange
        var client = await _factory.GetAuthenticatedClientAsync(email, password);

        // Act
        var response = await client.PostAsJsonAsync(Url, Body(1, 1, LocalToUtc(TestDate(), 8, 0)));

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ScheduleVersion_TwoBookingsReadTheSameVersion_SecondSaveFails()
    {
        // Arrange: two separate DbContexts (= two requests at the same moment) read the same dentist
        var dentistId = await CreateDentistAsync(TestDate());

        using var scopeA = _factory.Services.CreateScope();
        using var scopeB = _factory.Services.CreateScope();
        var dbA = scopeA.ServiceProvider.GetRequiredService<DatabaseContext>();
        var dbB = scopeB.ServiceProvider.GetRequiredService<DatabaseContext>();

        var dentistA = await dbA.Dentists.SingleAsync(x => x.Id == dentistId);
        var dentistB = await dbB.Dentists.SingleAsync(x => x.Id == dentistId);

        // Act: A saves first and changes the version
        dentistA.ScheduleVersion = Guid.NewGuid();
        await dbA.SaveChangesAsync();

        dentistB.ScheduleVersion = Guid.NewGuid();

        // Assert: B still has the old version, so its save is rejected
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => dbB.SaveChangesAsync());
    }
}