using FluentValidation;
using Market.Application.Abstractions;
using Market.Application.Common;
using Market.Application.Common.Exceptions;
using Market.Application.Modules.Auth.Commands.ChangePassword;
using Market.Application.Modules.Auth.Commands.ForgotPassword;
using Market.Application.Modules.Auth.Commands.Login;
using Market.Application.Modules.Auth.Commands.Refresh;
using Market.Application.Modules.Auth.Commands.ResetPassword;
using Market.Domain.Entities.Identity;
using Market.Infrastructure.Common;
using Market.Shared.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Time.Testing;

namespace Market.Tests.AuthTests.UnitTests;

/// <summary>
/// Time-dependent account rules, with a <see cref="FakeTimeProvider"/> instead of waiting.
/// </summary>
public class AccountUnitTests
{
    private const string Password = "Lozinka123";

    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero));
    private readonly PasswordHasher<MarketUserEntity> _hasher = new();

    private DatabaseContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Each test gets a new database
            .Options;

        return new DatabaseContext(options, _time);
    }

    private async Task<MarketUserEntity> AddUserAsync(DatabaseContext context)
    {
        var user = new MarketUserEntity
        {
            Firstname = "Test",
            Lastname = "Kupac",
            Email = "kupac@test.local",
            IsEnabled = true
        };
        user.PasswordHash = _hasher.HashPassword(user, Password);

        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private JwtTokenService CreateJwtTokenService()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            Issuer = "Market.Tests",
            Audience = "Market.Tests",
            Key = "UNIT_TESTS_ONLY_KEY_0123456789_ABCDEFGHIJ"
        });

        return new JwtTokenService(options, _time);
    }

    [Fact]
    public async Task ResetPassword_ExpiredToken_ThrowsFieldErrorOnToken()
    {
        // Arrange: a token issued now...
        using var context = GetInMemoryDbContext();
        var user = await AddUserAsync(context);
        const string token = "token-from-the-link";

        context.PasswordResetTokens.Add(new PasswordResetTokenEntity
        {
            UserId = user.Id,
            TokenHash = SecureToken.Hash(token),
            ExpiresAtUtc = _time.GetUtcNow().UtcDateTime.Add(ForgotPasswordCommandHandler.TokenLifetime)
        });
        await context.SaveChangesAsync();

        // ...and used one minute after it expired
        _time.Advance(ForgotPasswordCommandHandler.TokenLifetime + TimeSpan.FromMinutes(1));
        var handler = new ResetPasswordCommandHandler(context, _hasher, _time);
        var command = new ResetPasswordCommand { Email = user.Email, Token = token, NewPassword = "NovaLozinka1" };

        // Act + Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal(nameof(ResetPasswordCommand.Token), Assert.Single(exception.Errors).PropertyName);
    }

    [Fact]
    public async Task Refresh_OldTokenAfterPasswordChange_IsRejected_ButDoesNotCloseTheNewSession()
    {
        // Arrange: the user is logged in on another device, then changes the password on this device
        using var context = GetInMemoryDbContext();
        var user = await AddUserAsync(context);
        var jwt = CreateJwtTokenService();

        var login = new LoginCommandHandler(context, jwt, _hasher);
        var otherDeviceTokens = await login.Handle(
            new LoginCommand { Email = user.Email, Password = Password },
            CancellationToken.None);

        var changePassword = new ChangePasswordCommandHandler(context, new FakeCurrentUser(user.Id), jwt, _hasher, _time);
        var newTokens = await changePassword.Handle(
            new ChangePasswordCommand { CurrentPassword = Password, NewPassword = "Promijenjena2" },
            CancellationToken.None);
        var tokenVersionAfterChange = user.TokenVersion;

        // Act: an hour later (long after the reuse grace period) the other device tries its old refresh token
        _time.Advance(TimeSpan.FromHours(1));
        var refresh = new RefreshTokenCommandHandler(context, jwt, _time);

        // Assert: the other device gets 401...
        await Assert.ThrowsAsync<MarketUnauthorizedException>(() => refresh.Handle(
            new RefreshTokenCommand { RefreshToken = otherDeviceTokens.RefreshToken },
            CancellationToken.None));

        // ...but it is not treated as a stolen token: the session of this device stays valid
        Assert.Equal(tokenVersionAfterChange, user.TokenVersion);
        var refreshed = await refresh.Handle(new RefreshTokenCommand { RefreshToken = newTokens.RefreshToken }, CancellationToken.None);
        Assert.False(string.IsNullOrEmpty(refreshed.AccessToken));
    }

    private sealed class FakeCurrentUser(int userId) : IAppCurrentUser
    {
        public int? UserId => userId;
        public string? Email => null;
        public bool IsAuthenticated => true;
        public bool IsAdmin => false;
        public bool IsManager => false;
        public bool IsEmployee => false;
    }
}
