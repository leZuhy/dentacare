namespace Market.Application.Modules.Clinic.WorkingHours.Queries.GetById;

public sealed class GetWorkingHoursByIdQuery : IRequest<GetWorkingHoursByIdQueryDto>
{
    public required int Id { get; init; }
}
