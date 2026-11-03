namespace Market.Application.Modules.Clinic.WorkingHours.Queries.List;

public sealed class ListWorkingHoursQuery : BasePagedQuery<ListWorkingHoursQueryDto>
{
    public int? DentistId { get; init; }
    public DayOfWeek? DayOfWeek { get; init; }
}
