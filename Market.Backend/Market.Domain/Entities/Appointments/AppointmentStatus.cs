namespace Market.Domain.Entities.Appointments;

/// <summary>
/// Lifecycle status of an appointment.
/// Values are explicit so the numbers stored in the database never change.
/// </summary>
public enum AppointmentStatus
{
    Scheduled = 1,
    Cancelled = 2,
    Completed = 3
}