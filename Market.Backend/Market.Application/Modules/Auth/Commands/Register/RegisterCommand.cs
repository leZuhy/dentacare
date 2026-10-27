namespace Market.Application.Modules.Auth.Commands.Register;

/// <summary>
/// Registration of a new customer account. The new user is logged in immediately (the response contains tokens).
/// </summary>
public sealed class RegisterCommand : IRequest<RegisterCommandDto>
{
    public string Firstname { get; init; } = string.Empty;

    public string Lastname { get; init; } = string.Empty;

    /// <summary>
    /// Login email; must not belong to another account.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// At least 8 characters with at least one letter and one digit (see <see cref="PasswordRules"/>).
    /// </summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Street and house number. (optional)
    /// </summary>
    public string? Address { get; init; }

    /// <summary>
    /// City. (optional)
    /// </summary>
    public string? City { get; init; }
}
