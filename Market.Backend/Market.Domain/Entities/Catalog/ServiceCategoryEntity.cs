using Market.Domain.Common;

namespace Market.Domain.Entities.Catalog;

/// <summary>
/// Category of dental services (e.g. prevention, prosthetics). One category has many dental services.
/// </summary>
public class ServiceCategoryEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public static class Constraints
    {
        public const int NameMaxLength = 100;
    }
}
