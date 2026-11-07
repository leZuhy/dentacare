namespace Market.Application.Modules.Patients.Commands.UpdateMyPatientProfile;

/// <summary>
/// Changes phone and date of birth of the logged-in patient.
/// Null or empty removes the value.
/// </summary>
public sealed class UpdateMyPatientProfileCommand : IRequest
{
    /// <summary>
    /// Phone number, e.g. "061 123 456" or "+387 61 123 456"; empty or null removes it. (optional)
    /// </summary>
    public string? Phone { get; init; }

    /// <summary>
    /// Date of birth in "yyyy-MM-dd" format; null removes it. (optional)
    /// </summary>
    public DateOnly? DateOfBirth { get; init; }
}