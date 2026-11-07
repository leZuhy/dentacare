using Market.Application.Modules.Patients.Queries.GetMyPatientProfile;
using Market.Shared.Constants;
using Market.Application.Modules.Patients.Commands.UpdateMyPatientProfile;
using Market.Shared.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Market.API.Controllers
{
    [Route("api/patients")]
    [ApiController]
    [Authorize(Policy = PolicyNames.Patient)]
    public sealed class PatientsController(ISender sender) : ControllerBase
    {
        /// <summary>
        /// Phone and date of birth of the logged-in patient.
        /// </summary>
        [HttpGet("me")]
        [ProducesResponseType(typeof(GetMyPatientProfileQueryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<GetMyPatientProfileQueryDto> GetMe(CancellationToken ct)
        {
            return await sender.Send(new GetMyPatientProfileQuery(), ct);
        }

        /// <summary>
        /// Changes phone and date of birth of the logged-in patient. Null or empty removes a value.
        /// </summary>
        [HttpPut("me")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task UpdateMe([FromBody] UpdateMyPatientProfileCommand command, CancellationToken ct)
        {
            await sender.Send(command, ct);
        }
    }
}
