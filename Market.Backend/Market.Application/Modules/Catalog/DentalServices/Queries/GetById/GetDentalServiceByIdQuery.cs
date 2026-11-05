namespace Market.Application.Modules.Catalog.DentalServices.Queries.GetById;

public sealed class GetDentalServiceByIdQuery : IRequest<GetDentalServiceByIdQueryDto>
{
    public int Id { get; init; }
}
