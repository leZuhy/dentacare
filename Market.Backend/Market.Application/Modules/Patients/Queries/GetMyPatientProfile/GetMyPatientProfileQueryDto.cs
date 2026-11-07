
namespace Market.Application.Modules.Patients.Queries.GetMyPatientProfile;

public sealed class GetMyPatientProfileQueryDto
{
    public required string? Phone { get; init; }
    public required DateOnly? DateOfBirth { get; init; }
}
