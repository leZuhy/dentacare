namespace Market.Application.Modules.Catalog.CatalogHome.Queries.GetCatalogHome;

/// <summary>
/// Reads the home page data directly from the database on every request.
/// To cache it (e.g. in Redis), see the example in <see cref="Abstractions.Caching.ICacheService"/>.
/// </summary>
public sealed class GetCatalogHomeQueryHandler(IAppDbContext ctx, TimeProvider timeProvider)
    : IRequestHandler<GetCatalogHomeQuery, GetCatalogHomeQueryDto>
{
    public async Task<GetCatalogHomeQueryDto> Handle(GetCatalogHomeQuery request, CancellationToken ct)
    {
        var categoryLimit = Math.Clamp(request.CategoryLimit, 1, GetCatalogHomeQuery.MaxCategoryLimit);
        var productLimit = Math.Clamp(request.ProductLimit, 1, GetCatalogHomeQuery.MaxProductLimit);
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var enabledCategories = ctx.ProductCategories
            .AsNoTracking()
            .Where(c => c.IsEnabled);

        // Only products that can be ordered: enabled products in enabled categories.
        var visibleProducts = ctx.Products
            .AsNoTracking()
            .Where(p => p.IsEnabled && p.Category!.IsEnabled);

        var categories = await enabledCategories
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Take(categoryLimit)
            .Select(c => new GetCatalogHomeQueryDtoCategory
            {
                Id = c.Id,
                Name = c.Name
            })
            .ToListAsync(ct);

        var newestProducts = await visibleProducts
            .OrderByDescending(p => p.CreatedAtUtc)
            .ThenByDescending(p => p.Id)
            .Take(productLimit)
            .Select(p => new GetCatalogHomeQueryDtoProduct
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                CategoryId = p.CategoryId,
                CategoryName = p.Category!.Name,
                ImageUrl = p.ImageUrl
            })
            .ToListAsync(ct);

        // Active promotions inside their date window (null = no limit on that side).
        var promotions = await ctx.Promotions
            .AsNoTracking()
            .Where(p => p.IsActive
                && (p.StartsAtUtc == null || p.StartsAtUtc <= nowUtc)
                && (p.EndsAtUtc == null || p.EndsAtUtc > nowUtc))
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Id)
            .Select(p => new GetCatalogHomeQueryDtoPromotion
            {
                Id = p.Id,
                Title = p.Title,
                ImageUrl = p.ImageUrl,
                TargetUrl = p.TargetUrl,
                SortOrder = p.SortOrder
            })
            .ToListAsync(ct);

        return new GetCatalogHomeQueryDto
        {
            Categories = categories,
            NewestProducts = newestProducts,
            Promotions = promotions,
            Stats = new GetCatalogHomeQueryDtoStats
            {
                TotalCategories = await enabledCategories.CountAsync(ct),
                TotalProducts = await visibleProducts.CountAsync(ct)
            }
        };
    }
}
