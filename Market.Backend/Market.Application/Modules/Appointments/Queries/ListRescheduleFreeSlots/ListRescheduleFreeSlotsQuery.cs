namespace Market.Application.Modules.Appointments.Queries.ListRescheduleFreeSlots;

/// <summary>
/// Free slots where the logged-in patient can move one of their own appointments.
/// Dentist and duration come from the appointment. The appointment doesn't block itself.
/// Not paged: a calculation over a limited range (max 14 days), same as the public free slots.
/// </summary>
public sealed class ListRescheduleFreeSlotsQuery : IRequest<List<ListRescheduleFreeSlotsQueryDto>>
{
    /// <summary>Appointment id (from the route).</summary>
    public int Id { get; init; }

    /// <summary>First day (local clinic date).</summary>
    public DateOnly FromDate { get; init; }

    /// <summary>Last day, included (null = same day as FromDate).</summary>
    public DateOnly? ToDate { get; init; }
}