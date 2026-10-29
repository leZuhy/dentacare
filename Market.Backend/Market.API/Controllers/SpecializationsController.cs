using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Market.Application.Modules.Catalog.Specializations.Commands.Create;
using Market.Application.Modules.Catalog.Specializations.Commands.Delete;
using Market.Application.Modules.Catalog.Specializations.Commands.Update;
using Market.Application.Modules.Catalog.Specializations.Queries.GetById;
using Market.Application.Modules.Catalog.Specializations.Queries.List;
using Market.Shared.Constants;

namespace Market.API.Controllers
{
    [ApiController]
    [Route("api/specializations")]
    [Authorize(Policy = PolicyNames.Admin)]
    public sealed class SpecializationsController(ISender sender) : ControllerBase
    {
        [HttpGet]
        public async Task<PageResult<ListSpecializationQueryDto>> List([FromQuery] ListSpecializationQuery query, CancellationToken ct)
        {
            var result = await sender.Send(query, ct);
            return result;
        }

        [HttpGet("{id:int}")]
        public async Task<GetSpecializationByIdQueryDto> GetById(int id, CancellationToken ct)
        {
            var result = await sender.Send(new GetSpecializationByIdQuery { Id = id }, ct);
            return result;
        }

        [HttpPost]
        public async Task<ActionResult<int>> Create(CreateSpecializationCommand command, CancellationToken ct)
        {
            int id = await sender.Send(command, ct);

            // 201 + Location header pointing to GET api/specializations/{id}
            return CreatedAtAction(nameof(GetById), new { id }, new { id });
        }

        [HttpPut("{id:int}")]
        public async Task Update(int id, UpdateSpecializationCommand command, CancellationToken ct)
        {
            //Id comes from the route, not the body, Id in the command is JsonIgnore'd
            command.Id = id;
            await sender.Send(command, ct);
        }

        [HttpDelete("{id:int}")]
        public async Task Delete(int id, CancellationToken ct)
        {
            await sender.Send((new DeleteSpecializationCommand { Id = id }), ct);
        }
    }
}
