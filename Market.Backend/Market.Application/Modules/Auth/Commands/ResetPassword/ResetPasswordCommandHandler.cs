using FluentValidation.Results;
using Market.Application.Modules.Auth.Common;

namespace Market.Application.Modules.Auth.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandler(
    IAppDbContext ctx,
    IPasswordHasher<MarketUserEntity> hasher,
    TimeProvider timeProvider)
    : IRequestHandler<ResetPasswordCommand>
{
    /// <summary>
    /// One message for every rejected token (unknown, expired, already used, other email, disabled account).
    /// </summary>
    public const string InvalidTokenMessage = "Link za promjenu lozinke je nevažeći ili je istekao.";

    public async Task Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        // Required fields and the password rule are checked by ResetPasswordCommandValidator.
        var email = request.Email.Trim().ToLowerInvariant();
        var tokenHash = SecureToken.Hash(request.Token);
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        // TRACKING query: the token and its user are changed below.
        // Cancelled tokens (a newer link was requested) are soft-deleted and skipped by the global query filter.
        var resetToken = await ctx.PasswordResetTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash && x.User.Email.ToLower() == email, ct);

        if (resetToken is null
            || resetToken.UsedAtUtc is not null
            || resetToken.ExpiresAtUtc <= nowUtc
            || !resetToken.User.IsEnabled)
        {
            // Reported as a field error (400, field "token"), the same shape as validator errors.
            // It is checked here and not in the validator, because the handler needs the loaded token and user.
            throw new ValidationException([new ValidationFailure(nameof(ResetPasswordCommand.Token), InvalidTokenMessage)]);
        }

        var user = resetToken.User;

        user.PasswordHash = hasher.HashPassword(user, request.NewPassword);

        // The link works only once
        resetToken.UsedAtUtc = nowUtc;

        // Whoever knew the old password (or stole a token) is logged out everywhere
        await ctx.RevokeAllSessionsAsync(user, nowUtc, ct);

        await ctx.SaveChangesAsync(ct);
    }
}
