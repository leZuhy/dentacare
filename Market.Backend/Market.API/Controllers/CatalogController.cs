using Market.Application.Modules.Catalog.CatalogHome.Queries.GetCatalogHome;

namespace Market.API.Controllers;

[ApiController]
[Route("[controller]")]
public class CatalogController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Public home page data: enabled categories, newest products, active promotions and counts.
    /// Example: <c>GET /Catalog/home?categoryLimit=8&amp;productLimit=12</c>.
    /// </summary>
    [HttpGet("home")]
    [AllowAnonymous]
    public async Task<GetCatalogHomeQueryDto> GetHome([FromQuery] GetCatalogHomeQuery query, CancellationToken ct)
    {
        var result = await sender.Send(query, ct);
        return result;
    }
}
