using Market.Application.Modules.Appointments.Commands.Complete;
using Market.Domain.Entities.Appointments;

namespace Market.Tests.AppointmentTests.UnitTests;

/// <summary>
/// Completing a visit, request format: findings required (max 2000), therapy optional (max 2000),
/// list of extra services not null, at most 20 items, every id greater than 0.
/// Business rules (own appointment, status, start, active services) are in the integration tests.
/// </summary>
public sealed class CompleteAppointmentValidatorUnitTests
{
    private readonly CompleteAppointmentCommandValidator _validator = new();

    private static CompleteAppointmentCommand Command(
        string findings = "Karijes na zubu 36.",
        string? therapy = null,
        List<int>? serviceIds = null) => new()
    {
        Id = 1,
        Findings = findings,
        Therapy = therapy,
        AdditionalDentalServiceIds = serviceIds ?? new List<int>()
    };

    [Fact]
    public void ValidCommand_WithoutExtraServices_IsValid()
    {
        var result = _validator.Validate(Command());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void SameServiceTwice_IsValid()
    {
        // Two fillings = two procedures of the same service
        var result = _validator.Validate(Command(therapy: "Plomba", serviceIds: new List<int> { 3, 3 }));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FindingsEmptyOrSpaces_IsInvalid(string findings)
    {
        var result = _validator.Validate(Command(findings: findings));

        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(CompleteAppointmentCommand.Findings), error.PropertyName);
    }

    [Fact]
    public void FindingsAtMaxLength_IsValid_OneMore_IsInvalid()
    {
        var max = MedicalRecordEntity.Constraints.FindingsMaxLength;

        Assert.True(_validator.Validate(Command(findings: new string('a', max))).IsValid);
        Assert.False(_validator.Validate(Command(findings: new string('a', max + 1))).IsValid);
    }

    [Fact]
    public void TherapyTooLong_IsInvalid()
    {
        var tooLong = new string('a', MedicalRecordEntity.Constraints.TherapyMaxLength + 1);

        var result = _validator.Validate(Command(therapy: tooLong));

        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(CompleteAppointmentCommand.Therapy), error.PropertyName);
    }

    [Fact]
    public void ServiceIdsNull_IsInvalid()
    {
        var command = new CompleteAppointmentCommand { Findings = "Nalaz", AdditionalDentalServiceIds = null! };

        var result = _validator.Validate(command);

        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(CompleteAppointmentCommand.AdditionalDentalServiceIds), error.PropertyName);
    }

    [Fact]
    public void TooManyServices_IsInvalid()
    {
        var ids = Enumerable.Repeat(1, MedicalRecordEntity.Constraints.AdditionalServicesMax + 1).ToList();

        var result = _validator.Validate(Command(serviceIds: ids));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ServiceIdZero_IsInvalid()
    {
        var result = _validator.Validate(Command(serviceIds: new List<int> { 2, 0 }));

        Assert.False(result.IsValid);
        // RuleForEach names the item with its index, e.g. "AdditionalDentalServiceIds[1]"
        Assert.Contains(result.Errors, e => e.PropertyName.StartsWith(nameof(CompleteAppointmentCommand.AdditionalDentalServiceIds)));
    }
}
