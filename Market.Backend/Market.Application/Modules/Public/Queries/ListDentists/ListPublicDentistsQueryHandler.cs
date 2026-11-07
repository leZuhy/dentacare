namespace Market.Application.Modules.Public.Queries.ListDentists;

public sealed class ListPublicDentistsQueryHandler(IAppDbContext ctx)
    : IRequestHandler<ListPublicDentistsQuery, PageResult<ListPublicDentistsQueryDto>>
{
    public async Task<PageResult<ListPublicDentistsQueryDto>> Handle(ListPublicDentistsQuery request, CancellationToken ct)
    {
        // only dentists whose account is enabled; deleted ones are hidden by the global filter
        var q = ctx.Dentists
            .AsNoTracking()
            .Where(x => x.User.IsEnabled);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            q = q.Where(x => (x.User.Firstname + " " + x.User.Lastname).ToLower().Contains(search));
        }

        if (request.SpecializationId is not null)
            q = q.Where(x => x.SpecializationId == request.SpecializationId);

        var projectedQuery = q
            .OrderBy(x => x.User.Lastname)
            .ThenBy(x => x.User.Firstname)
            .ThenBy(x => x.Id)
            .Select(x => new ListPublicDentistsQueryDto
            {
                Id = x.Id,
                Firstname = x.User.Firstname,
                Lastname = x.User.Lastname,
                SpecializationId = x.SpecializationId,
                SpecializationName = x.Specialization.Name,
                Bio = x.Bio
            });

        return await PageResult<ListPublicDentistsQueryDto>.FromQueryableAsync(projectedQuery, request.Paging, ct);
    }
}