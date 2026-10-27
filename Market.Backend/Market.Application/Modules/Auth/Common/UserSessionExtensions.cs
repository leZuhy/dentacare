namespace Market.Application.Modules.Auth.Common;

/// <summary>
/// Session steps shared by the auth handlers. A "session" is one refresh token (one logged-in device/browser).
/// Neither method saves: the handler calls <c>SaveChangesAsync</c> once, together with its other changes.
/// </summary>
public static class UserSessionExtensions
{
    /// <summary>
    /// Issues a new access/refresh token pair for <paramref name="user"/> and adds the refresh token
    /// (its HASH only) to the context. Used by login, refresh, register and change password.
    /// The user must already have an Id (a new user is saved first), because the access token contains it.
    /// </summary>
    public static JwtTokenPair StartSession(
        this IAppDbContext ctx,
        IJwtTokenService jwt,
        MarketUserEntity user,
        string? fingerprint = null)
    {
        var tokens = jwt.IssueTokens(user);

        ctx.RefreshTokens.Add(new RefreshTokenEntity
        {
            TokenHash = tokens.RefreshTokenHash,
            ExpiresAtUtc = tokens.RefreshTokenExpiresAtUtc,
            UserId = user.Id,
            Fingerprint = fingerprint
        });

        return tokens;
    }

    /// <summary>
    /// Logs the user out everywhere: every active refresh token is revoked, and <c>TokenVersion</c> is incremented,
    /// so already issued access tokens are rejected too (the "ver" claim is checked on every request,
    /// see Market.API DependencyInjection). Used by password change/reset and by refresh token reuse detection.
    /// <para>
    /// The tokens are also deleted (soft delete), not only revoked. RefreshTokenCommandHandler treats a REVOKED token
    /// that comes back as stolen and closes all sessions again. After a password change, the user's other devices still
    /// have their old tokens and will try them: they must simply get 401, and must not close the new session
    /// of the device on which the password was changed.
    /// </para>
    /// </summary>
    public static async Task RevokeAllSessionsAsync(
        this IAppDbContext ctx,
        MarketUserEntity user,
        DateTime nowUtc,
        CancellationToken ct)
    {
        var activeTokens = await ctx.RefreshTokens
            .Where(x => x.UserId == user.Id && !x.IsRevoked)
            .ToListAsync(ct);

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
            token.RevokedAtUtc = nowUtc;
        }

        ctx.RefreshTokens.RemoveRange(activeTokens);

        user.TokenVersion++;
    }
}
