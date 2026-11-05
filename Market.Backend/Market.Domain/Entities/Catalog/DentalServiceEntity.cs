using Market.Domain.Common;

namespace Market.Domain.Entities.Catalog;

public sealed class DentalServiceEntity : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal PriceBam { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsActive { get; set; } = true;

    public int CategoryId { get; set; }
    public ServiceCategoryEntity Category { get; set; } = null!;


    public static class Constraints
    {
        public const int NameMaxLength = 100;
        public const int DescriptionMaxLength = 500;
        public const decimal PriceMax = 10000m;
        public const int DurationMin = 15;
        public const int DurationMax = 480;
        public const int DurationStep = 15;
    }
}
