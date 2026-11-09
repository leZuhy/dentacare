using Market.Application.Modules.Appointments.Common;
using Market.Domain.Entities.Clinic;

namespace Market.Tests.AppointmentTests.UnitTests;

/// <summary>
/// Free slots: 15-minute steps from the start of each working interval, the whole service must fit,
/// no overlap with busy appointments (touching is allowed), no slots in the past,
/// local clinic time converted to UTC (winter UTC+1, summer UTC+2).
/// </summary>
public sealed class FreeSlotCalculatorUnitTests
{
    private static readonly TimeZoneInfo Sarajevo = TimeZoneInfo.FindSystemTimeZoneById("Europe/Sarajevo");

    private static readonly DateOnly WinterMonday = new(2027, 1, 18);    // UTC+1
    private static readonly DateOnly WinterWednesday = new(2027, 1, 20); // UTC+1
    private static readonly DateOnly SummerMonday = new(2027, 7, 19);    // UTC+2

    // "Now" before all test dates, so no slot is in the past (except in the test about the past).
    private static readonly DateTime LongAgoUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static WorkingHoursEntity Hours(DayOfWeek day, int startHour, int endHour) => new()
    {
        DayOfWeek = day,
        StartTime = new TimeOnly(startHour, 0),
        EndTime = new TimeOnly(endHour, 0)
    };

    private static DateTime Utc(DateOnly date, int hour, int minute = 0) =>
        date.ToDateTime(new TimeOnly(hour, minute), DateTimeKind.Utc);

    private static List<TimeSlot> Calculate(
        DateOnly from,
        DateOnly to,
        WorkingHoursEntity[] hours,
        int durationMinutes,
        TimeSlot[]? busy = null,
        DateTime? nowUtc = null) =>
        FreeSlotCalculator.Calculate(from, to, hours, busy ?? [], durationMinutes, nowUtc ?? LongAgoUtc, Sarajevo);

    [Fact]
    public void Calculate_OneInterval_ReturnsSlotsEvery15Minutes()
    {
        // 08:00–10:00 local = 07:00–09:00 UTC in winter
        var slots = Calculate(WinterMonday, WinterMonday, [Hours(DayOfWeek.Monday, 8, 10)], 30);

        Assert.Equal(7, slots.Count); // 08:00, 08:15, ... 09:30
        Assert.Equal(new TimeSlot(Utc(WinterMonday, 7), Utc(WinterMonday, 7, 30)), slots[0]);
        Assert.Equal(new TimeSlot(Utc(WinterMonday, 8, 30), Utc(WinterMonday, 9)), slots[^1]);
    }

    [Fact]
    public void Calculate_ServiceLongerThanInterval_ReturnsEmpty()
    {
        var slots = Calculate(WinterMonday, WinterMonday, [Hours(DayOfWeek.Monday, 8, 9)], 90);

        Assert.Empty(slots);
    }

    [Fact]
    public void Calculate_BusyAppointment_RemovesOnlyOverlappingSlots()
    {
        // busy 09:00–09:30 local = 08:00–08:30 UTC
        var busy = new[] { new TimeSlot(Utc(WinterMonday, 8), Utc(WinterMonday, 8, 30)) };

        var slots = Calculate(WinterMonday, WinterMonday, [Hours(DayOfWeek.Monday, 8, 10)], 30, busy);

        // 08:45, 09:00 and 09:15 local overlap; 08:30–09:00 and 09:30–10:00 only touch
        Assert.Equal(4, slots.Count);
        Assert.Contains(new TimeSlot(Utc(WinterMonday, 7, 30), Utc(WinterMonday, 8)), slots);
        Assert.Contains(new TimeSlot(Utc(WinterMonday, 8, 30), Utc(WinterMonday, 9)), slots);
        Assert.DoesNotContain(slots, s => s.StartUtc == Utc(WinterMonday, 7, 45));
    }

    [Fact]
    public void Calculate_SlotsInThePast_AreSkipped()
    {
        // now = 08:20 local = 07:20 UTC
        var now = Utc(WinterMonday, 7, 20);

        var slots = Calculate(WinterMonday, WinterMonday, [Hours(DayOfWeek.Monday, 8, 10)], 30, nowUtc: now);

        Assert.Equal(5, slots.Count);
        Assert.Equal(Utc(WinterMonday, 7, 30), slots[0].StartUtc); // 08:30 local
    }

    [Fact]
    public void Calculate_ConvertsLocalTimeToUtc_WinterAndSummer()
    {
        var winter = Calculate(WinterMonday, WinterMonday, [Hours(DayOfWeek.Monday, 8, 9)], 60);
        var summer = Calculate(SummerMonday, SummerMonday, [Hours(DayOfWeek.Monday, 8, 9)], 60);

        Assert.Equal(Utc(WinterMonday, 7), Assert.Single(winter).StartUtc); // UTC+1
        Assert.Equal(Utc(SummerMonday, 6), Assert.Single(summer).StartUtc); // UTC+2
    }

    [Fact]
    public void Calculate_TwoIntervals_NoSlotDuringBreak()
    {
        WorkingHoursEntity[] hours = [Hours(DayOfWeek.Monday, 8, 10), Hours(DayOfWeek.Monday, 11, 12)];

        var slots = Calculate(WinterMonday, WinterMonday, hours, 60);

        Assert.Equal(6, slots.Count); // 08:00–09:00 (5) + 11:00 (1)
        // nothing may start between 10:00 and 11:00 local = 09:00–10:00 UTC
        Assert.DoesNotContain(slots, s => s.StartUtc >= Utc(WinterMonday, 9) && s.StartUtc < Utc(WinterMonday, 10));
    }

    [Fact]
    public void Calculate_DayWithoutWorkingHours_ReturnsEmpty()
    {
        var tuesday = WinterMonday.AddDays(1);

        var slots = Calculate(tuesday, tuesday, [Hours(DayOfWeek.Monday, 8, 10)], 30);

        Assert.Empty(slots);
    }

    [Fact]
    public void Calculate_DateRange_IncludesEveryDayInRange()
    {
        WorkingHoursEntity[] hours = [Hours(DayOfWeek.Monday, 8, 9), Hours(DayOfWeek.Wednesday, 8, 9)];

        var slots = Calculate(WinterMonday, WinterWednesday, hours, 60);

        Assert.Equal(
            [new TimeSlot(Utc(WinterMonday, 7), Utc(WinterMonday, 8)),
             new TimeSlot(Utc(WinterWednesday, 7), Utc(WinterWednesday, 8))],
            slots);
    }
}