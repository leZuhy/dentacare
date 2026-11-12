using Market.Application.Modules.Appointments.Commands.Create;

namespace Market.Tests.AppointmentTests.UnitTests;

/// <summary>
/// Booking request format: dentist and service ids greater than 0,
/// start required and in UTC (ends with "Z").
/// Business rules (free slot, 60 days, overlap) are tested in the integration tests.
/// </summary>
public sealed class CreateAppointmentValidatorUnitTests
{
    private readonly CreateAppointmentCommandValidator _validator = new();

    private static DateTime Utc(int hour) =>
        new DateTime(2026, 11, 20, hour, 0, 0, DateTimeKind.Utc);

    private static CreateAppointmentCommand ValidCommand() => new()
    {
        DentistId = 1,
        DentalServiceId = 1,
        StartUtc = Utc(8)
    };

    [Fact]
    public void ValidCommand_IsValid()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void DentistIdZero_IsInvalid()
    {
        var command = new CreateAppointmentCommand { DentistId = 0, DentalServiceId = 1, StartUtc = Utc(8) };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAppointmentCommand.DentistId));
    }

    [Fact]
    public void DentalServiceIdZero_IsInvalid()
    {
        var command = new CreateAppointmentCommand { DentistId = 1, DentalServiceId = 0, StartUtc = Utc(8) };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAppointmentCommand.DentalServiceId));
    }

    [Fact]
    public void StartUtcMissing_IsInvalid_WithOneError()
    {
        // default(DateTime) = 0001-01-01 means "startUtc" was not sent
        var command = new CreateAppointmentCommand { DentistId = 1, DentalServiceId = 1 };

        var result = _validator.Validate(command);

        // Cascade.Stop: only "required", not also "must be UTC"
        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(CreateAppointmentCommand.StartUtc), error.PropertyName);
    }

    [Theory]
    [InlineData(DateTimeKind.Unspecified)] // "2026-11-20T08:00:00" (no zone)
    [InlineData(DateTimeKind.Local)]       // "2026-11-20T09:00:00+01:00"
    public void StartUtcNotUtc_IsInvalid(DateTimeKind kind)
    {
        var command = new CreateAppointmentCommand
        {
            DentistId = 1,
            DentalServiceId = 1,
            StartUtc = DateTime.SpecifyKind(new DateTime(2026, 11, 20, 8, 0, 0), kind)
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAppointmentCommand.StartUtc));
    }
}