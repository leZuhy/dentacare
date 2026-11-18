using Market.Domain.Entities.Appointments;

namespace Market.Application.Modules.Appointments.Queries.GetMyById;

/// <summary>
/// Details of one appointment for the patient. Times are UTC (end with "Z").
/// Cancel and reschedule (next issues) will be started from this screen.
/// </summary>
public sealed class GetMyAppointmentByIdQueryDto
{
    public required int Id { get; init; }
    public required DateTime StartUtc { get; init; }
    public required DateTime EndUtc { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required AppointmentStatus Status { get; init; }

    /// <summary>Filled only for a cancelled appointment.</summary>
    public required DateTime? CancelledAtUtc { get; init; }

    /// <summary>Role of the user who cancelled (null when not cancelled).</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required Role? CancelledByRole { get; init; }

    public required string DentistFullName { get; init; }
    public required string DentistSpecializationName { get; init; }

    /// <summary>One row per procedure, with the price copied at booking.</summary>
    public required IReadOnlyList<GetMyAppointmentByIdQueryServiceDto> Services { get; init; }

    /// <summary>Sum of PriceAtTimeBam (full price; the loyalty discount is applied on the invoice).</summary>
    public required decimal TotalPriceBam { get; init; }
}

/// <summary>One service (procedure) on the appointment.</summary>
public sealed class GetMyAppointmentByIdQueryServiceDto
{
    public required string Name { get; init; }
    public required decimal PriceAtTimeBam { get; init; }
}