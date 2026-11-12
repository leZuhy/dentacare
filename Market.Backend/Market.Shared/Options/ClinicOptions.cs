using System.ComponentModel.DataAnnotations;

namespace Market.Shared.Options;

/// <summary>
/// "Clinic" section: settings of the clinic itself.
/// </summary>
public sealed class ClinicOptions
{
    public const string SectionName = "Clinic";

    /// <summary>
    /// IANA time zone of the clinic, e.g. "Europe/Sarajevo".
    /// Working hours are in this zone; appointments are stored in UTC.
    /// </summary>
    [Required]
    public string TimeZoneId { get; init; } = string.Empty;
    /// <summary>
    /// How many days ahead a patient can book an appointment (e.g. 60).
    /// Free slots after that day are not shown either.
    /// </summary>
    [Range(1, 365)]
    public int MaxBookingDaysAhead { get; init; }
}