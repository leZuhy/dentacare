using Market.Application.Modules.Auth.Common;

namespace Market.Application.Modules.Auth.Commands.Refresh;

/// <summary>
/// Rotates the refresh token: the presented token is revoked and a new access/refresh pair is issued.
/// Every rejection is a <see cref="MarketUnauthorizedException"/> (HTTP 401) with the same message.
/// </summary>
public sealed class RefreshTokenCommandHandler(
    IAppDbContext ctx,
    IJwtTokenService jwt,
    TimeProvider timeProvider)
    : IRequestHandler<RefreshTokenCommand, RefreshTokenCommandDto>
{
    private const string InvalidTokenMessage = "Refresh token je nevažeći ili je istekao.";

    /// <summary>
    /// A token rotated less than this long ago is treated as a race (e.g. two browser tabs refreshing
    /// at the same time with the same token), not as theft: the request gets 401, but other sessions stay alive.
    /// </summary>
    private static readonly TimeSpan ReuseGracePeriod = TimeSpan.FromSeconds(30);

    public async Task<RefreshTokenCommandDto> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        // 1) Hash the received refresh token (only hashes are stored in the database)
        var incomingHash = jwt.HashRefreshToken(request.RefreshToken);

        // 2) Find the token in the database - revoked ones too, so that reuse can be detected.
        //    Deleted ones are not found (sessions closed on purpose, e.g. by a password change - see UserSessionExtensions).
        //    TRACKING query, because we are going to modify it.
        var rt = await ctx.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == incomingHash && !x.IsDeleted, ct);

        if (rt is null)
            throw new MarketUnauthorizedException(InvalidTokenMessage);

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        // 3) Reuse detection: a refresh token can be used only once (rotation).
        //    If an already revoked token comes back, it was probably stolen or copied,
        //    so all sessions of that user are closed:
        //    - every active refresh token of the user is revoked,
        //    - TokenVersion is incremented, so already issued access tokens are rejected too
        //      (the "ver" claim is checked on every request, see Market.API DependencyInjection).
        //    Exception: a token rotated within the last few seconds (ReuseGracePeriod) is a race between
        //    two tabs/requests of the same client, so only this request is rejected.
        if (rt.IsRevoked)
        {
            var rotatedRecently = rt.RevokedAtUtc is not null && nowUtc - rt.RevokedAtUtc.Value < ReuseGracePeriod;
            if (!rotatedRecently)
            {
                await ctx.RevokeAllSessionsAsync(rt.User, nowUtc, ct);
                await ctx.SaveChangesAsync(ct);
            }

            throw new MarketUnauthorizedException(InvalidTokenMessage);
        }

        if (rt.ExpiresAtUtc <= nowUtc)
            throw new MarketUnauthorizedException(InvalidTokenMessage);

        // 4) Fingerprint check: a token bound to a device can only be refreshed from that device.
        if (rt.Fingerprint is not null && rt.Fingerprint != request.Fingerprint)
            throw new MarketUnauthorizedException(InvalidTokenMessage);

        var user = rt.User;
        if (!user.IsEnabled || user.IsDeleted)
            throw new MarketUnauthorizedException(InvalidTokenMessage);

        // 5) Rotation: revoke the old one
        rt.IsRevoked = true;
        rt.RevokedAtUtc = nowUtc;

        // 6) Issue a NEW pair (access + refresh) and 7) save the NEW refresh token (HASH only) in the database
        //    (see UserSessionExtensions.StartSession; the pair contains both the RAW token and its HASH)
        var pair = ctx.StartSession(jwt, user, request.Fingerprint);
        await ctx.SaveChangesAsync(ct);

        // 8) Return the RAW refresh token and access token to the client
        return new RefreshTokenCommandDto
        {
            AccessToken = pair.AccessToken,
            RefreshToken = pair.RefreshTokenRaw,
            AccessTokenExpiresAtUtc = pair.AccessTokenExpiresAtUtc,
            RefreshTokenExpiresAtUtc = pair.RefreshTokenExpiresAtUtc
        };
    }
}
