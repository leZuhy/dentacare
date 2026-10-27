using Market.Application.Modules.Auth.Commands.ChangePassword;
using Market.Application.Modules.Auth.Commands.Register;
using Market.Application.Modules.Auth.Queries.GetMe;
using Market.Shared.Dtos;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace Market.Tests.AuthTests.IntegrationTests;

/// <summary>
/// Register, forgot/reset password, profile (me) and change password.
/// Every test registers its own user (random email), so the seeded accounts and their cached tokens
/// in <see cref="CustomWebApplicationFactory{TProgram}"/> are never logged out by a password change.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class AccountIntegrationTests
{
    private const string Password = "Lozinka123";

    private readonly CustomWebApplicationFactory<Program> _factory;

    public AccountIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private static string NewEmail() => $"user-{Guid.NewGuid():N}@test.local";

    private HttpClient CreateClient(string? accessToken = null)
    {
        var client = _factory.CreateClient();
        if (accessToken is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    /// <summary>
    /// Registers a new customer with <see cref="Password"/> and returns its email and tokens.
    /// </summary>
    private async Task<(string Email, RegisterCommandDto Tokens)> RegisterAsync()
    {
        var email = NewEmail();
        var response = await CreateClient().PostAsJsonAsync("api/auth/register", new
        {
            Firstname = "Lejla",
            Lastname = "Kovač",
            Email = email,
            Password,
            Address = "Titova 10",
            City = "Sarajevo"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var tokens = await response.Content.ReadFromJsonAsync<RegisterCommandDto>();
        return (email, tokens!);
    }

    private static async Task<FieldErrorDto[]> ReadFieldErrorsAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        return error!.Errors!.ToArray();
    }

    [Fact]
    public async Task Post_Register_NewCustomer_Returns201WithTokens_AndIsLoggedIn()
    {
        // Act
        var (email, tokens) = await RegisterAsync();
        var me = await CreateClient(tokens.AccessToken).GetFromJsonAsync<GetMeQueryDto>("api/auth/me");

        // Assert
        Assert.False(string.IsNullOrEmpty(tokens.AccessToken));
        Assert.False(string.IsNullOrEmpty(tokens.RefreshToken));
        Assert.True(tokens.RefreshTokenExpiresAtUtc > tokens.AccessTokenExpiresAtUtc);
        Assert.Equal(email, me!.Email);
        Assert.Equal("Titova 10", me.Address);
        Assert.Equal("Sarajevo", me.City);
        Assert.False(me.IsAdmin || me.IsManager || me.IsEmployee); // a new account is a customer
    }

    [Fact]
    public async Task Post_Register_EmailAlreadyRegistered_ShouldReturnFieldErrorOnEmail()
    {
        // Arrange: a seeded account, in different case
        var request = new
        {
            Firstname = "Drugi",
            Lastname = "Admin",
            Email = CustomWebApplicationFactory<Program>.AdminEmail.ToUpperInvariant(),
            Password
        };

        // Act
        var response = await CreateClient().PostAsJsonAsync("api/auth/register", request);

        // Assert
        var fieldError = Assert.Single(await ReadFieldErrorsAsync(response));
        Assert.Equal("email", fieldError.Field);
        Assert.Equal("Email je već registrovan.", fieldError.Message);
    }

    [Theory]
    [InlineData("Kratka1")]     // 7 characters
    [InlineData("SamoSlova")]   // no digit
    [InlineData("12345678")]    // no letter
    public async Task Post_Register_WeakPassword_ShouldReturnFieldErrorOnPassword(string password)
    {
        // Act
        var response = await CreateClient().PostAsJsonAsync("api/auth/register", new
        {
            Firstname = "Slaba",
            Lastname = "Lozinka",
            Email = NewEmail(),
            Password = password
        });

        // Assert
        Assert.Equal("password", Assert.Single(await ReadFieldErrorsAsync(response)).Field);
    }

    [Fact]
    public async Task Post_ForgotPassword_UnknownEmail_Returns204AndSendsNoEmail()
    {
        // Arrange
        var email = NewEmail();

        // Act
        var response = await CreateClient().PostAsJsonAsync("api/auth/forgot-password", new { Email = email });

        // Assert: the same response as for an existing account
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(_factory.Emails.SentTo(email));
    }

    [Fact]
    public async Task Post_ForgotAndResetPassword_SetsNewPasswordOnce_AndLogsOutEverywhere()
    {
        // Arrange
        var (email, oldTokens) = await RegisterAsync();
        var anonymous = CreateClient();
        const string newPassword = "NovaLozinka1";

        // Act 1: forgot password → email with the link
        var forgot = await anonymous.PostAsJsonAsync("api/auth/forgot-password", new { Email = email });
        var link = ExtractLink(Assert.Single(_factory.Emails.SentTo(email)).HtmlBody);
        var query = HttpUtility.ParseQueryString(new Uri(link).Query);

        // Act 2: the frontend sends email + token from the link (twice: the second use must fail)
        var resetRequest = new { Email = query["email"], Token = query["token"], NewPassword = newPassword };
        var reset = await anonymous.PostAsJsonAsync("api/auth/reset-password", resetRequest);
        var secondReset = await anonymous.PostAsJsonAsync("api/auth/reset-password", resetRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, forgot.StatusCode);
        Assert.StartsWith($"{CustomWebApplicationFactory<Program>.FrontendBaseUrl}/auth/reset-password?", link);
        Assert.Equal(email, query["email"]);
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
        Assert.Equal("token", Assert.Single(await ReadFieldErrorsAsync(secondReset)).Field);

        var loginNew = await anonymous.PostAsJsonAsync("api/auth/login", new { Email = email, Password = newPassword });
        var loginOld = await anonymous.PostAsJsonAsync("api/auth/login", new { Email = email, Password });
        var refreshOld = await anonymous.PostAsJsonAsync("api/auth/refresh", new { oldTokens.RefreshToken });
        var meWithOldAccessToken = await CreateClient(oldTokens.AccessToken).GetAsync("api/auth/me");

        Assert.Equal(HttpStatusCode.OK, loginNew.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, loginOld.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshOld.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, meWithOldAccessToken.StatusCode);
    }

    [Fact]
    public async Task Put_Me_ChangesProfile_ButNotEmail()
    {
        // Arrange
        var (email, tokens) = await RegisterAsync();
        var client = CreateClient(tokens.AccessToken);

        // Act: "Email" is not part of the request and is ignored
        var put = await client.PutAsJsonAsync("api/auth/me", new
        {
            Firstname = "  Amra ",
            Lastname = "Hodžić",
            Address = "Ferhadija 1",
            City = "",
            Email = "drugi@test.local"
        });
        var me = await client.GetFromJsonAsync<GetMeQueryDto>("api/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);
        Assert.Equal("Amra", me!.Firstname);
        Assert.Equal("Hodžić", me.Lastname);
        Assert.Equal("Ferhadija 1", me.Address);
        Assert.Null(me.City); // an empty field is stored as "no value"
        Assert.Equal(email, me.Email);
    }

    [Fact]
    public async Task Get_Me_Anonymous_Returns401()
    {
        // Act
        var response = await CreateClient().GetAsync("api/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_ChangePassword_WrongCurrentPassword_ShouldReturnFieldErrorOnCurrentPassword()
    {
        // Arrange
        var (_, tokens) = await RegisterAsync();

        // Act
        var response = await CreateClient(tokens.AccessToken).PostAsJsonAsync("api/auth/change-password", new
        {
            CurrentPassword = "PogresnaLozinka1",
            NewPassword = "Promijenjena2"
        });

        // Assert
        Assert.Equal("currentPassword", Assert.Single(await ReadFieldErrorsAsync(response)).Field);
    }

    [Fact]
    public async Task Post_ChangePassword_ReturnsNewTokens_AndRevokesOldSession()
    {
        // Arrange
        var (email, oldTokens) = await RegisterAsync();
        var anonymous = CreateClient();
        const string newPassword = "Promijenjena2";

        // Act
        var response = await CreateClient(oldTokens.AccessToken).PostAsJsonAsync("api/auth/change-password", new
        {
            CurrentPassword = Password,
            NewPassword = newPassword
        });
        var newTokens = await response.Content.ReadFromJsonAsync<ChangePasswordCommandDto>();

        // Assert: old tokens no longer work, this client continues with the new ones
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(oldTokens.RefreshToken, newTokens!.RefreshToken);

        var refreshOld = await anonymous.PostAsJsonAsync("api/auth/refresh", new { oldTokens.RefreshToken });
        var meWithOldAccessToken = await CreateClient(oldTokens.AccessToken).GetAsync("api/auth/me");
        var meWithNewAccessToken = await CreateClient(newTokens.AccessToken).GetAsync("api/auth/me");
        var refreshNew = await anonymous.PostAsJsonAsync("api/auth/refresh", new { newTokens.RefreshToken });
        var loginNew = await anonymous.PostAsJsonAsync("api/auth/login", new { Email = email, Password = newPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, refreshOld.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, meWithOldAccessToken.StatusCode);
        Assert.Equal(HttpStatusCode.OK, meWithNewAccessToken.StatusCode);
        Assert.Equal(HttpStatusCode.OK, refreshNew.StatusCode);
        Assert.Equal(HttpStatusCode.OK, loginNew.StatusCode);
    }

    /// <summary>
    /// The reset link from the email body (href of the link, HTML-decoded: "&amp;amp;" → "&amp;").
    /// </summary>
    private static string ExtractLink(string htmlBody)
    {
        var match = Regex.Match(htmlBody, "href=\"([^\"]+)\"");
        Assert.True(match.Success, "The email contains no link.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
}
