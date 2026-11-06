using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Loyalty.LoyaltyTiers.Queries.GetById;

public sealed class GetLoyaltyTierByIdQueryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public int MinPoints { get; init; }
    public decimal DiscountPercent { get; init; }
}
