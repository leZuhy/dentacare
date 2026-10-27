namespace Market.Application.Modules.Auth.Commands.Login;

/// <summary>
/// Represents a pair of tokens (access + refresh) that the client receives upon login.
/// Same shape as <see cref="Refresh.RefreshTokenCommandDto"/>, so the client can handle both responses the same way.
/// </summary>
public sealed class LoginCommandDto
{
    /// <summary>
    /// JWT access token – used for authorized API calls.
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// Refresh token that the client stores locally and uses to obtain a new access token.
    /// </summary>
    public required string RefreshToken { get; init; }

    /// <summary>
    /// Expiration time of the access token in UTC format.
    /// </summary>
    public required DateTime AccessTokenExpiresAtUtc { get; init; }

    /// <summary>
    /// Expiration time of the refresh token in UTC format.
    /// </summary>
    public required DateTime RefreshTokenExpiresAtUtc { get; init; }
}
