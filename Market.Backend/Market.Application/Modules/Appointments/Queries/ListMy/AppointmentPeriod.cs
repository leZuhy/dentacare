namespace Market.Application.Modules.Appointments.Queries.ListMy;

/// <summary>
/// Which appointments of the patient to show. In the URL as text: ?period=past
/// Upcoming = start is in the future; Past = start is now or earlier.
/// </summary>
public enum AppointmentPeriod
{
    Upcoming = 0,
    Past = 1,
    All = 2
}