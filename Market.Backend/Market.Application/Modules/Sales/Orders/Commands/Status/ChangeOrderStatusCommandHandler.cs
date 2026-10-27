using Market.Domain.Entities.Sales;

namespace Market.Application.Modules.Sales.Orders.Commands.Status;

/// <summary>
/// Handler for changing order status
/// </summary>
public class ChangeOrderStatusCommandHandler(IAppDbContext db, IAppCurrentUser currentUser) : IRequestHandler<ChangeOrderStatusCommand>
{
    public async Task Handle(ChangeOrderStatusCommand request, CancellationToken ct)
    {
        // Defense in depth: the endpoint is already protected by the "Admin" policy.
        if (!currentUser.IsAdmin)
            throw new MarketForbiddenException("Samo administrator može mijenjati status narudžbe.");

        var order = await db.Orders
            .FirstOrDefaultAsync(o => o.Id == request.Id, ct)
            ?? throw new MarketNotFoundException($"Narudžba (ID={request.Id}) nije pronađena.");

        // Validate status transition
        ValidateStatusTransition(order.Status, request.NewStatus);

        // Update status
        order.Status = request.NewStatus;

        // If marking as paid, set paid date
        if (request.NewStatus == OrderStatusType.Paid && !order.PaidAtUtc.HasValue)
        {
            order.PaidAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }

    private static void ValidateStatusTransition(OrderStatusType current, OrderStatusType next)
    {
        // Define valid transitions
        var validTransitions = new Dictionary<OrderStatusType, OrderStatusType[]>
        {
            { OrderStatusType.Draft, new[] { OrderStatusType.Confirmed, OrderStatusType.Cancelled } },
            { OrderStatusType.Confirmed, new[] { OrderStatusType.Paid, OrderStatusType.Cancelled } },
            { OrderStatusType.Paid, new[] { OrderStatusType.Completed, OrderStatusType.Cancelled } },
            { OrderStatusType.Completed, Array.Empty<OrderStatusType>() },
            { OrderStatusType.Cancelled, Array.Empty<OrderStatusType>() }
        };

        if (!validTransitions.ContainsKey(current))
        {
            throw new MarketBusinessRuleException("order.status.unknown", $"Nepoznat trenutni status: {current}.");
        }

        if (!validTransitions[current].Contains(next))
        {
            throw new MarketBusinessRuleException("order.status.invalidTransition",
                $"Status se ne može promijeniti iz {current} u {next}. " +
                $"Dozvoljeni prelazi: {string.Join(", ", validTransitions[current])}.");
        }
    }
}
