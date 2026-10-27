namespace Market.Application.Modules.Sales.Orders.Queries.List;

public sealed class ListOrdersQueryHandler(IAppDbContext ctx, IAppCurrentUser currentUser)
        : IRequestHandler<ListOrdersQuery, PageResult<ListOrdersQueryDto>>
{

    public async Task<PageResult<ListOrdersQueryDto>> Handle(ListOrdersQuery request, CancellationToken ct)
    {
        var q = ctx.Orders.AsNoTracking();

        if (!currentUser.IsAdmin)
        {
            q = q.Where(x => x.MarketUserId == currentUser.UserId);
        }
        var searchTerm = request.Search?.Trim().ToLower() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            q = q.Where(x => x.ReferenceNumber.ToLower().Contains(searchTerm));
        }

        if (request.Status is not null)
        {
            q = q.Where(x => x.Status == request.Status);
        }

        // Newest orders first; ThenByDescending(Id) makes the order stable (OrderedAtUtc is not unique),
        // so paging never skips or repeats rows.
        var projectedQuery = q.OrderByDescending(x => x.OrderedAtUtc)
            .ThenByDescending(x => x.Id)
            .Select(x => new ListOrdersQueryDto
            {
                Id = x.Id,
                ReferenceNumber = x.ReferenceNumber,
                User = new ListOrdersQueryDtoUser
                {
                    UserFirstname = x.MarketUser!.Firstname,
                    UserLastname = x.MarketUser!.Lastname,
                    UserAddress = x.MarketUser!.Address,
                    UserCity = x.MarketUser!.City,
                },
                OrderedAtUtc = x.OrderedAtUtc,
                PaidAtUtc = x.PaidAtUtc,
                Status = x.Status,
                TotalAmount = x.TotalAmount,
                Note = x.Note,
            });

        return await PageResult<ListOrdersQueryDto>.FromQueryableAsync(projectedQuery, request.Paging, ct);
    }
}
