namespace Market.Application.Modules.Catalog.DentalServices.Commands.Update;

public sealed class UpdateDentalServiceCommandHandler(IAppDbContext ctx) : IRequestHandler<UpdateDentalServiceCommand, Unit>
{
    public async Task<Unit> Handle(UpdateDentalServiceCommand request, CancellationToken ct)
    {
        var dentalService = await ctx.DentalServices.FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (dentalService is null)
            throw new MarketNotFoundException($"Usluga sa ID {request.Id} ne postoji");

        dentalService.CategoryId = request.CategoryId;
        dentalService.Name = request.Name.Trim();
        dentalService.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        dentalService.PriceBam = request.PriceBam;
        dentalService.DurationMinutes = request.DurationMinutes;
        dentalService.IsActive = request.IsActive;

        await ctx.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
