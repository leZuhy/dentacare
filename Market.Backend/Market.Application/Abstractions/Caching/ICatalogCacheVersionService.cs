namespace Market.Application.Abstractions.Caching;

/// <summary>
/// Version number of the catalog data, used as part of cache keys (see <see cref="ICacheService"/>).
/// Instead of deleting every cached catalog key after a change, a write handler bumps the version:
/// old keys are no longer read and expire on their own (TTL).
/// <para>
/// The starter registers it but does not use it anywhere.
/// </para>
/// </summary>
public interface ICatalogCacheVersionService
{
    /// <summary>
    /// Current catalog version (1 when it was never bumped).
    /// </summary>
    Task<long> GetVersionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments the catalog version and returns the new value.
    /// Call it after <c>SaveChangesAsync</c> in every handler that changes catalog data.
    /// </summary>
    Task<long> BumpVersionAsync(CancellationToken cancellationToken = default);
}
