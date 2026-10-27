using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Market.Application.Common;

/// <summary>
/// Random tokens that are sent to the client (refresh token, password reset link) and their hashes.
/// The database stores only <see cref="Hash"/> of a token: whoever reads the database cannot use the tokens.
/// </summary>
public static class SecureToken
{
    /// <summary>
    /// <paramref name="byteCount"/> cryptographically random bytes as base64url
    /// (letters, digits, '-' and '_'; safe in URLs without encoding).
    /// </summary>
    public static string Generate(int byteCount)
    {
        return Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(byteCount));
    }

    /// <summary>
    /// SHA-256 of the token as base64url (43 characters). The same token always gives the same hash,
    /// so a token received from the client is found in the database by its hash.
    /// </summary>
    public static string Hash(string rawToken)
    {
        return Base64Url.EncodeToString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
    }
}
