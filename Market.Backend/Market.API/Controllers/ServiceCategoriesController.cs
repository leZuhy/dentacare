using Market.Application.Modules.Catalog.ServiceCategories.Commands.Create;
using Market.Application.Modules.Catalog.ServiceCategories.Commands.Delete;
using Market.Application.Modules.Catalog.ServiceCategories.Commands.Update;
using Market.Application.Modules.Catalog.ServiceCategories.Queries.GetById;
using Market.Application.Modules.Catalog.ServiceCategories.Queries.List;
using Market.Shared.Constants;

namespace Market.API.Controllers;

[ApiController]
[Route("api/service-categories")]
[Authorize(Policy = PolicyNames.Admin)]
public sealed class ServiceCategoriesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<PageResult<ListServiceCategoryQueryDto>> List([FromQuery] ListServiceCategoryQuery query, CancellationToken ct)
    {
        var result = await sender.Send(query, ct);
        return result;
    }

    [HttpGet("{id:int}")]
    public async Task<GetServiceCategoryByIdQueryDto> GetById(int id, CancellationToken ct)
    {
        var result = await sender.Send(new GetServiceCategoryByIdQuery { Id = id }, ct);
        return result;
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create(CreateServiceCategoryCommand command, CancellationToken ct)
    {
        int id = await sender.Send(command, ct);

        // 201 + Location header pointing to GET api/service-categories/{id}
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:int}")]
    public async Task Update(int id, UpdateServiceCategoryCommand command, CancellationToken ct)
    {
        // Id comes from the route, not the body, Id in the command is JsonIgnore'd
        command.Id = id;
        await sender.Send(command, ct);
    }

    [HttpDelete("{id:int}")]
    public async Task Delete(int id, CancellationToken ct)
    {
        await sender.Send(new DeleteServiceCategoryCommand { Id = id }, ct);
    }
}
