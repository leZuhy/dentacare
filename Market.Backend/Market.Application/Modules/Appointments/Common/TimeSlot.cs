namespace Market.Application.Modules.Appointments.Common;

/// <summary>
/// A time interval in UTC: a free slot, or a busy (already booked) appointment.
/// </summary>
public sealed record TimeSlot(DateTime StartUtc, DateTime EndUtc);