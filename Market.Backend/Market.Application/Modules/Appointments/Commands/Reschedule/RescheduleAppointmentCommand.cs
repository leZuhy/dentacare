using System.Text.Json.Serialization;

namespace Market.Application.Modules.Appointments.Commands.Reschedule;

/// <summary>
/// The logged-in patient moves one of their own appointments to another free slot.
/// Same dentist, same services, same duration. Only the time changes.
/// Body: { "startUtc": "2026-11-25T09:30:00Z" }
/// </summary>
public sealed class RescheduleAppointmentCommand : IRequest
{
    /// <summary>From the route, not from the body.</summary>
    [JsonIgnore]
    public int Id { get; set; }

    /// <summary>New start, exactly as returned by the free slots (UTC, ends with "Z").</summary>
    public DateTime StartUtc { get; init; }
}