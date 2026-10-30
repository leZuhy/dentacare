using System.Text.Json.Serialization;

namespace Market.Application.Modules.Catalog.ServiceCategories.Commands.Update;

public sealed class UpdateServiceCategoryCommand : IRequest<Unit>
{
    // Id comes from the route URL, not from the request body
    [JsonIgnore]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
