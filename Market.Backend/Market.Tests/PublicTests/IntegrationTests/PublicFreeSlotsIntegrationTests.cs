using System.Net;
using System.Net.Http.Json;
using Market.Domain.Entities.Appointments;
using Market.Domain.Entities.Catalog;
using Market.Domain.Entities.Clinic;
using Microsoft.Extensions.DependencyInjection;

namespace Market.Tests.PublicTests.IntegrationTests;

/// <summary>
/// Free slots (api/public/free-slots): visible without login.
/// The database is shared by all tests, so every test creates its own dentist,
/// working hours (08:00-10:00 local) and service (30 min, unique name).
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class PublicFreeSlotsIntegrationTests
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    // same zone as Clinic:TimeZoneId in appsettings.json
    private static readonly TimeZoneInfo ClinicZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Sarajevo");

    public PublicFreeSlotsIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private sealed record CreatedResponse(int Id);
    private sealed record SlotItem(DateTime StartUtc, DateTime EndUtc);

    /// <summary>
    /// Local clinic date 7 days from today (always in the future, within the 14-day range).
    /// </summary>
    private static DateOnly TestDate()
    {
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ClinicZone);
        return DateOnly.FromDateTime(localNow).AddDays(7);
    }

    /// <summary>
    /// Local clinic time on the given date converted to UTC.
    /// Through the zone, not "+1", because the date may be after a clock change.
    /// </summary>
    private static DateTime LocalToUtc(DateOnly date, int hour, int minute) =>
        TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(new TimeOnly(hour, minute)), ClinicZone);

    private static string Url(int dentistId, int serviceId, DateOnly fromDate, DateOnly? toDate = null)
    {
        var url = $"api/public/free-slots?dentistId={dentistId}&dentalServiceId={serviceId}&fromDate={fromDate:yyyy-MM-dd}";
        return toDate is null ? url : $"{url}&toDate={toDate:yyyy-MM-dd}";
    }

    private async Task<int> CreateDentistAsync()
    {
        var admin = await _factory.GetAuthenticatedClientAsync();

        int specializationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            specializationId = await db.Specializations.Select(x => x.Id).FirstAsync();
        }

        var unique = Guid.NewGuid().ToString("N")[..8];
        var response = await admin.PostAsJsonAsync("api/dentists", new
        {
            Firstname = "Slobodni",
            Lastname = "Termini",
            Email = $"free-slots-{unique}@test.local",
            Password = "Dentist123!",
            SpecializationId = specializationId
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        return created!.Id;
    }

    /// <summary>
    /// Working hours 08:00-10:00 (local) on the day of the week of the given date.
    /// </summary>
    private async Task AddWorkingHoursAsync(int dentistId, DateOnly date)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        db.WorkingHours.Add(new WorkingHoursEntity
        {
            DentistId = dentistId,
            DayOfWeek = date.DayOfWeek,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(10, 0)
        });
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// A 30-minute service with a unique name, in the first seeded category.
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
    /// A 30-minute appointment of the seeded patient, directly in the database (booking comes in #13).
    /// </summary>
    private async Task AddAppointmentAsync(int dentistId, DateTime startUtc, AppointmentStatus status)
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
            Status = status,
            CancelledAtUtc = status == AppointmentStatus.Cancelled ? DateTime.UtcNow : null
        });
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Dentist with working hours on the test date and an active 30-minute service.
    /// </summary>
    private async Task<(int DentistId, int ServiceId, DateOnly Date)> ArrangeDentistAsync()
    {
        var date = TestDate();
        var dentistId = await CreateDentistAsync();
        await AddWorkingHoursAsync(dentistId, date);
        var serviceId = await AddServiceAsync();
        return (dentistId, serviceId, date);
    }

    [Fact]
    public async Task FreeSlots_AsAnonymous_Returns200WithSlots()
    {
        // Arrange
        var (dentistId, serviceId, date) = await ArrangeDentistAsync();
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(Url(dentistId, serviceId, date));

        // Assert: 08:00, 08:15 ... 09:30 = 7 slots of 30 min
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var slots = await response.Content.ReadFromJsonAsync<List<SlotItem>>();
        Assert.Equal(7, slots!.Count);

        Assert.Equal(LocalToUtc(date, 8, 0), slots[0].StartUtc);
        Assert.Equal(LocalToUtc(date, 8, 30), slots[0].EndUtc);

        Assert.Equal(LocalToUtc(date, 9, 30), slots[^1].StartUtc);
        Assert.Equal(LocalToUtc(date, 10, 0), slots[^1].EndUtc);
    }

    [Fact]
    public async Task FreeSlots_ScheduledAppointment_BlocksOverlappingSlots()
    {
        // Arrange: appointment 08:30-09:00 local
        var (dentistId, serviceId, date) = await ArrangeDentistAsync();
        await AddAppointmentAsync(dentistId, LocalToUtc(date, 8, 30), AppointmentStatus.Scheduled);
        var client = _factory.CreateClient();

        // Act
        var slots = await client.GetFromJsonAsync<List<SlotItem>>(Url(dentistId, serviceId, date));

        // Assert: 08:15, 08:30 and 08:45 overlap; 08:00 and 09:00 only touch it
        var starts = slots!.Select(x => x.StartUtc).ToList();
        Assert.Equal(4, starts.Count);
        Assert.Contains(LocalToUtc(date, 8, 0), starts);
        Assert.Contains(LocalToUtc(date, 9, 0), starts);
        Assert.DoesNotContain(LocalToUtc(date, 8, 15), starts);
        Assert.DoesNotContain(LocalToUtc(date, 8, 30), starts);
        Assert.DoesNotContain(LocalToUtc(date, 8, 45), starts);
    }

    [Fact]
    public async Task FreeSlots_CancelledAppointment_DoesNotBlock()
    {
        // Arrange: cancelled appointment 08:30-09:00 local
        var (dentistId, serviceId, date) = await ArrangeDentistAsync();
        await AddAppointmentAsync(dentistId, LocalToUtc(date, 8, 30), AppointmentStatus.Cancelled);
        var client = _factory.CreateClient();

        // Act
        var slots = await client.GetFromJsonAsync<List<SlotItem>>(Url(dentistId, serviceId, date));

        // Assert: all 7 slots are still free
        Assert.Equal(7, slots!.Count);
        Assert.Contains(slots, x => x.StartUtc == LocalToUtc(date, 8, 30));
    }

    [Fact]
    public async Task FreeSlots_UnknownDentist_Returns404()
    {
        // Arrange
        var serviceId = await AddServiceAsync();
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(Url(999_999, serviceId, TestDate()));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task FreeSlots_InactiveService_Returns404()
    {
        // Arrange
        var date = TestDate();
        var dentistId = await CreateDentistAsync();
        await AddWorkingHoursAsync(dentistId, date);
        var serviceId = await AddServiceAsync(isActive: false);
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(Url(dentistId, serviceId, date));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task FreeSlots_RangeLongerThan14Days_Returns400()
    {
        // Arrange: from + 14 days = 15 days with both dates included
        var date = TestDate();
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(Url(1, 1, date, date.AddDays(14)));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FreeSlots_ToDateBeforeFromDate_Returns400()
    {
        // Arrange
        var date = TestDate();
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(Url(1, 1, date, date.AddDays(-1)));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FreeSlots_MoreThan60DaysAhead_ReturnsEmptyList()
    {
        // Arrange: the dentist works on that day, but it is after MaxBookingDaysAhead (60)
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ClinicZone);
        var date = DateOnly.FromDateTime(localNow).AddDays(61);
        var dentistId = await CreateDentistAsync();
        await AddWorkingHoursAsync(dentistId, date);
        var serviceId = await AddServiceAsync();
        var client = _factory.CreateClient();

        // Act
        var slots = await client.GetFromJsonAsync<List<SlotItem>>(Url(dentistId, serviceId, date));

        // Assert: not an error, just no slots
        Assert.Empty(slots!);
    }
}
