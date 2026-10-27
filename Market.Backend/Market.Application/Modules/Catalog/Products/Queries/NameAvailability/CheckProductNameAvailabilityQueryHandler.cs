namespace Market.Application.Modules.Catalog.Products.Queries.NameAvailability;

public sealed class CheckProductNameAvailabilityQueryHandler(IAppDbContext ctx)
    : IRequestHandler<CheckProductNameAvailabilityQuery, CheckProductNameAvailabilityQueryDto>
{
    public async Task<CheckProductNameAvailabilityQueryDto> Handle(CheckProductNameAvailabilityQuery request, CancellationToken ct)
    {
        // Same rule as in CreateProductCommandValidator / UpdateProductCommandValidator:
        // case-insensitive, soft-deleted products are skipped by the global query filter.
        var normalized = request.Name.Trim().ToLower();

        var taken = await ctx.Products
            .AnyAsync(p => p.Id != request.ExcludeId && p.Name.ToLower() == normalized, ct);

        return new CheckProductNameAvailabilityQueryDto { Available = !taken };
    }
}
