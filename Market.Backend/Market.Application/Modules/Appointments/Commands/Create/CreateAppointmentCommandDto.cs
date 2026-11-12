namespace Market.Application.Modules.Appointments.Commands.Create;

/// <summary>
/// The booked appointment, so the frontend can show a confirmation without another request.
/// </summary>
public sealed class CreateAppointmentCommandDto
{
    public required int Id { get; init; }
    public required DateTime StartUtc { get; init; }
    public required DateTime EndUtc { get; init; }
    public required string DentistFullName { get; init; }
    public required string DentalServiceName { get; init; }
    public required decimal PriceBam { get; init; }
}