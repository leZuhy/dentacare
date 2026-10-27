using Market.Application.Modules.Auth.Commands.ChangePassword;
using Market.Application.Modules.Auth.Commands.ForgotPassword;
using Market.Application.Modules.Auth.Commands.Login;
using Market.Application.Modules.Auth.Commands.Logout;
using Market.Application.Modules.Auth.Commands.Refresh;
using Market.Application.Modules.Auth.Commands.Register;
using Market.Application.Modules.Auth.Commands.ResetPassword;
using Market.Application.Modules.Auth.Commands.UpdateMe;
using Market.Application.Modules.Auth.Queries.GetMe;
using Market.Shared.Dtos;

namespace Market.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Login with email and password. Wrong email, wrong password and a disabled account
    /// all return the same 401 response.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginCommandDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginCommandDto>> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        return Ok(await mediator.Send(command, ct));
    }

    /// <summary>
    /// Exchanges a refresh token for a new access/refresh pair (the old refresh token is revoked).
    /// Any invalid, expired, revoked or reused token returns 401.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RefreshTokenCommandDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RefreshTokenCommandDto>> Refresh([FromBody] RefreshTokenCommand command, CancellationToken ct)
    {
        return Ok(await mediator.Send(command, ct));
    }

    /// <summary>
    /// Revokes the refresh token (idempotent).
    /// </summary>
    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task Logout([FromBody] LogoutCommand command, CancellationToken ct)
    {
        await mediator.Send(command, ct);
    }

    /// <summary>
    /// Registers a new customer account and logs it in (201 + tokens, same shape as login).
    /// An email that is already registered returns 400 with a field error on "email".
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterCommandDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegisterCommandDto>> Register([FromBody] RegisterCommand command, CancellationToken ct)
    {
        var tokens = await mediator.Send(command, ct);

        // 201 Created; the Location header points to the profile of the new user (GET api/auth/me)
        return CreatedAtAction(nameof(GetMe), tokens);
    }

    /// <summary>
    /// Sends a link for setting a new password, if an enabled account with this email exists.
    /// Always 204, so the response does not reveal which emails are registered.
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    public async Task ForgotPassword([FromBody] ForgotPasswordCommand command, CancellationToken ct)
    {
        await mediator.Send(command, ct);
    }

    /// <summary>
    /// Sets a new password with the token from the email and logs the user out on every device.
    /// An invalid, expired or already used token returns 400 with a field error on "token".
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    public async Task ResetPassword([FromBody] ResetPasswordCommand command, CancellationToken ct)
    {
        await mediator.Send(command, ct);
    }

    /// <summary>
    /// Profile of the logged-in user.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(GetMeQueryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<GetMeQueryDto> GetMe(CancellationToken ct)
    {
        return await mediator.Send(new GetMeQuery(), ct);
    }

    /// <summary>
    /// Changes first name, last name, address and city of the logged-in user (not the email).
    /// </summary>
    [HttpPut("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task UpdateMe([FromBody] UpdateMeCommand command, CancellationToken ct)
    {
        await mediator.Send(command, ct);
    }

    /// <summary>
    /// Changes the password of the logged-in user. Other devices are logged out;
    /// this client continues with the new tokens from the response.
    /// A wrong current password returns 400 with a field error on "currentPassword".
    /// </summary>
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ChangePasswordCommandDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ChangePasswordCommandDto>> ChangePassword([FromBody] ChangePasswordCommand command, CancellationToken ct)
    {
        return Ok(await mediator.Send(command, ct));
    }
}
