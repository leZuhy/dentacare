using Market.Application.Modules.Clinic.WorkingHours.Commands.Create;
using Market.Application.Modules.Clinic.WorkingHours.Queries.GetById;
using Market.Application.Modules.Clinic.WorkingHours.Queries.List;
using Market.Application.Modules.Clinic.WorkingHours.Commands.Delete;
using Market.Application.Modules.Clinic.WorkingHours.Commands.Update;
using Market.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Market.API.Controllers;

[Route("api/working-hours")]
[ApiController]
[Authorize(Policy = PolicyNames.Admin)]
public class WorkingHoursController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<PageResult<ListWorkingHoursQueryDto>> List([FromQuery] ListWorkingHoursQuery query, CancellationToken ct)
    { 
        return await sender.Send(query, ct);
    }

    [HttpGet("{id:int}")]
    public async Task<GetWorkingHoursByIdQueryDto> GetById(int id, CancellationToken ct)
    {
        return await sender.Send(new GetWorkingHoursByIdQuery { Id = id }, ct);
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create(CreateWorkingHoursCommand command, CancellationToken ct)
    {
        var id = await sender.Send(command, ct);

        return CreatedAtAction(nameof(GetById), new { id }, new {id});
    }

    [HttpPut("{id:int}")]
    public async Task Update(int id, UpdateWorkingHoursCommand command, CancellationToken ct)
    {
        command.Id = id;
        await sender.Send(command, ct);
    }

    [HttpDelete("{id:int}")]
    public async Task Delete(int id, CancellationToken ct)
    {
        await sender.Send(new DeleteWorkingHoursCommand { Id = id }, ct);
    }
}
