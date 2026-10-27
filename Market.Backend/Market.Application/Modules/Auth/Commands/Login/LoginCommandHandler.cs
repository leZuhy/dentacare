using Market.Application.Modules.Auth.Common;

namespace Market.Application.Modules.Auth.Commands.Login;

public sealed class LoginCommandHandler(
    IAppDbContext ctx,
    IJwtTokenService jwt,
    IPasswordHasher<MarketUserEntity> hasher)
    : IRequestHandler<LoginCommand, LoginCommandDto>
{
    /// <summary>
    /// One message for every failed login (unknown email, wrong password, disabled account),
    /// so the API does not reveal which emails are registered (user enumeration).
    /// </summary>
    private const string InvalidCredentialsMessage = "Pogrešan email ili lozinka.";

    public async Task<LoginCommandDto> Handle(LoginCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await ctx.Users
            .FirstOrDefaultAsync(x => x.Email.ToLower() == email && x.IsEnabled && !x.IsDeleted, ct)
            ?? throw new MarketUnauthorizedException(InvalidCredentialsMessage);

        var verify = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verify == PasswordVerificationResult.Failed)
            throw new MarketUnauthorizedException(InvalidCredentialsMessage);

        // New access/refresh pair; the refresh token is stored as a hash (see UserSessionExtensions)
        var tokens = ctx.StartSession(jwt, user, request.Fingerprint);

        await ctx.SaveChangesAsync(ct);

        return new LoginCommandDto
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshTokenRaw,
            AccessTokenExpiresAtUtc = tokens.AccessTokenExpiresAtUtc,
            RefreshTokenExpiresAtUtc = tokens.RefreshTokenExpiresAtUtc
        };
    }
}
