namespace Market.Application.Modules.Public.Queries.ListServiceCategories;

public sealed class ListPublicServiceCategoriesQueryDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
}