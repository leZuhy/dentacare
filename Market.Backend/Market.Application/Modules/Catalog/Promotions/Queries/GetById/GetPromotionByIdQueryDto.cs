namespace Market.Application.Modules.Catalog.Promotions.Queries.GetById;

public sealed class GetPromotionByIdQueryDto
{
    public required int Id { get; init; }
    public required string Title { get; init; }
    public required string ImageUrl { get; init; }
    public required string? TargetUrl { get; init; }
    public required bool IsActive { get; init; }
    public required DateTime? StartsAtUtc { get; init; }
    public required DateTime? EndsAtUtc { get; init; }
    public required int SortOrder { get; init; }
}
