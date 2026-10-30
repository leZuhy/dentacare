namespace Market.Application.Modules.Catalog.ServiceCategories.Queries.List;

public sealed class ListServiceCategoryQuery : BasePagedQuery<ListServiceCategoryQueryDto>
{
    public string? Search { get; init; }
}
