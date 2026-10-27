namespace Market.Application.Common;

/// <summary>
/// Paging parameters for list queries (query string: <c>paging.page</c>, <c>paging.pageSize</c>).
/// Out-of-range values are clamped instead of rejected.
/// </summary>
public sealed class PageRequest
{
    /// <summary>Page size used when the client does not send one.</summary>
    public const int DefaultPageSize = 10;

    /// <summary>Upper limit for <see cref="PageSize"/> (the client may use it to load whole lists, e.g. for dropdowns).</summary>
    public const int MaxPageSize = 1000;

    private readonly int _page = 1;
    private readonly int _pageSize = DefaultPageSize;

    /// <summary>1-based page number; values below 1 become 1.</summary>
    public int Page
    {
        get => _page;
        init => _page = Math.Max(1, value);
    }

    /// <summary>Number of items per page: values below 1 become <see cref="DefaultPageSize"/>, larger than <see cref="MaxPageSize"/> are capped.</summary>
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value < 1 ? DefaultPageSize : Math.Min(value, MaxPageSize);
    }

    [JsonIgnore]
    public int SkipCount => (Page - 1) * PageSize;
}
