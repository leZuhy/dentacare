namespace Market.Application.Modules.Catalog.ProductCategories.Commands.Create;

public class CreateProductCategoryCommand : IRequest<int>
{
    public required string Name { get; set; }

    /// <summary>
    /// New categories are enabled unless the client sends <c>false</c>.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}
