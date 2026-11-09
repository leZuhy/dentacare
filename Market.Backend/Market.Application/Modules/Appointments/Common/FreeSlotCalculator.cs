using Market.Domain.Entities.Clinic;

namespace Market.Application.Modules.Appointments.Common;

/// <summary>
/// Calculates free slots of one dentist for one service. Pure logic, no database:
/// everything it needs comes in as parameters, so it can be unit tested.
/// Used for the free slot list (#12) and to check a requested start when booking (#13).
/// </summary>
public static class FreeSlotCalculator
{
    public const int StepMinutes = 15;

    /// <summary>
    /// Max number of days in one request, both dates included (e.g. 1st to 14th).
    /// </summary>
    public const int MaxRangeDays = 14;

    public static List<TimeSlot> Calculate(
        DateOnly fromDate,
        DateOnly toDate,
        IReadOnlyCollection<WorkingHoursEntity> workingHours,
        IReadOnlyCollection<TimeSlot> busy,
        int durationMinutes,
        DateTime nowUtc,
        TimeZoneInfo timeZone)
    {
        var result = new List<TimeSlot>();

        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            var intervals = workingHours
                .Where(x => x.DayOfWeek == date.DayOfWeek)
                .OrderBy(x => x.StartTime);

            foreach (var interval in intervals)
            {
                // Working hours are local clinic time (Kind = Unspecified).
                var localStart = date.ToDateTime(interval.StartTime);
                var localEnd = date.ToDateTime(interval.EndTime);

                for (var local = localStart;
                     local.AddMinutes(durationMinutes) <= localEnd;
                     local = local.AddMinutes(StepMinutes))
                {
                    // e.g. 02:30 on the night clocks go forward doesn't exist.
                    if (timeZone.IsInvalidTime(local))
                        continue;

                    var startUtc = TimeZoneInfo.ConvertTimeToUtc(local, timeZone);
                    var endUtc = startUtc.AddMinutes(durationMinutes);

                    if (startUtc <= nowUtc)
                        continue;

                    // Same overlap rule as working hours: touching is allowed.
                    var isBusy = busy.Any(b => b.StartUtc < endUtc && startUtc < b.EndUtc);
                    if (isBusy)
                        continue;

                    result.Add(new TimeSlot(startUtc, endUtc));
                }
            }
        }

        return result;
    }
}