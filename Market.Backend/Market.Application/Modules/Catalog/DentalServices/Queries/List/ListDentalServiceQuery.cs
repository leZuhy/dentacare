namespace Market.Application.Modules.Catalog.DentalServices.Queries.List;

public sealed class ListDentalServiceQuery : BasePagedQuery<ListDentalServiceQueryDto>
{
    public string? Search { get; init; }
    public int? CategoryId { get; init; }
    public bool? IsActive { get; init; } 
}
