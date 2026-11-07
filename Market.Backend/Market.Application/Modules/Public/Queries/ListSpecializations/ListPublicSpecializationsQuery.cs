namespace Market.Application.Modules.Public.Queries.ListSpecializations;

/// <summary>
/// Specializations for the public filter: only those of at least one dentist with an enabled account. No paging.
/// </summary>
public sealed class ListPublicSpecializationsQuery : IRequest<List<ListPublicSpecializationsQueryDto>>
{
}