using System.Text.Json.Serialization;

namespace Market.Application.Modules.Appointments.Commands.Complete;

/// <summary>
/// The logged-in dentist completes one of their own appointments:
/// writes the findings (medical record) and may add extra performed services.
/// The service the patient chose when booking always stays.
/// Body: { "findings": "...", "therapy": "...", "additionalDentalServiceIds": [3, 3, 5] }
/// </summary>
public sealed class CompleteAppointmentCommand : IRequest
{
    /// <summary>From the route, not from the body.</summary>
    [JsonIgnore]
    public int Id { get; set; }

    public string Findings { get; init; } = string.Empty;

    public string? Therapy { get; init; }

    /// <summary>
    /// Extra performed services. Each id is one procedure, so the same id can appear more than once.
    /// </summary>
    public List<int> AdditionalDentalServiceIds { get; init; } = new();
}
