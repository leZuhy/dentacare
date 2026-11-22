using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Appointments.Common;

/// <summary>
/// One calendar day of the clinic (local time) as a UTC range.
/// Appointments are stored in UTC, but the dentist thinks in local days.
/// </summary>
public static class ClinicDay
{
    /// <summary>
    /// Start of the day (included) and start of the next day (not included), both in UTC.
    /// Not always 24 h: the day when the clocks change has 23 or 25 hours.
    /// </summary>
    public static (DateTime FromUtc, DateTime ToUtc) ToUtcRange(DateOnly date, TimeZoneInfo timeZone)
    {
        // midnight of this day and of the next day, in local clinic time
        var localStart = date.ToDateTime(TimeOnly.MinValue);
        var localNextStart = date.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var fromUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone);
        var toUtc = TimeZoneInfo.ConvertTimeToUtc(localNextStart, timeZone);

        return (fromUtc, toUtc);
    }
}
