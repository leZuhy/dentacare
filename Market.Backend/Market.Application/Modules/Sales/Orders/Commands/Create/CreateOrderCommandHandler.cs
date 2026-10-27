using Market.Domain.Entities.Sales;
using System.Globalization;

namespace Market.Application.Modules.Sales.Orders.Commands.Create;

public class CreateOrderCommandHandler(IAppDbContext ctx, IAppCurrentUser currentUser)
    : IRequestHandler<CreateOrderCommand, int>
{
    public async Task<int> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        // Input format (items not empty, quantities, duplicates) is checked by CreateOrderCommandValidator.
        var userId = currentUser.UserId
            ?? throw new MarketUnauthorizedException("Korisnik nije prijavljen.");

        #region Create order and set basic properties
        var nowUtc = DateTime.UtcNow;
        var order = new OrderEntity
        {
            ReferenceNumber = GenerateReferenceNumber(nowUtc),
            MarketUserId = userId,
            OrderedAtUtc = nowUtc,
            Status = OrderStatusType.Draft,
            TotalAmount = 0m, //
            Note = request.Note
        };
        ctx.Orders.Add(order);
        #endregion

        #region Load products from database and prepare a map

        // pokupiti sve id-ove proizvoda koji se naručuju
        List<int> productIds = request.Items.Select(ri => ri.ProductId).ToList(); // ne treba hashset jer filter se radi u bazi

        List<ProductEntity> products = await ctx.Products
            .Where(p => productIds.Contains(p.Id)) //<-- dorada nakon nastave za poboljsanje performansi: filtrirati samo proizvode koji su u request.Items
            .AsNoTracking()
            .ToListAsync(ct);

        Dictionary<int, ProductEntity> productsMap = products.ToDictionary(x => x.Id);
        #endregion

        #region Create order items and calculate totals

        // za demo svrhe, svi proizvodi imaju 5% popusta
        decimal discountPercent = 0.05m;

        foreach (var item in request.Items)
        {
            ProductEntity? product = productsMap.GetValueOrDefault(item.ProductId); //<--- bolja performansa O(n) jer koristi dictionary

            if (product is null)
            {
                throw new MarketNotFoundException($"Proizvod (ID={item.ProductId}) nije pronađen.");
            }

            if (!product.IsEnabled)
            {
                throw new MarketBusinessRuleException("order.product.disabled",
                    $"Proizvod {product.Name} je onemogućen i ne može se naručiti.");
            }

            // Stock is only checked, not reserved or decremented.
            // Reservation/decrement is intentionally left for your own business flow (e.g. on Confirmed or Paid).
            if (item.Quantity > product.StockQuantity)
            {
                throw new MarketBusinessRuleException("order.product.insufficientStock",
                    $"Nema dovoljno zaliha za proizvod {product.Name} (dostupno: {product.StockQuantity}, traženo: {item.Quantity}).");
            }

            decimal subtotal = RoundMoney(product.Price * item.Quantity);
            decimal discountAmount = RoundMoney(subtotal * discountPercent);
            decimal total = RoundMoney(subtotal - discountAmount);

            var orderItem = new OrderItemEntity
            {
                Order = order,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = product.Price,
                Subtotal = subtotal,
                DiscountPercent = discountPercent,
                DiscountAmount = discountAmount,
                Total = total
            };

            ctx.OrderItems.Add(orderItem);
            order.TotalAmount += RoundMoney(orderItem.Total);
        }
        #endregion

        await ctx.SaveChangesAsync(ct);

        return order.Id;
    }

    /// <summary>
    /// Human-readable reference number, e.g. <c>ORD-20261019-1A2B3C4D</c> (date + 8 random hex characters).
    /// </summary>
    private static string GenerateReferenceNumber(DateTime nowUtc)
    {
        var random = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return string.Create(CultureInfo.InvariantCulture, $"ORD-{nowUtc:yyyyMMdd}-{random}");
    }

    private static decimal RoundMoney(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
