namespace Market.Application.Modules.Catalog.ServiceCategories.Queries.GetById;

public sealed class GetServiceCategoryByIdQueryDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
}
