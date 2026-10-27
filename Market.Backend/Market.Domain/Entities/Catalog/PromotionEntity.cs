using Market.Domain.Common;

namespace Market.Domain.Entities.Catalog;

/// <summary>
/// Represents a promotion banner shown on the public catalog home page.
/// </summary>
public class PromotionEntity : BaseEntity
{
    /// <summary>
    /// Title shown on the banner.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Absolute http(s) URL of the banner image.
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Where the banner leads: an absolute http(s) URL or a frontend route starting with "/". (optional)
    /// </summary>
    public string? TargetUrl { get; set; }

    /// <summary>
    /// Only active promotions are shown on the home page.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// The promotion is shown from this moment (UTC). Null means "no start limit". (optional)
    /// </summary>
    public DateTime? StartsAtUtc { get; set; }

    /// <summary>
    /// The promotion is shown until this moment (UTC). Null means "no end limit". (optional)
    /// </summary>
    public DateTime? EndsAtUtc { get; set; }

    /// <summary>
    /// Display order on the home page (smaller first).
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Single source of truth for technical/business constraints.
    /// Used in validators and EF configuration.
    /// </summary>
    public static class Constraints
    {
        public const int TitleMaxLength = 120;

        public const int ImageUrlMaxLength = 500;

        public const int TargetUrlMaxLength = 500;
    }
}
