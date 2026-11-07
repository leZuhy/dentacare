using Market.Application.Modules.Patients.Commands.UpdateMyPatientProfile;

namespace Market.Tests.PatientTests.UnitTests;

/// <summary>
/// Patient profile: phone 8-15 digits with only + ( ) / - and spaces (+ only at the start),
/// date of birth not in the future and not older than 120 years. Both are optional.
/// </summary>
public sealed class PatientProfileValidatorUnitTests
{
    private readonly UpdateMyPatientProfileCommandValidator _validator = new();

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);

    // === phone ===

    [Theory]
    [InlineData("061 123 456")]       // 9 digits
    [InlineData("+387 61 123 456")]   // 11 digits, + at the start
    [InlineData("033/123-456")]
    [InlineData("(033) 123 456")]
    [InlineData("12345678")]          // exactly 8 digits
    [InlineData("123456789012345")]   // exactly 15 digits
    public void Phone_Valid_IsValid(string phone)
    {
        var result = _validator.Validate(new UpdateMyPatientProfileCommand { Phone = phone });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("1234567")]                 // 7 digits
    [InlineData("1234567890123456")]        // 16 digits
    [InlineData("061-abc-456")]             // letters
    [InlineData("061 +123 456")]            // + in the middle
    [InlineData("++387 61 123 456")]        // two +
    [InlineData("+387 61 123 456 789 0")]   // 15 digits, but 21 characters
    public void Phone_Invalid_IsInvalid(string phone)
    {
        var result = _validator.Validate(new UpdateMyPatientProfileCommand { Phone = phone });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateMyPatientProfileCommand.Phone));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Phone_Empty_IsValid(string? phone)
    {
        // empty phone = remove it
        var result = _validator.Validate(new UpdateMyPatientProfileCommand { Phone = phone });

        Assert.True(result.IsValid);
    }

    // === date of birth ===

    [Fact]
    public void DateOfBirth_Today_IsValid()
    {
        var result = _validator.Validate(new UpdateMyPatientProfileCommand { DateOfBirth = Today() });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void DateOfBirth_Exactly120YearsAgo_IsValid()
    {
        var result = _validator.Validate(new UpdateMyPatientProfileCommand { DateOfBirth = Today().AddYears(-120) });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void DateOfBirth_Null_IsValid()
    {
        var result = _validator.Validate(new UpdateMyPatientProfileCommand { DateOfBirth = null });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void DateOfBirth_Tomorrow_IsInvalid()
    {
        var result = _validator.Validate(new UpdateMyPatientProfileCommand { DateOfBirth = Today().AddDays(1) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateMyPatientProfileCommand.DateOfBirth));
    }

    [Fact]
    public void DateOfBirth_OlderThan120Years_IsInvalid()
    {
        var result = _validator.Validate(new UpdateMyPatientProfileCommand { DateOfBirth = Today().AddYears(-120).AddDays(-1) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateMyPatientProfileCommand.DateOfBirth));
    }
}