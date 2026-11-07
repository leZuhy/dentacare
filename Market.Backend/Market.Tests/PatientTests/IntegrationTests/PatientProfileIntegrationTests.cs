using Market.Application.Modules.Auth.Commands.Register;
using Market.Application.Modules.Patients.Commands.UpdateMyPatientProfile;
using Market.Application.Modules.Patients.Queries.GetMyPatientProfile;
using Market.Shared.Dtos;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Market.Tests.PatientTests.IntegrationTests;

/// <summary>
/// GET and PUT api/patients/me. Every test registers its own patient, so tests that change
/// the profile don't affect each other or the seeded demo patient.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class PatientProfileIntegrationTests
{
    private const string Url = "api/patients/me";

    private readonly CustomWebApplicationFactory<Program> _factory;

    public PatientProfileIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Registers a new patient and returns a client that is logged in as that patient.
    /// </summary>
    private async Task<HttpClient> RegisterPatientClientAsync()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("api/auth/register", new
        {
            Firstname = "Amra",
            Lastname = "Hodžić",
            Email = $"patient-{Guid.NewGuid():N}@test.local",
            Password = "Lozinka123",
            Address = "Titova 10",
            City = "Sarajevo"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var tokens = await response.Content.ReadFromJsonAsync<RegisterCommandDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        return client;
    }

    private static async Task<FieldErrorDto[]> ReadFieldErrorsAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        return error!.Errors!.ToArray();
    }

    [Fact]
    public async Task Get_NewPatient_ReturnsEmptyProfile()
    {
        // Arrange
        var client = await RegisterPatientClientAsync();

        // Act
        var profile = await client.GetFromJsonAsync<GetMyPatientProfileQueryDto>(Url);

        // Assert: phone and date of birth are not asked at registration
        Assert.Null(profile!.Phone);
        Assert.Null(profile.DateOfBirth);
    }

    [Fact]
    public async Task Put_ValidData_Returns204_AndIsSaved()
    {
        // Arrange
        var client = await RegisterPatientClientAsync();

        // Act
        var put = await client.PutAsJsonAsync(Url, new UpdateMyPatientProfileCommand
        {
            Phone = "  061 123 456  ",
            DateOfBirth = new DateOnly(1995, 4, 20)
        });
        var profile = await client.GetFromJsonAsync<GetMyPatientProfileQueryDto>(Url);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);
        Assert.Equal("061 123 456", profile!.Phone); // spaces at the ends are removed
        Assert.Equal(new DateOnly(1995, 4, 20), profile.DateOfBirth);
    }

    [Fact]
    public async Task Put_NullValues_ClearsProfile()
    {
        // Arrange: first save some data
        var client = await RegisterPatientClientAsync();
        await client.PutAsJsonAsync(Url, new UpdateMyPatientProfileCommand
        {
            Phone = "061 123 456",
            DateOfBirth = new DateOnly(1995, 4, 20)
        });

        // Act
        var put = await client.PutAsJsonAsync(Url, new UpdateMyPatientProfileCommand
        {
            Phone = null,
            DateOfBirth = null
        });
        var profile = await client.GetFromJsonAsync<GetMyPatientProfileQueryDto>(Url);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);
        Assert.Null(profile!.Phone);
        Assert.Null(profile.DateOfBirth);
    }

    [Fact]
    public async Task Put_InvalidPhone_Returns400OnPhone()
    {
        // Arrange
        var client = await RegisterPatientClientAsync();

        // Act
        var response = await client.PutAsJsonAsync(Url, new UpdateMyPatientProfileCommand { Phone = "061 12" });

        // Assert
        var errors = await ReadFieldErrorsAsync(response);
        Assert.Contains(errors, e => e.Field == "phone");
    }

    [Fact]
    public async Task Put_FutureDateOfBirth_Returns400OnDateOfBirth()
    {
        // Arrange
        var client = await RegisterPatientClientAsync();
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

        // Act
        var response = await client.PutAsJsonAsync(Url, new UpdateMyPatientProfileCommand { DateOfBirth = tomorrow });

        // Assert
        var errors = await ReadFieldErrorsAsync(response);
        Assert.Contains(errors, e => e.Field == "dateOfBirth");
    }

    [Theory]
    [InlineData(CustomWebApplicationFactory<Program>.AdminEmail, CustomWebApplicationFactory<Program>.AdminPassword)]
    [InlineData(CustomWebApplicationFactory<Program>.DentistEmail, CustomWebApplicationFactory<Program>.DentistPassword)]
    public async Task GetAndPut_AsNonPatient_Returns403(string email, string password)
    {
        // Arrange
        var client = await _factory.GetAuthenticatedClientAsync(email, password);

        // Act
        var get = await client.GetAsync(Url);
        var put = await client.PutAsJsonAsync(Url, new UpdateMyPatientProfileCommand { Phone = "061 123 456" });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, get.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, put.StatusCode);
    }

    [Fact]
    public async Task GetAndPut_WithoutToken_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var get = await client.GetAsync(Url);
        var put = await client.PutAsJsonAsync(Url, new UpdateMyPatientProfileCommand { Phone = "061 123 456" });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, get.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, put.StatusCode);
    }
}