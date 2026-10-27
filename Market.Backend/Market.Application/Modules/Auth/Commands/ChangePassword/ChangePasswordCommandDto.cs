namespace Market.Application.Modules.Auth.Commands.ChangePassword;

/// <summary>
/// New tokens after a password change (the old ones no longer work). Same shape as <see cref="Login.LoginCommandDto"/>.
/// </summary>
public sealed class ChangePasswordCommandDto
{
    public required string AccessToken { get; init; }

    public required string RefreshToken { get; init; }

    public required DateTime AccessTokenExpiresAtUtc { get; init; }

    public required DateTime RefreshTokenExpiresAtUtc { get; init; }
}
