using Market.Application.Modules.Catalog.Promotions.Commands.Create;
using Market.Application.Modules.Catalog.Promotions.Commands.Delete;
using Market.Application.Modules.Catalog.Promotions.Commands.Update;
using Market.Application.Modules.Catalog.Promotions.Queries.GetById;
using Market.Application.Modules.Catalog.Promotions.Queries.List;
using Market.Shared.Constants;

namespace Market.API.Controllers;

/// <summary>
/// Administration of home page promotions. Visitors get active promotions through GET /Catalog/home.
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize(Policy = PolicyNames.Admin)]
public class PromotionsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<int>> Create(CreatePromotionCommand command, CancellationToken ct)
    {
        int id = await sender.Send(command, ct);

        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:int}")]
    public async Task Update(int id, UpdatePromotionCommand command, CancellationToken ct)
    {
        // ID from the route takes precedence
        command.Id = id;
        await sender.Send(command, ct);
        // no return -> 204 No Content
    }

    [HttpDelete("{id:int}")]
    public async Task Delete(int id, CancellationToken ct)
    {
        await sender.Send(new DeletePromotionCommand { Id = id }, ct);
        // no return -> 204 No Content
    }

    [HttpGet("{id:int}")]
    public async Task<GetPromotionByIdQueryDto> GetById(int id, CancellationToken ct)
    {
        var promotion = await sender.Send(new GetPromotionByIdQuery { Id = id }, ct);
        return promotion; // if NotFoundException -> 404 via middleware
    }

    [HttpGet]
    public async Task<PageResult<ListPromotionsQueryDto>> List([FromQuery] ListPromotionsQuery query, CancellationToken ct)
    {
        var result = await sender.Send(query, ct);
        return result;
    }
}
