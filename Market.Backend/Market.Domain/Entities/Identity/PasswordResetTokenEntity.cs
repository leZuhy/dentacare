using Market.Domain.Common;

namespace Market.Domain.Entities.Identity;

/// <summary>
/// One-time token from the "forgot password" email. Only the SHA-256 hash is stored, like for refresh tokens,
/// so a copy of the database is not enough to change anybody's password.
/// </summary>
public sealed class PasswordResetTokenEntity : BaseEntity
{
    public int UserId { get; set; }

    public MarketUserEntity User { get; set; } = default!;

    /// <summary>
    /// SHA-256 hash of the token from the link (base64url), never the token itself.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// When the token was used to set a new password; a used token cannot be used again. (optional)
    /// </summary>
    public DateTime? UsedAtUtc { get; set; }

    /// <summary>
    /// Single source of truth for technical/business constraints.
    /// Used in validators and EF configuration.
    /// </summary>
    public static class Constraints
    {
        /// <summary>
        /// SHA-256 as base64url has 43 characters.
        /// </summary>
        public const int TokenHashMaxLength = 64;
    }
}
