namespace Market.Application.Modules.Appointments.Commands.Create;

/// <summary>
/// The logged-in patient books one service with one dentist.
/// There is no PatientId: the patient is always taken from the access token (IDOR).
/// Body: { "dentistId": 1, "dentalServiceId": 2, "startUtc": "2026-11-20T08:00:00Z" }
/// </summary>
public sealed class CreateAppointmentCommand : IRequest<CreateAppointmentCommandDto>
{
    public int DentistId { get; init; }

    public int DentalServiceId { get; init; }

    /// <summary>
    /// Start of a free slot, exactly as returned by GET api/public/free-slots (UTC, ends with "Z").
    /// </summary>
    public DateTime StartUtc { get; init; }
}