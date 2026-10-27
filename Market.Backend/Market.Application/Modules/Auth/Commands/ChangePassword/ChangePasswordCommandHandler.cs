using FluentValidation.Results;
using Market.Application.Modules.Auth.Common;

namespace Market.Application.Modules.Auth.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler(
    IAppDbContext ctx,
    IAppCurrentUser currentUser,
    IJwtTokenService jwt,
    IPasswordHasher<MarketUserEntity> hasher,
    TimeProvider timeProvider)
    : IRequestHandler<ChangePasswordCommand, ChangePasswordCommandDto>
{
    public async Task<ChangePasswordCommandDto> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        // Required fields and the password rule are checked by ChangePasswordCommandValidator.
        var userId = currentUser.UserId
            ?? throw new MarketUnauthorizedException("Korisnik nije prijavljen.");

        // TRACKING query: password hash and TokenVersion are changed below.
        var user = await ctx.Users
            .FirstOrDefaultAsync(x => x.Id == userId && x.IsEnabled, ct)
            ?? throw new MarketUnauthorizedException("Korisnik nije prijavljen.");

        var verify = hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (verify == PasswordVerificationResult.Failed)
        {
            // Reported as a field error (400, field "currentPassword"), not 401: the user is logged in,
            // only the value in the form is wrong. Checked here, because the handler needs the loaded user.
            throw new ValidationException(
                [new ValidationFailure(nameof(ChangePasswordCommand.CurrentPassword), "Trenutna lozinka nije ispravna.")]);
        }

        user.PasswordHash = hasher.HashPassword(user, request.NewPassword);

        // Every device is logged out (TokenVersion++), including this one...
        await ctx.RevokeAllSessionsAsync(user, timeProvider.GetUtcNow().UtcDateTime, ct);

        // ...which continues with a new pair. Issued AFTER TokenVersion++, so the new access token has the new "ver".
        var tokens = ctx.StartSession(jwt, user);

        await ctx.SaveChangesAsync(ct);

        return new ChangePasswordCommandDto
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshTokenRaw,
            AccessTokenExpiresAtUtc = tokens.AccessTokenExpiresAtUtc,
            RefreshTokenExpiresAtUtc = tokens.RefreshTokenExpiresAtUtc
        };
    }
}
