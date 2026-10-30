namespace Market.Application.Modules.Catalog.ServiceCategories.Commands.Update;

public sealed class UpdateServiceCategoryCommandHandler(IAppDbContext ctx)
    : IRequestHandler<UpdateServiceCategoryCommand, Unit>
{
    public async Task<Unit> Handle(UpdateServiceCategoryCommand request, CancellationToken ct)
    {
        var category = await ctx.ServiceCategories.FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (category is null)
            throw new MarketNotFoundException($"Kategorija sa Id-em {request.Id} ne postoji.");

        category.Name = request.Name.Trim();
        await ctx.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
