namespace Market.Application.Modules.Public.Queries.ListServices;

/// <summary>
/// Public list of ACTIVE dental services with search, filters and sorting.
/// Query string: ?search=plomba&amp;categoryId=2&amp;maxPriceBam=100&amp;sort=priceAsc&amp;paging.page=1
/// </summary>
public sealed class ListPublicServicesQuery : BasePagedQuery<ListPublicServicesQueryDto>
{
    /// <summary>Part of the service name or description. (optional)</summary>
    public string? Search { get; init; }

    /// <summary>Only services of this category. (optional)</summary>
    public int? CategoryId { get; init; }

    /// <summary>Only services with price up to this amount in KM. (optional)</summary>
    public decimal? MaxPriceBam { get; init; }

    /// <summary>Name (default), PriceAsc or PriceDesc.</summary>
    public PublicServiceSort Sort { get; init; } = PublicServiceSort.Name;
}