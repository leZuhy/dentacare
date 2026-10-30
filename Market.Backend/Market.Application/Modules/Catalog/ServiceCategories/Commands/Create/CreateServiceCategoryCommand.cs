namespace Market.Application.Modules.Catalog.ServiceCategories.Commands.Create;

public sealed class CreateServiceCategoryCommand : IRequest<int>
{
    public string Name { get; set; } = string.Empty;
}
