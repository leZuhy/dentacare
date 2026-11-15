using Market.Application.Modules.Appointments.Queries.ListMy;
using Market.Domain.Entities.Appointments;

namespace Market.Tests.AppointmentTests.UnitTests;

public sealed class ListMyAppointmentsValidatorUnitTests
{
    private readonly ListMyAppointmentsQueryValidator _validator = new();

    [Fact]
    public void DefaultQuery_IsValid()
    {
        // no period, no status, default paging = what the frontend sends first
        var result = _validator.Validate(new ListMyAppointmentsQuery());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void PeriodAndStatus_AreValid()
    {
        var query = new ListMyAppointmentsQuery
        {
            Period = AppointmentPeriod.All,
            Status = AppointmentStatus.Cancelled
        };

        var result = _validator.Validate(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void UnknownPeriod_IsInvalid()
    {
        // ?period=7 is bound without an error, the validator must catch it
        var query = new ListMyAppointmentsQuery { Period = (AppointmentPeriod)7 };

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ListMyAppointmentsQuery.Period));
    }

    [Fact]
    public void UnknownStatus_IsInvalid()
    {
        var query = new ListMyAppointmentsQuery { Status = (AppointmentStatus)99 };

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ListMyAppointmentsQuery.Status));
    }
}