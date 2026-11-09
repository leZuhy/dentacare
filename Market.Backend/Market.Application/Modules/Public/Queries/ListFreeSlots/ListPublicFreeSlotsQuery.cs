namespace Market.Application.Modules.Public.Queries.ListFreeSlots;

/// <summary>
/// Free slots of one dentist for one service, from FromDate to ToDate (local clinic dates, both included).
/// Not paged: it is a calculation over a limited range (max 14 days), not a CRUD list.
/// Query string: ?dentistId=1&amp;dentalServiceId=2&amp;fromDate=2026-11-16&amp;toDate=2026-11-20
/// </summary>
public sealed class ListPublicFreeSlotsQuery : IRequest<List<ListPublicFreeSlotsQueryDto>>
{
    public int DentistId { get; init; }

    public int DentalServiceId { get; init; }

    /// <summary>First day (local clinic date).</summary>
    public DateOnly FromDate { get; init; }

    /// <summary>Last day, included. (optional, null = same day as FromDate)</summary>
    public DateOnly? ToDate { get; init; }
}
