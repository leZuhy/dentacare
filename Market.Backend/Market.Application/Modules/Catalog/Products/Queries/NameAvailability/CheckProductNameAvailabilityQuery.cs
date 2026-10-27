namespace Market.Application.Modules.Catalog.Products.Queries.NameAvailability;

/// <summary>
/// Checks whether a product name is still free, e.g. for an async validator on the product form.
/// The create/update validators check the same rule again, so the server stays the source of truth.
/// </summary>
public sealed class CheckProductNameAvailabilityQuery : IRequest<CheckProductNameAvailabilityQueryDto>
{
    /// <summary>Name to check (compared case-insensitively, without leading/trailing spaces).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Product to ignore — the one being edited. (optional)</summary>
    public int? ExcludeId { get; init; }
}
