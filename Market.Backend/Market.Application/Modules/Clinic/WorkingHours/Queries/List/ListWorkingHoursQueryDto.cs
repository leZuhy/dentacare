namespace Market.Application.Modules.Clinic.WorkingHours.Queries.List;

public sealed class ListWorkingHoursQueryDto
{
    public required int Id { get; init; }
    public required int DentistId { get; init; }
    public required string DentistFullName { get; init; }
    public required DayOfWeek DayOfWeek { get; init; }
    public required TimeOnly StartTime { get; init; }
    public required TimeOnly EndTime { get; init; }
}
