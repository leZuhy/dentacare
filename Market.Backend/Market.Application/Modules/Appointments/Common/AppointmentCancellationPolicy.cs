namespace Market.Application.Modules.Appointments.Common;

/// <summary>
/// Rule: an appointment can be cancelled at least N hours before it starts.
/// </summary>
public static class AppointmentCancellationPolicy
{
    public static bool CanCancel(DateTime startUtc, DateTime nowUtc, int minHoursBefore)
        => startUtc - nowUtc >= TimeSpan.FromHours(minHoursBefore);
}