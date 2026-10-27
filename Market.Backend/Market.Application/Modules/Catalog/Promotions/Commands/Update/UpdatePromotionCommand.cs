namespace Market.Application.Modules.Catalog.Promotions.Commands.Update;

public sealed class UpdatePromotionCommand : IRequest<Unit>
{
    [JsonIgnore]
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string ImageUrl { get; set; }
    public string? TargetUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime? StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public int SortOrder { get; set; }
}
