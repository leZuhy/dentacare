namespace Market.Application.Abstractions.Caching;

/// <summary>
/// Simple "cache-aside" helper over <c>IDistributedCache</c>.
/// <para>
/// The starter registers it but does not use it anywhere. Without configuration it runs on an in-memory cache;
/// with a non-empty <c>ConnectionStrings:Redis</c> it runs on Redis, and handlers do not change.
/// </para>
/// <para>
/// Example — cache the public catalog home page (GetCatalogHomeQueryHandler) for 5 minutes:
/// <code>
/// var version = await catalogCacheVersion.GetVersionAsync(ct);
/// var key = $"catalog:v{version}:home:c{categoryLimit}:p{productLimit}";
///
/// var dto = await cache.GetOrCreateAsync(key, token => LoadHomeFromDatabaseAsync(token), TimeSpan.FromMinutes(5), ct);
/// </code>
/// Every handler that changes the catalog (products, categories, promotions) then calls
/// <see cref="ICatalogCacheVersionService.BumpVersionAsync"/> after <c>SaveChangesAsync</c>,
/// so the next request uses a new key and reads fresh data.
/// </para>
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Returns the cached value for <paramref name="key"/>; if there is none, calls <paramref name="factory"/>,
    /// stores its result for <paramref name="ttl"/> and returns it. Values are stored as JSON,
    /// so <typeparamref name="T"/> must be a serializable DTO (not an EF entity).
    /// </summary>
    Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default) where T : class;
}
