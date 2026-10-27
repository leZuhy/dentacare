namespace Market.Application.Common;

public static class DateTimeExtensions
{
    /// <summary>
    /// Returns the value as UTC. All dates in this application are UTC, so a value without time zone
    /// information (<see cref="DateTimeKind.Unspecified"/>, e.g. "2026-10-01" sent without "Z") is treated as UTC,
    /// and a local time is converted to UTC.
    /// </summary>
    public static DateTime AsUtc(this DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
