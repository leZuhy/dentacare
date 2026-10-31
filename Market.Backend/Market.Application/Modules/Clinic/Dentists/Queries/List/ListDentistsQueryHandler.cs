using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Clinic.Dentists.Queries.List
{
    public sealed class ListDentistsQueryHandler(IAppDbContext ctx) : IRequestHandler<ListDentistsQuery, PageResult<ListDentistsQueryDto>>
    {
        public async Task<PageResult<ListDentistsQueryDto>> Handle(ListDentistsQuery request, CancellationToken ct)
        {
            var q = ctx.Dentists.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim().ToLower();

                q = q.Where(x =>
                (x.User.Firstname + " " + x.User.Lastname).ToLower().Contains(search) ||
                x.User.Email.ToLower().Contains(search));
            }

            if (request.SpecializationId is not null)
                q = q.Where(x => x.SpecializationId == request.SpecializationId);

            var projectedQuery = q
                .OrderBy(x => x.User.Lastname)
                .ThenBy(x => x.User.Firstname)
                .ThenBy(x => x.Id)
                .Select(x => new ListDentistsQueryDto
                {
                    Id = x.Id,
                    Firstname = x.User.Firstname,
                    Lastname = x.User.Lastname,
                    Email = x.User.Email,
                    SpecializationName = x.Specialization.Name
                });

            return await PageResult<ListDentistsQueryDto>.FromQueryableAsync(projectedQuery, request.Paging, ct);
        }
    }
}
