namespace Market.Application.Modules.Auth.Commands.ResetPassword;

/// <summary>
/// Sets a new password with the token from the "forgot password" email.
/// All sessions of the user are closed (every device has to log in again).
/// </summary>
public sealed class ResetPasswordCommand : IRequest
{
    /// <summary>
    /// Email from the link.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Token from the link.
    /// </summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// Same rule as at registration (see <see cref="PasswordRules"/>).
    /// </summary>
    public string NewPassword { get; init; } = string.Empty;
}
