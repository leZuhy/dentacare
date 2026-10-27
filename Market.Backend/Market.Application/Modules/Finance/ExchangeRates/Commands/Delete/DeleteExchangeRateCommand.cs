namespace Market.Application.Modules.Finance.ExchangeRates.Commands.Delete;

public sealed class DeleteExchangeRateCommand : IRequest<Unit>
{
    public required int Id { get; set; }
}
