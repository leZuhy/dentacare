namespace Market.Application.Modules.Catalog.ServiceCategories.Queries.GetById;

public sealed class GetServiceCategoryByIdQuery : IRequest<GetServiceCategoryByIdQueryDto>
{
    public required int Id { get; init; }
}
