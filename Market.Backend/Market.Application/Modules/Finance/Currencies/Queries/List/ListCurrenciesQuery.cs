namespace Market.Application.Modules.Finance.Currencies.Queries.List;

/// <summary>
/// All enabled currencies with the rate valid now (a short list, so it is not paged).
/// </summary>
public sealed class ListCurrenciesQuery : IRequest<List<ListCurrenciesQueryDto>>
{
}
