namespace Market.Application.Modules.Catalog.ServiceCategories.Commands.Delete;

public sealed class DeleteServiceCategoryCommand : IRequest<Unit>
{
    public required int Id { get; set; }
}
