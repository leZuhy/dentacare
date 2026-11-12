using Market.Application.Modules.Appointments.Commands.Create;
using Market.Shared.Constants;
using Market.Shared.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Market.API.Controllers;

[Route("api/appointments")]
[ApiController]
public sealed class AppointmentsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// The logged-in patient books one service with one dentist, at the start of a free slot.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = PolicyNames.Patient)]
    [ProducesResponseType(typeof(CreateAppointmentCommandDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateAppointmentCommandDto>> Create([FromBody] CreateAppointmentCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
