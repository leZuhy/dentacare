namespace Market.Application.Modules.Auth.Commands.ForgotPassword;

/// <summary>
/// "Forgot password": sends a one-time link for setting a new password to the email of the account.
/// The response is always the same (204), so the API does not reveal which emails are registered.
/// </summary>
public sealed class ForgotPasswordCommand : IRequest
{
    public string Email { get; init; } = string.Empty;
}
