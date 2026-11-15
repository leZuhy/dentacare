namespace Market.Application.Modules.Appointments.Queries.GetMyById;

/// <summary>
/// One appointment of the logged-in patient. Somebody else's appointment returns 404 (IDOR),
/// so it is not revealed that the appointment exists.
/// </summary>
public sealed class GetMyAppointmentByIdQuery : IRequest<GetMyAppointmentByIdQueryDto>
{
    public int Id { get; init; }
}