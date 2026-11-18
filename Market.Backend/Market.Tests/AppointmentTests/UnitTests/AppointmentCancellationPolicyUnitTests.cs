using Market.Application.Modules.Appointments.Common;

public class AppointmentCancellationPolicyUnitTests
{
    private const int MinHours = 24;
    private static readonly DateTime NowUtc = new(2026, 11, 18, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ExactlyMinHoursBefore_CanCancel()
    {
        var startUtc = NowUtc.AddHours(MinHours);

        var result = AppointmentCancellationPolicy.CanCancel(startUtc, NowUtc, MinHours);

        Assert.True(result);
    }

    [Fact]
    public void OneMinuteLessThanMinHours_CannotCancel()
    {
        var startUtc = NowUtc.AddHours(MinHours).AddMinutes(-1);

        var result = AppointmentCancellationPolicy.CanCancel(startUtc, NowUtc, MinHours);

        Assert.False(result);
    }

    [Fact]
    public void PastAppointment_CannotCancel()
    {
        var startUtc = NowUtc.AddHours(-1);

        var result = AppointmentCancellationPolicy.CanCancel(startUtc, NowUtc, MinHours);

        Assert.False(result);
    }

    [Fact]
    public void FarInFuture_CanCancel()
    {
        var startUtc = NowUtc.AddDays(10);

        var result = AppointmentCancellationPolicy.CanCancel(startUtc, NowUtc, MinHours);

        Assert.True(result);
    }
}