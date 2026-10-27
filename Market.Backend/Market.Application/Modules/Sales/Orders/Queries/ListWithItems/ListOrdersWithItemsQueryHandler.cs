namespace Market.Application.Modules.Sales.Orders.Queries.ListWithItems;

public sealed class ListOrdersWithItemsQueryHandler(IAppDbContext ctx, IAppCurrentUser currentUser)
        : IRequestHandler<ListOrdersWithItemsQuery, PageResult<ListOrdersWithItemsQueryDto>>
{

    public async Task<PageResult<ListOrdersWithItemsQueryDto>> Handle(ListOrdersWithItemsQuery request, CancellationToken ct)
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

        // Newest orders first; ThenByDescending(Id) makes the order stable (OrderedAtUtc is not unique),
        // so paging never skips or repeats rows.
        var projectedQuery = q.OrderByDescending(x => x.OrderedAtUtc)
            .ThenByDescending(x => x.Id)
            .Select(x => new ListOrdersWithItemsQueryDto
            {
                Id = x.Id,
                ReferenceNumber = x.ReferenceNumber,
                User = new ListOrdersWithItemsQueryDtoUser
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
                //"x.Items" ili "ctx.OrderItems.Where(x => x.OrderId == x.Id)"
                Items = x.Items.Select(i => new ListOrdersWithItemsQueryDtoItem
                {
                    Id = i.Id,
                    Product = new ListOrdersWithItemsQueryDtoItemProduct
                    {
                        ProductId = i.ProductId,
                        ProductName = i.Product!.Name,
                        ProductCategoryName = i.Product!.Category!.Name
                    },
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Subtotal = i.Subtotal,
                    DiscountAmount = i.DiscountAmount,
                    DiscountPercent = i.DiscountPercent,
                    Total = i.Total
                }).ToList()
            });

        return await PageResult<ListOrdersWithItemsQueryDto>.FromQueryableAsync(projectedQuery, request.Paging, ct);
    }
}
