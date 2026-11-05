using Market.Application.Modules.Catalog.DentalServices.Commands.Create;
using Market.Application.Modules.Catalog.DentalServices.Commands.Update;
using Market.Application.Modules.Catalog.DentalServices.Queries.GetById;
using Market.Application.Modules.Catalog.DentalServices.Queries.List;
using Market.Shared.Constants;

namespace Market.API.Controllers;

[Route("api/dental-services")]
[ApiController]
[Authorize(Policy = PolicyNames.Admin)]
public sealed class DentalServicesController(ISender sender) : ControllerBase
{

    [HttpGet]
    public async Task<ActionResult<PageResult<ListDentalServiceQueryDto>>> List([FromQuery] ListDentalServiceQuery query, CancellationToken ct)
    {
        return await sender.Send(query, ct);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GetDentalServiceByIdQueryDto>> GetById(int id, CancellationToken ct)
    {
        return await sender.Send(new GetDentalServiceByIdQuery { Id = id }, ct);
    }

    [HttpPost]
    public async Task<ActionResult> Create(CreateDentalServiceCommand command, CancellationToken ct)
    {
        var id = await sender.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:int}")]
    public async Task Update(int id, UpdateDentalServiceCommand command, CancellationToken ct)
    {
        command.Id = id;
        await sender.Send(command, ct);
    }
}
