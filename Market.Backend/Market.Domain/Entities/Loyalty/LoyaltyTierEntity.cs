using Market.Domain.Common;

namespace Market.Domain.Entities.Loyalty;

/// <summary>
/// Loyalty tier (e.g. Bronze, Silver, Gold). A patient is on the tier once their points reach MinPoints.
/// </summary>
public sealed class LoyaltyTierEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int MinPoints { get; set; }
    public decimal DiscountPercent { get; set; }

    public static class Constraints
    {
        public const int NameMaxLength = 50;
        public const int MinPointsMax = 1_000_000;
        public const decimal DiscountPercentMax = 40m;
        public const int BaseTierMinPoints = 0;
    }
}