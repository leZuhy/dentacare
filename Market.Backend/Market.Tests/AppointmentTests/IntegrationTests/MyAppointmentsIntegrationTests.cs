using Market.Application.Common;
using Market.Application.Modules.Appointments.Queries.GetMyById;
using Market.Application.Modules.Appointments.Queries.ListMy;
using Market.Application.Modules.Auth.Commands.Register;
using Market.Domain.Entities.Appointments;
using Market.Domain.Entities.Catalog;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Market.Tests.AppointmentTests.IntegrationTests;

/// <summary>
/// GET api/appointments/my and GET api/appointments/my/{id} ("My appointments").
/// Every test registers its own patients and dentist, so the lists depend only on that test.
/// Appointments are written directly into the database: booking is tested in
/// CreateAppointmentIntegrationTests, and past or cancelled appointments can't be made through the API.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class MyAppointmentsIntegrationTests
{
    private const string Url = "api/appointments/my";

    private readonly CustomWebApplicationFactory<Program> _factory;

    public MyAppointmentsIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private sealed record CreatedResponse(int Id);

    // ===== helpers =====

    /// <summary>08:00 UTC, the given number of days from today (negative = in the past).</summary>
    private static DateTime DaysFromNow(int days) => DateTime.UtcNow.Date.AddDays(days).AddHours(8);

    /// <summary>Registers a new patient. Returns a client logged in as that patient and the email.</summary>
    private async Task<(HttpClient Client, string Email)> RegisterPatientAsync()
    {
        var client = _factory.CreateClient();
        var email = $"my-appointments-{Guid.NewGuid():N}@test.local";

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

    /// <summary>New dentist "Moji Termini" through the admin API (no working hours needed here).</summary>
    private async Task<int> CreateDentistAsync()
    {
        var admin = await _factory.GetAuthenticatedClientAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var specializationId = await db.Specializations.Select(x => x.Id).FirstAsync();

        var response = await admin.PostAsJsonAsync("api/dentists", new
        {
            Firstname = "Moji",
            Lastname = "Termini",
            Email = $"my-appointments-dentist-{Guid.NewGuid():N}@test.local",
            Password = "Dentist123!",
            SpecializationId = specializationId
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;
    }

    /// <summary>A service with a unique name and the given price. Returns its id and name.</summary>
    private async Task<(int Id, string Name)> AddServiceAsync(decimal priceBam)
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
            IsActive = true
        };
        db.DentalServices.Add(service);
        await db.SaveChangesAsync();

        return (service.Id, service.Name);
    }

    /// <summary>
    /// A 30-minute appointment of the patient with this email, directly in the database,
    /// with one row in AppointmentServices per (service, price).
    /// </summary>
    private async Task<int> AddAppointmentAsync(
        string patientEmail,
        int dentistId,
        DateTime startUtc,
        AppointmentStatus status,
        params (int ServiceId, decimal Price)[] services)
    {
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
            Services = services
                .Select(s => new AppointmentServiceEntity
                {
                    DentalServiceId = s.ServiceId,
                    PriceAtTimeBam = s.Price
                })
                .ToList()
        };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        return appointment.Id;
    }

    // ===== list: GET api/appointments/my =====

    [Fact]
    public async Task ListMy_ReturnsOnlyOwnUpcoming_FromNearest()
    {
        // Arrange: patient A has two future appointments, patient B one
        var dentistId = await CreateDentistAsync();
        var service = await AddServiceAsync(50m);
        var (clientA, emailA) = await RegisterPatientAsync();
        var (_, emailB) = await RegisterPatientAsync();

        var later = await AddAppointmentAsync(emailA, dentistId, DaysFromNow(5), AppointmentStatus.Scheduled, (service.Id, 50m));
        var sooner = await AddAppointmentAsync(emailA, dentistId, DaysFromNow(2), AppointmentStatus.Scheduled, (service.Id, 50m));
        var othersId = await AddAppointmentAsync(emailB, dentistId, DaysFromNow(3), AppointmentStatus.Scheduled, (service.Id, 50m));

        // Act
        var page = await clientA.GetFromJsonAsync<PageResult<ListMyAppointmentsQueryDto>>(Url);

        // Assert: only A's appointments, the nearest first
        Assert.Equal(new[] { sooner, later }, page!.Items.Select(x => x.Id));
        Assert.DoesNotContain(page.Items, x => x.Id == othersId);
        Assert.Equal(2, page.TotalItems);
    }

    [Fact]
    public async Task ListMy_PeriodPast_ReturnsOnlyPast_NewestFirst()
    {
        // Arrange
        var dentistId = await CreateDentistAsync();
        var service = await AddServiceAsync(50m);
        var (client, email) = await RegisterPatientAsync();

        var older = await AddAppointmentAsync(email, dentistId, DaysFromNow(-5), AppointmentStatus.Completed, (service.Id, 50m));
        var newer = await AddAppointmentAsync(email, dentistId, DaysFromNow(-1), AppointmentStatus.Completed, (service.Id, 50m));
        await AddAppointmentAsync(email, dentistId, DaysFromNow(3), AppointmentStatus.Scheduled, (service.Id, 50m));

        // Act
        var page = await client.GetFromJsonAsync<PageResult<ListMyAppointmentsQueryDto>>($"{Url}?period=past");

        // Assert: the future one is not here, the latest past one is first
        Assert.Equal(new[] { newer, older }, page!.Items.Select(x => x.Id));
    }

    [Fact]
    public async Task ListMy_StatusFilter_ReturnsOnlyThatStatus()
    {
        // Arrange
        var dentistId = await CreateDentistAsync();
        var service = await AddServiceAsync(50m);
        var (client, email) = await RegisterPatientAsync();

        await AddAppointmentAsync(email, dentistId, DaysFromNow(2), AppointmentStatus.Scheduled, (service.Id, 50m));
        var cancelled = await AddAppointmentAsync(email, dentistId, DaysFromNow(3), AppointmentStatus.Cancelled, (service.Id, 50m));

        // Act
        var page = await client.GetFromJsonAsync<PageResult<ListMyAppointmentsQueryDto>>($"{Url}?status=cancelled");

        // Assert
        var row = Assert.Single(page!.Items);
        Assert.Equal(cancelled, row.Id);
        Assert.Equal(AppointmentStatus.Cancelled, row.Status);
    }

    [Fact]
    public async Task ListMy_Row_HasDentistServicesAndTotal()
    {
        // Arrange: one appointment with two procedures (50 + 30 KM)
        var dentistId = await CreateDentistAsync();
        var first = await AddServiceAsync(50m);
        var second = await AddServiceAsync(30m);
        var (client, email) = await RegisterPatientAsync();
        var start = DaysFromNow(4);

        await AddAppointmentAsync(email, dentistId, start, AppointmentStatus.Scheduled, (first.Id, 50m), (second.Id, 30m));

        // Act
        var page = await client.GetFromJsonAsync<PageResult<ListMyAppointmentsQueryDto>>(Url);

        // Assert
        var row = Assert.Single(page!.Items);
        Assert.Equal(start, row.StartUtc);
        Assert.Equal(start.AddMinutes(30), row.EndUtc);
        Assert.Equal("Moji Termini", row.DentistFullName);
        Assert.Equal(new[] { first.Name, second.Name }, row.DentalServiceNames);
        Assert.Equal(80m, row.TotalPriceBam);
    }

    [Fact]
    public async Task ListMy_Paging_ReturnsOnePage_AndTotal()
    {
        // Arrange: three future appointments, two per page
        var dentistId = await CreateDentistAsync();
        var service = await AddServiceAsync(50m);
        var (client, email) = await RegisterPatientAsync();

        await AddAppointmentAsync(email, dentistId, DaysFromNow(2), AppointmentStatus.Scheduled, (service.Id, 50m));
        await AddAppointmentAsync(email, dentistId, DaysFromNow(3), AppointmentStatus.Scheduled, (service.Id, 50m));
        var last = await AddAppointmentAsync(email, dentistId, DaysFromNow(4), AppointmentStatus.Scheduled, (service.Id, 50m));

        // Act: second page
        var page = await client.GetFromJsonAsync<PageResult<ListMyAppointmentsQueryDto>>(
            $"{Url}?paging.page=2&paging.pageSize=2");

        // Assert
        var row = Assert.Single(page!.Items);
        Assert.Equal(last, row.Id);
        Assert.Equal(3, page.TotalItems);
        Assert.Equal(2, page.TotalPages);
    }

    [Fact]
    public async Task ListMy_UnknownPeriod_Returns400()
    {
        var (client, _) = await RegisterPatientAsync();

        var response = await client.GetAsync($"{Url}?period=7");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ===== details: GET api/appointments/my/{id} =====

    [Fact]
    public async Task GetMyById_Own_Returns200_WithServices()
    {
        // Arrange
        var dentistId = await CreateDentistAsync();
        var first = await AddServiceAsync(50m);
        var second = await AddServiceAsync(30m);
        var (client, email) = await RegisterPatientAsync();
        var id = await AddAppointmentAsync(email, dentistId, DaysFromNow(2), AppointmentStatus.Scheduled, (first.Id, 50m), (second.Id, 30m));

        // Act
        var dto = await client.GetFromJsonAsync<GetMyAppointmentByIdQueryDto>($"{Url}/{id}");

        // Assert
        Assert.Equal(id, dto!.Id);
        Assert.Equal(AppointmentStatus.Scheduled, dto.Status);
        Assert.Null(dto.CancelledAtUtc);
        Assert.Equal("Moji Termini", dto.DentistFullName);
        Assert.False(string.IsNullOrWhiteSpace(dto.DentistSpecializationName));
        Assert.Equal(new[] { first.Name, second.Name }, dto.Services.Select(s => s.Name));
        Assert.Equal(new[] { 50m, 30m }, dto.Services.Select(s => s.PriceAtTimeBam));
        Assert.Equal(80m, dto.TotalPriceBam);
    }

    [Fact]
    public async Task GetMyById_SomebodyElses_Returns404()
    {
        // Arrange: the appointment belongs to patient A, B asks for it (IDOR)
        var dentistId = await CreateDentistAsync();
        var service = await AddServiceAsync(50m);
        var (_, emailA) = await RegisterPatientAsync();
        var (clientB, _) = await RegisterPatientAsync();
        var idOfA = await AddAppointmentAsync(emailA, dentistId, DaysFromNow(2), AppointmentStatus.Scheduled, (service.Id, 50m));

        // Act
        var response = await clientB.GetAsync($"{Url}/{idOfA}");

        // Assert: 404, not 403, so it is not revealed that the appointment exists
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMyById_Unknown_Returns404()
    {
        var (client, _) = await RegisterPatientAsync();

        var response = await client.GetAsync($"{Url}/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ===== roles and token =====

    [Theory]
    [InlineData(CustomWebApplicationFactory<Program>.AdminEmail, CustomWebApplicationFactory<Program>.AdminPassword)]
    [InlineData(CustomWebApplicationFactory<Program>.DentistEmail, CustomWebApplicationFactory<Program>.DentistPassword)]
    public async Task ListMy_AsAdminOrDentist_Returns403(string email, string password)
    {
        var client = await _factory.GetAuthenticatedClientAsync(email, password);

        var response = await client.GetAsync(Url);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListMy_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(Url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyById_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"{Url}/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}