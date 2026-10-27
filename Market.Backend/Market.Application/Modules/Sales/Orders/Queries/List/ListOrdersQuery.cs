using Market.Domain.Entities.Sales;

namespace Market.Application.Modules.Sales.Orders.Queries.List;

public sealed class ListOrdersQuery : BasePagedQuery<ListOrdersQueryDto>
{
    /// <summary>Search by reference number (contains, case-insensitive). (optional)</summary>
    public string? Search { get; init; }

    /// <summary>
    /// Only orders in this status, e.g. <c>?status=2</c> for <see cref="OrderStatusType.Confirmed"/>. (optional)
    /// </summary>
    public OrderStatusType? Status { get; init; }
}
