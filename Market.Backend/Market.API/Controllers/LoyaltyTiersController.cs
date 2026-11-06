using Market.Application.Modules.Loyalty.LoyaltyTiers.Commands.Create;
using Market.Application.Modules.Loyalty.LoyaltyTiers.Queries.GetById;
using Market.Application.Modules.Loyalty.LoyaltyTiers.Queries.List;
using Market.Application.Modules.Loyalty.LoyaltyTiers.Commands.Update;
using Market.Application.Modules.Loyalty.LoyaltyTiers.Commands.Delete;
using Market.Shared.Constants;

namespace Market.API.Controllers;

[Route("api/loyalty-tiers")]
[ApiController]
[Authorize(Policy = PolicyNames.Admin)]
public sealed class LoyaltyTiersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PageResult<ListLoyaltyTierQueryDto>>> List([FromQuery] ListLoyaltyTierQuery query, CancellationToken ct)
    {
        return await sender.Send(query, ct);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GetLoyaltyTierByIdQueryDto>> GetById(int id, CancellationToken ct)
    {
        return await sender.Send(new GetLoyaltyTierByIdQuery { Id = id }, ct);
    }

    [HttpPost]
    public async Task<ActionResult> Create(CreateLoyaltyTierCommand command, CancellationToken ct)
    {
        var id = await sender.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:int}")]
    public async Task Update(int id, UpdateLoyaltyTierCommand command, CancellationToken ct)
    {
        command.Id = id;
        await sender.Send(command, ct);
    }

    [HttpDelete("{id:int}")]
    public async Task Delete(int id, CancellationToken ct)
    {
        await sender.Send(new DeleteLoyaltyTierCommand { Id = id }, ct);
    }
}
