namespace Market.Application.Modules.Catalog.Products.Queries.NameAvailability;

public sealed class CheckProductNameAvailabilityQueryDto
{
    public required bool Available { get; init; }
}
