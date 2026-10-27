namespace Market.Application.Modules.Catalog.CatalogHome.Queries.GetCatalogHome;

public sealed class GetCatalogHomeQueryDto
{
    public required List<GetCatalogHomeQueryDtoCategory> Categories { get; init; }
    public required List<GetCatalogHomeQueryDtoProduct> NewestProducts { get; init; }
    public required List<GetCatalogHomeQueryDtoPromotion> Promotions { get; init; }
    public required GetCatalogHomeQueryDtoStats Stats { get; init; }
}

public sealed class GetCatalogHomeQueryDtoCategory
{
    public required int Id { get; init; }
    public required string Name { get; init; }
}

public sealed class GetCatalogHomeQueryDtoProduct
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string? Description { get; init; }
    public required decimal Price { get; init; }
    public required int CategoryId { get; init; }
    public required string CategoryName { get; init; }

    /// <summary>Relative image URL (e.g. <c>/uploads/products/{guid}.webp</c>) or null.</summary>
    public required string? ImageUrl { get; init; }
}

public sealed class GetCatalogHomeQueryDtoPromotion
{
    public required int Id { get; init; }
    public required string Title { get; init; }
    public required string ImageUrl { get; init; }
    public required string? TargetUrl { get; init; }
    public required int SortOrder { get; init; }
}

public sealed class GetCatalogHomeQueryDtoStats
{
    /// <summary>Number of enabled categories.</summary>
    public required int TotalCategories { get; init; }

    /// <summary>Number of enabled products in enabled categories.</summary>
    public required int TotalProducts { get; init; }
}
