namespace Market.Application.Modules.Public.Queries.ListDentists;

/// <summary>
/// Public list of dentists with an enabled account, with search and specialization filter.
/// Query string: ?search=dino&amp;specializationId=1&amp;paging.page=1
/// </summary>
public sealed class ListPublicDentistsQuery : BasePagedQuery<ListPublicDentistsQueryDto>
{
    /// <summary>Part of the first name, last name or "first last". (optional)</summary>
    public string? Search { get; init; }

    /// <summary>Only dentists with this specialization. (optional)</summary>
    public int? SpecializationId { get; init; }
}