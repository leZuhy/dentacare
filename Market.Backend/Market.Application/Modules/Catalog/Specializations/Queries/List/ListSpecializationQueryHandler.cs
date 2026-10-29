using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Market.Application.Modules.Catalog.Specializations.Queries.List
{
    public sealed class ListSpecializationQueryHandler(IAppDbContext ctx) 
        : IRequestHandler<ListSpecializationQuery, PageResult<ListSpecializationQueryDto>>
    {
        public async Task<PageResult<ListSpecializationQueryDto>> Handle(ListSpecializationQuery request, CancellationToken ct)
        {
            //No alteration of the queryable is done, so AsNoTracking is safe to use for faster performance
            var q = ctx.Specializations.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim().ToLower();
                q = q.Where(x => x.Name.ToLower().Contains(search));
            }

            var projectedQuery = q.OrderBy(x => x.Name)
                .ThenBy(x => x.Id)
                .Select(x => new ListSpecializationQueryDto
                {
                    Id = x.Id,
                    Name = x.Name
                });

            return await PageResult<ListSpecializationQueryDto>.FromQueryableAsync(projectedQuery, request.Paging, ct);
        }
    }
}
