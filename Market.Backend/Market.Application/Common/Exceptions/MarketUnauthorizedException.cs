namespace Market.Application.Common.Exceptions;

/// <summary>
/// The caller is not authenticated or the presented credentials/tokens are not valid
/// (wrong email or password, invalid/expired/revoked refresh token...).
/// Mapped to HTTP 401 with code "auth.unauthorized".
/// </summary>
public sealed class MarketUnauthorizedException : Exception
{
    public MarketUnauthorizedException(string message) : base(message) { }
}
