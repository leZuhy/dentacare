namespace Market.Application.Modules.Public.Queries.ListServiceCategories;

/// <summary>
/// Categories for the public filter: only those with at least one active service. No paging.
/// </summary>
public sealed class ListPublicServiceCategoriesQuery : IRequest<List<ListPublicServiceCategoriesQueryDto>>
{
}