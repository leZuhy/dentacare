namespace Market.Application.Modules.Catalog.ServiceCategories.Commands.Delete;

public sealed class DeleteServiceCategoryCommandHandler(IAppDbContext ctx)
    : IRequestHandler<DeleteServiceCategoryCommand, Unit>
{
    public async Task<Unit> Handle(DeleteServiceCategoryCommand request, CancellationToken ct)
    {
        var category = await ctx.ServiceCategories.FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (category is null)
            throw new MarketNotFoundException($"Kategorija sa Id-em {request.Id} ne postoji.");

        var isInUse = await ctx.DentalServices
            .AnyAsync(x => x.CategoryId == request.Id, ct);

        if (isInUse)
            throw new MarketConflictException("Kategorija se koristi kod usluga i ne može biti obrisana.");
        ctx.ServiceCategories.Remove(category);


        await ctx.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
