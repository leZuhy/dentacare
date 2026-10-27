namespace Market.Application.Modules.Catalog.Products.Queries.GetById;

public class GetProductByIdQueryHandler(IAppDbContext context, IAppCurrentUser currentUser)
    : IRequestHandler<GetProductByIdQuery, GetProductByIdQueryDto>
{
    public async Task<GetProductByIdQueryDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var q = context.Products
            .Where(c => c.Id == request.Id);

        // Same visibility rule as the product list: only the administrator sees disabled products
        // and products in disabled categories; for everyone else they do not exist (404).
        if (!currentUser.IsAdmin)
        {
            q = q.Where(x => x.IsEnabled && x.Category!.IsEnabled);
        }

        var dto = await q
            .Select(x => new GetProductByIdQueryDto
            {
                Id = x.Id,
                Name = x.Name,
                IsEnabled = x.IsEnabled,
                Description = x.Description,
                Price = x.Price,
                StockQuantity = x.StockQuantity,
                CategoryName = x.Category!.Name,
                CategoryId = x.CategoryId,
                ImageUrl = x.ImageUrl
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (dto == null)
        {
            throw new MarketNotFoundException($"Proizvod (ID={request.Id}) nije pronađen.");
        }

        return dto;
    }
}
