using Market.Domain.Entities.Sales;

namespace Market.Application.Modules.Sales.Orders.Commands.Update;

public class UpdateOrderCommandHandler(IAppDbContext ctx, IAppCurrentUser currentUser)
    : IRequestHandler<UpdateOrderCommand, int>
{
    public async Task<int> Handle(UpdateOrderCommand request, CancellationToken ct)
    {
        // Input format (items not empty, quantities, duplicates) is checked by UpdateOrderCommandValidator.

        #region Querying existing order and set basic properties
        var q = ctx.Orders
            .Where(x => x.Id == request.Id);

        if (!currentUser.IsAdmin)
        {
            q = q.Where(x => x.MarketUserId == currentUser.UserId);
        }

        var order = await q
            .FirstOrDefaultAsync(ct);

        if (order is null)
            throw new MarketNotFoundException($"Narudžba (ID={request.Id}) nije pronađena.");

        if (order.Status != OrderStatusType.Draft)
            throw new MarketBusinessRuleException("order.update.notDraft",
                $"Narudžba {order.ReferenceNumber} se ne može mijenjati jer nije u statusu Draft.");

        order.Note = request.Note;
        order.TotalAmount = 0m;
        #endregion

        #region Query existing order items

        List<OrderItemEntity> exisingOrderItems = await ctx.OrderItems
                   .Where(oi => oi.OrderId == order.Id)
                   //.AsNoTracking() // ne smije biti AsNoTracking jer želimo update orderItema
                   .ToListAsync(ct); //<--- prihvatljivo za peformanse: jedan (1) sql upit za sve iteme

        Dictionary<int, OrderItemEntity> exisingOrderItemsMap = exisingOrderItems.ToDictionary(x => x.Id); //mapa postojecih itema po ID-u

        #endregion

        #region Delete order items that are not in the API request

        var itemsToDelete = exisingOrderItems
            .Where(oi => request.Items.All(ri => ri.Id != oi.Id))
            .ToList();

        ctx.OrderItems.RemoveRange(itemsToDelete);

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

        #region Querying existing or create order items and calculate totals

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
            OrderItemEntity? orderItem = null;

            //jel insert
            if (item.Id == 0)
            {
                // insert logic
                orderItem = new OrderItemEntity
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
            }
            else
            {
                //update logic

                //orderItem = orderItems
                //    .Where(oi => oi.Id == item.Id) // ovo je O(n2)
                //    .FirstOrDefault(); //<--- nije kriticno za peformanse: ovo nije sql upit !!

                orderItem = exisingOrderItemsMap.GetValueOrDefault(item.Id); //<--- bolja performansa O(1) jer koristi dictionary

                if (orderItem is null)
                {
                    throw new MarketNotFoundException($"Stavka narudžbe (ID={item.Id}) nije pronađena u narudžbi (ID={order.Id}).");
                }

                //orderItem.OrderId = order.Id; // ne moze se menjati

                // The product of an existing item may be changed. The new product was checked above
                // (exists, enabled, enough stock) like the product of a new item, and the price below
                // is the current price of the new product.
                orderItem.ProductId = item.ProductId;
                orderItem.Quantity = item.Quantity;
                orderItem.UnitPrice = product.Price;
                orderItem.Subtotal = subtotal;
                orderItem.DiscountPercent = discountPercent;
                orderItem.DiscountAmount = discountAmount;
                orderItem.Total = total;
            }

            order.TotalAmount += RoundMoney(orderItem.Total);
        }
        #endregion

        await ctx.SaveChangesAsync(ct);

        return order.Id;
    }
    private static decimal RoundMoney(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}