using Market.Application.Modules.Public.Queries.ListServiceCategories;
using Market.Application.Modules.Public.Queries.ListSpecializations;
using Market.Application.Modules.Public.Queries.ListServices;
using Market.Application.Modules.Public.Queries.ListDentists;
using Market.Application.Modules.Public.Queries.ListFreeSlots;
using Market.Shared.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Market.API.Controllers;

/// <summary>
/// Public catalog: services and dentists that anyone can see, without logging in.
/// Admin endpoints (api/dental-services, api/dentists...) stay admin-only.
/// </summary>
[Route("api/public")]
[ApiController]
[AllowAnonymous]
public sealed class PublicCatalogController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Categories that have at least one active service (for the filter).
    /// </summary>
    [HttpGet("service-categories")]
    [ProducesResponseType(typeof(List<ListPublicServiceCategoriesQueryDto>), StatusCodes.Status200OK)]
    public async Task<List<ListPublicServiceCategoriesQueryDto>> ListServiceCategories(CancellationToken ct)
    {
        return await sender.Send(new ListPublicServiceCategoriesQuery(), ct);
    }

    /// <summary>
    /// Specializations of dentists with an enabled account (for the filter).
    /// </summary>
    [HttpGet("specializations")]
    [ProducesResponseType(typeof(List<ListPublicSpecializationsQueryDto>), StatusCodes.Status200OK)]
    public async Task<List<ListPublicSpecializationsQueryDto>> ListSpecializations(CancellationToken ct)
    {
        return await sender.Send(new ListPublicSpecializationsQuery(), ct);
    }

    /// <summary>
    /// Active services with search, category, max price and sorting.
    /// </summary>
    [HttpGet("services")]
    [ProducesResponseType(typeof(PageResult<ListPublicServicesQueryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    public async Task<PageResult<ListPublicServicesQueryDto>> ListServices([FromQuery] ListPublicServicesQuery query, CancellationToken ct)
    {
        return await sender.Send(query, ct);
    }

    /// <summary>
    /// Dentists with an enabled account, with search (name) and specialization filter. No email.
    /// </summary>
    [HttpGet("dentists")]
    [ProducesResponseType(typeof(PageResult<ListPublicDentistsQueryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    public async Task<PageResult<ListPublicDentistsQueryDto>> ListDentists([FromQuery] ListPublicDentistsQuery query, CancellationToken ct)
    {
        return await sender.Send(query, ct);
    }

    /// <summary>
    /// Free slots (UTC) of a dentist for a service, from fromDate to toDate (local clinic dates, max 14 days).
    /// Not paged. Anyone can see them; booking requires login (#13).
    /// </summary>
    [HttpGet("free-slots")]
    [ProducesResponseType(typeof(List<ListPublicFreeSlotsQueryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    public async Task<List<ListPublicFreeSlotsQueryDto>> ListFreeSlots([FromQuery] ListPublicFreeSlotsQuery query, CancellationToken ct)
    {
        return await sender.Send(query, ct);
    }
}
