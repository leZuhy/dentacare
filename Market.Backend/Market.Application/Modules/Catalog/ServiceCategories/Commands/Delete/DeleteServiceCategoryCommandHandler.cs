namespace Market.Application.Modules.Catalog.ServiceCategories.Commands.Delete;

public sealed class DeleteServiceCategoryCommandHandler(IAppDbContext ctx)
    : IRequestHandler<DeleteServiceCategoryCommand, Unit>
{
    public async Task<Unit> Handle(DeleteServiceCategoryCommand request, CancellationToken ct)
    {
        var category = await ctx.ServiceCategories.FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (category is null)
            throw new MarketNotFoundException($"Kategorija sa Id-em {request.Id} ne postoji.");

        // TODO: when DentalServiceEntity exists, reject deleting a category that services still use (409).

        // Soft delete: DatabaseContext sets IsDeleted = true instead of removing the row.
        ctx.ServiceCategories.Remove(category);
        await ctx.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
