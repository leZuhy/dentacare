namespace Market.Application.Modules.Catalog.CatalogHome.Queries.GetCatalogHome;

/// <summary>
/// Data for the public home page: enabled categories, newest products, active promotions and counts.
/// Out-of-range limits are clamped (like paging), not rejected.
/// </summary>
public sealed class GetCatalogHomeQuery : IRequest<GetCatalogHomeQueryDto>
{
    public const int MaxCategoryLimit = 50;
    public const int MaxProductLimit = 100;

    /// <summary>How many categories to return (1..50, default 8).</summary>
    public int CategoryLimit { get; init; } = 8;

    /// <summary>How many newest products to return (1..100, default 12).</summary>
    public int ProductLimit { get; init; } = 12;
}
