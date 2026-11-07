namespace Market.Application.Modules.Public.Queries.ListServices;

/// <summary>
/// Sort order of the public service list. In the URL as text: ?sort=priceAsc
/// </summary>
public enum PublicServiceSort
{
    Name = 0,
    PriceAsc = 1,
    PriceDesc = 2
}