using Market.Application.Modules.Finance.Currencies.Queries.Convert;
using Market.Application.Modules.Finance.Currencies.Queries.List;
using Market.Application.Modules.Finance.ExchangeRates.Commands.Create;
using Market.Application.Modules.Finance.ExchangeRates.Commands.Delete;
using Market.Application.Modules.Finance.ExchangeRates.Commands.Refresh;
using Market.Application.Modules.Finance.ExchangeRates.Queries.List;
using Market.Shared.Constants;

namespace Market.API.Controllers;

/// <summary>
/// Currencies and exchange rates. Prices and orders stay in KM (BAM); conversion is for display only.
/// </summary>
[ApiController]
[Route("[controller]")]
public class CurrenciesController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Enabled currencies with the rate valid now (base currency first).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<List<ListCurrenciesQueryDto>> List(CancellationToken ct)
    {
        var result = await sender.Send(new ListCurrenciesQuery(), ct);
        return result;
    }

    /// <summary>
    /// Converts an amount, e.g. <c>GET /Currencies/convert?amount=100&amp;from=EUR&amp;to=BAM</c>
    /// (optional <c>atUtc</c> uses the rates valid at that moment).
    /// </summary>
    [HttpGet("convert")]
    [AllowAnonymous]
    public async Task<ConvertCurrencyQueryDto> Convert([FromQuery] ConvertCurrencyQuery query, CancellationToken ct)
    {
        var result = await sender.Send(query, ct);
        return result;
    }

    /// <summary>
    /// Exchange rate history, newest first (optional filter <c>currencyCode</c>).
    /// </summary>
    [HttpGet("rates")]
    [Authorize(Policy = PolicyNames.Admin)]
    public async Task<PageResult<ListExchangeRatesQueryDto>> ListRates([FromQuery] ListExchangeRatesQuery query, CancellationToken ct)
    {
        var result = await sender.Send(query, ct);
        return result;
    }

    /// <summary>
    /// Adds a new rate (rates are not edited; a newer rate replaces the old one from its date).
    /// </summary>
    [HttpPost("rates")]
    [Authorize(Policy = PolicyNames.Admin)]
    public async Task<ActionResult<int>> CreateRate(CreateExchangeRateCommand command, CancellationToken ct)
    {
        int id = await sender.Send(command, ct);

        // There is no "get rate by id" endpoint, so the response has no Location header.
        return StatusCode(StatusCodes.Status201Created, new { id });
    }

    [HttpDelete("rates/{id:int}")]
    [Authorize(Policy = PolicyNames.Admin)]
    public async Task DeleteRate(int id, CancellationToken ct)
    {
        await sender.Send(new DeleteExchangeRateCommand { Id = id }, ct);
        // no return -> 204 No Content
    }

    /// <summary>
    /// Downloads the latest rates from the configured provider ("ExchangeRates:Provider").
    /// Returns 409 while the provider is "None" (default).
    /// </summary>
    [HttpPost("rates/refresh")]
    [Authorize(Policy = PolicyNames.Admin)]
    public async Task<RefreshExchangeRatesCommandDto> RefreshRates(CancellationToken ct)
    {
        var result = await sender.Send(new RefreshExchangeRatesCommand(), ct);
        return result;
    }
}
