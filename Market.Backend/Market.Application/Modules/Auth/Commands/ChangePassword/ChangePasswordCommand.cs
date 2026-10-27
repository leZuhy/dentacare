namespace Market.Application.Modules.Auth.Commands.ChangePassword;

/// <summary>
/// Password change of the logged-in user. Other devices are logged out;
/// this client continues with the new tokens from the response.
/// </summary>
public sealed class ChangePasswordCommand : IRequest<ChangePasswordCommandDto>
{
    public string CurrentPassword { get; init; } = string.Empty;

    /// <summary>
    /// Same rule as at registration (see <see cref="PasswordRules"/>).
    /// </summary>
    public string NewPassword { get; init; } = string.Empty;
}
