using Market.Application.Modules.Clinic.Dentists.Commands.Create;
using Market.Application.Modules.Clinic.Dentists.Commands.Delete;
using Market.Application.Modules.Clinic.Dentists.Commands.Update;
using Market.Application.Modules.Clinic.Dentists.Queries.GetById;
using Market.Application.Modules.Clinic.Dentists.Queries.List;
using Market.Shared.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace Market.API.Controllers
{
    [Route("api/dentists")]
    [ApiController]
    [Authorize(Policy = PolicyNames.Admin)]
    public sealed class DentistsController(ISender sender) : ControllerBase
    {
        [HttpGet]
        public async Task<PageResult<ListDentistsQueryDto>> List([FromQuery] ListDentistsQuery query,
            CancellationToken ct)
        {
            return await sender.Send(query, ct);
        }

        [HttpGet("{id:int}")]
        public async Task<GetDentistByQueryDto> GetById(int id, CancellationToken ct)
        {
            return await sender.Send(new GetDentistByIdQuery { Id = id }, ct);
        }

        [HttpPost]
        public async Task<ActionResult<int>> Create(CreateDentistCommand command, CancellationToken ct)
        {
            var id = await sender.Send(command, ct);

            return CreatedAtAction(nameof(GetById), new { id }, new { id });
        }

        [HttpPut("{id:int}")]
        public async Task Update(int id, UpdateDentistCommand command, CancellationToken ct)
        {
            command.Id = id;
            await sender.Send(command, ct);
        }

        [HttpDelete("{id:int}")]
        public async Task Delete(int id, CancellationToken ct)
        {
            await sender.Send(new DeleteDentistCommand { Id = id }, ct);
        }
    }
}
