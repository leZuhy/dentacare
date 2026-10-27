namespace Market.Application.Modules.Catalog.Promotions.Queries.List;

public sealed class ListPromotionsQuery : BasePagedQuery<ListPromotionsQueryDto>
{
    /// <summary>Search by title (contains, case-insensitive). (optional)</summary>
    public string? Search { get; init; }

    /// <summary>Only active (true) or only inactive (false) promotions. (optional)</summary>
    public bool? IsActive { get; init; }
}
