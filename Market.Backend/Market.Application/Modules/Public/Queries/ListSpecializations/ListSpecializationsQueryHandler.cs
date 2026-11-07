namespace Market.Application.Modules.Public.Queries.ListSpecializations;

public sealed class ListPublicSpecializationsQueryHandler(IAppDbContext ctx)
    : IRequestHandler<ListPublicSpecializationsQuery, List<ListPublicSpecializationsQueryDto>>
{
    public async Task<List<ListPublicSpecializationsQueryDto>> Handle(ListPublicSpecializationsQuery request, CancellationToken ct)
    {
        return await ctx.Specializations
            .AsNoTracking()
            .Where(s => ctx.Dentists.Any(d => d.SpecializationId == s.Id && d.User.IsEnabled))
            .OrderBy(s => s.Name)
            .Select(s => new ListPublicSpecializationsQueryDto
            {
                Id = s.Id,
                Name = s.Name
            })
            .ToListAsync(ct);
    }
}