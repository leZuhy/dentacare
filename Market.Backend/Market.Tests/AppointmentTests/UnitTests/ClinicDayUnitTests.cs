using Market.Application.Modules.Appointments.Common;

namespace Market.Tests.AppointmentTests.UnitTests;

/// <summary>
/// Local clinic day -> UTC range: winter UTC+1, summer UTC+2,
/// and the two days when the clocks change (23 h and 25 h).
/// </summary>
public sealed class ClinicDayUnitTests
{
    private static readonly TimeZoneInfo Sarajevo = TimeZoneInfo.FindSystemTimeZoneById("Europe/Sarajevo");

    private static DateTime Utc(int year, int month, int day, int hour) =>
        new(year, month, day, hour, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ToUtcRange_WinterDay_StartsAt23UtcDayBefore()
    {
        // 18.01.2027 00:00 local (UTC+1) = 17.01. 23:00 UTC
        var (fromUtc, toUtc) = ClinicDay.ToUtcRange(new DateOnly(2027, 1, 18), Sarajevo);

        Assert.Equal(Utc(2027, 1, 17, 23), fromUtc);
        Assert.Equal(Utc(2027, 1, 18, 23), toUtc);
    }

    [Fact]
    public void ToUtcRange_SummerDay_StartsAt22UtcDayBefore()
    {
        // 19.07.2027 00:00 local (UTC+2) = 18.07. 22:00 UTC
        var (fromUtc, toUtc) = ClinicDay.ToUtcRange(new DateOnly(2027, 7, 19), Sarajevo);

        Assert.Equal(Utc(2027, 7, 18, 22), fromUtc);
        Assert.Equal(Utc(2027, 7, 19, 22), toUtc);
    }

    [Fact]
    public void ToUtcRange_SpringClockChange_DayHas23Hours()
    {
        // 28.03.2027: at 02:00 the clock jumps to 03:00 (UTC+1 -> UTC+2)
        var (fromUtc, toUtc) = ClinicDay.ToUtcRange(new DateOnly(2027, 3, 28), Sarajevo);

        Assert.Equal(Utc(2027, 3, 27, 23), fromUtc);
        Assert.Equal(Utc(2027, 3, 28, 22), toUtc);
        Assert.Equal(23, (toUtc - fromUtc).TotalHours);
    }

    [Fact]
    public void ToUtcRange_AutumnClockChange_DayHas25Hours()
    {
        // 31.10.2027: at 03:00 the clock goes back to 02:00 (UTC+2 -> UTC+1)
        var (fromUtc, toUtc) = ClinicDay.ToUtcRange(new DateOnly(2027, 10, 31), Sarajevo);

        Assert.Equal(Utc(2027, 10, 30, 22), fromUtc);
        Assert.Equal(Utc(2027, 10, 31, 23), toUtc);
        Assert.Equal(25, (toUtc - fromUtc).TotalHours);
    }
}