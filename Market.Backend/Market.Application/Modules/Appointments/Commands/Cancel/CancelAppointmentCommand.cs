namespace Market.Application.Modules.Appointments.Commands.Cancel;

/// <summary>
/// The logged-in patient or dentist cancels one of their own appointments.
/// Who is cancelling is always taken from the access token (IDOR).
/// </summary>
public sealed class CancelAppointmentCommand : IRequest<Unit>
{
    public required int Id { get; init; }
}