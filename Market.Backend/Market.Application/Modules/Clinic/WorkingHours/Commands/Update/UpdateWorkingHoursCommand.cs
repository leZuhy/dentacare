namespace Market.Application.Modules.Clinic.WorkingHours.Commands.Update;

public sealed class UpdateWorkingHoursCommand : IRequest<Unit>
{
    [JsonIgnore]
    public int Id { get; set; }

    public DayOfWeek DayOfWeek { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
}
