using Market.Domain.Entities.Appointments;

namespace Market.Application.Modules.Appointments.Queries.ListMy;

/// <summary>
/// One row in "My appointments". Times are UTC (end with "Z"); the frontend shows them in clinic time.
/// </summary>
public sealed class ListMyAppointmentsQueryDto
{
    public required int Id { get; init; }
    public required DateTime StartUtc { get; init; }
    public required DateTime EndUtc { get; init; }

    // as text in JSON ("Scheduled"), not as a number, so the frontend does not depend on the numbers
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required AppointmentStatus Status { get; init; }

    public required string DentistFullName { get; init; }

    /// <summary>Names of the services on this appointment (one row in AppointmentService = one name).</summary>
    public required IReadOnlyList<string> DentalServiceNames { get; init; }

    /// <summary>Sum of PriceAtTimeBam (full price; the loyalty discount is applied on the invoice).</summary>
    public required decimal TotalPriceBam { get; init; }
}