using Market.Domain.Entities.Catalog;

namespace Market.Application.Modules.Catalog.DentalServices.Commands.Create;

public sealed class CreateDentalServiceCommandHandler(IAppDbContext ctx) : IRequestHandler<CreateDentalServiceCommand, int>
{
    public async Task<int> Handle(CreateDentalServiceCommand request, CancellationToken ct)
    {
        var dentalService = new DentalServiceEntity
        {
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            PriceBam = request.PriceBam,
            DurationMinutes = request.DurationMinutes
        };

        ctx.DentalServices.Add(dentalService);
        await ctx.SaveChangesAsync(ct);

        return dentalService.Id;
    }
}
