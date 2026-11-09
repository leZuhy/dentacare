namespace Market.Application.Modules.Public.Queries.ListFreeSlots;

/// <summary>
/// One free slot in UTC. The frontend shows it in local time.
/// </summary>
public sealed class ListPublicFreeSlotsQueryDto
{
    public required DateTime StartUtc { get; init; }
    public required DateTime EndUtc { get; init; }
}
