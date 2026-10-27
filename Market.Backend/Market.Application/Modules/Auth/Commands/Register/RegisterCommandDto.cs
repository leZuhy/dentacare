namespace Market.Application.Modules.Auth.Commands.Register;

/// <summary>
/// Tokens of the newly registered user. Same shape as <see cref="Login.LoginCommandDto"/>,
/// so the client handles register and login responses the same way.
/// </summary>
public sealed class RegisterCommandDto
{
    public required string AccessToken { get; init; }

    public required string RefreshToken { get; init; }

    public required DateTime AccessTokenExpiresAtUtc { get; init; }

    public required DateTime RefreshTokenExpiresAtUtc { get; init; }
}
