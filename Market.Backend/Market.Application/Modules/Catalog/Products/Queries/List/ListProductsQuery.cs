namespace Market.Application.Modules.Catalog.Products.Queries.List;

public sealed class ListProductsQuery : BasePagedQuery<ListProductsQueryDto>
{
    /// <summary>Search by product name (contains, case-insensitive). (optional)</summary>
    public string? Search { get; init; }

    /// <summary>Only products of this category. (optional)</summary>
    public int? CategoryId { get; init; }
}
