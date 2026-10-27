namespace Market.Application.Modules.Catalog.Promotions.Commands.Create;

public sealed class CreatePromotionCommand : IRequest<int>
{
    public required string Title { get; set; }
    public required string ImageUrl { get; set; }
    public string? TargetUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime? StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public int SortOrder { get; set; }
}
