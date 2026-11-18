namespace Market.Application.Modules.Appointments.Queries.ListRescheduleFreeSlots;

/// <summary>One free slot in UTC. The frontend shows it in local time.</summary>
public sealed class ListRescheduleFreeSlotsQueryDto
{
    public required DateTime StartUtc { get; init; }
    public required DateTime EndUtc { get; init; }
}