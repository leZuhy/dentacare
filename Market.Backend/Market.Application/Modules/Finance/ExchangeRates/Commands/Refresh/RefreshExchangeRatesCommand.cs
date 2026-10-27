namespace Market.Application.Modules.Finance.ExchangeRates.Commands.Refresh;

/// <summary>
/// Downloads the latest rates from the configured <see cref="IExchangeRateProvider"/> and stores them as new rates.
/// </summary>
public sealed class RefreshExchangeRatesCommand : IRequest<RefreshExchangeRatesCommandDto>
{
}
