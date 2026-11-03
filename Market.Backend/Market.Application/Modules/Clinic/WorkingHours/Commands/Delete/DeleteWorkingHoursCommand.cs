namespace Market.Application.Modules.Clinic.WorkingHours.Commands.Delete;

public sealed class DeleteWorkingHoursCommand : IRequest<Unit>
{
    public required int Id { get; init; }
}
