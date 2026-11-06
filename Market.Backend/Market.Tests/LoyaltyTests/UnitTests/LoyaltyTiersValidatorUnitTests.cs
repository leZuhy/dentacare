using Market.Application.Modules.Loyalty.LoyaltyTiers.Commands.Create;
using Market.Application.Modules.Loyalty.LoyaltyTiers.Commands.Update;
using Market.Domain.Entities.Loyalty;

namespace Market.Tests.LoyaltyTests.UnitTests;

public sealed class LoyaltyTiersValidatorUnitTests
{
    private static DatabaseContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new DatabaseContext(options, TimeProvider.System);
    }

    private static async Task<int> AddTierAsync(DatabaseContext context, string name, int minPoints, decimal discountPercent)
    {
        var tier = new LoyaltyTierEntity
        {
            Name = name,
            MinPoints = minPoints,
            DiscountPercent = discountPercent
        };
        context.LoyaltyTiers.Add(tier);
        await context.SaveChangesAsync();
        return tier.Id;
    }

    // Bronze 0/0 %, Silver 500/5 %, Gold 1500/10 % - the same tiers as the seed
    private static async Task AddDefaultTiersAsync(DatabaseContext context)
    {
        await AddTierAsync(context, "Bronze", 0, 0m);
        await AddTierAsync(context, "Silver", 500, 5m);
        await AddTierAsync(context, "Gold", 1500, 10m);
    }

    private static CreateLoyaltyTierCommand CreateCommand(
        string name = "Platinum",
        int minPoints = 3000,
        decimal discountPercent = 15m) => new()
        {
            Name = name,
            MinPoints = minPoints,
            DiscountPercent = discountPercent
        };

    [Fact]
    public async Task Create_ValidCommand_IsValid()
    {
        var context = GetInMemoryDbContext();
        await AddDefaultTiersAsync(context);
        var validator = new CreateLoyaltyTierCommandValidator(context);

        var result = await validator.ValidateAsync(CreateCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Create_DuplicateMinPoints_IsInvalid()
    {
        var context = GetInMemoryDbContext();
        await AddDefaultTiersAsync(context);
        var validator = new CreateLoyaltyTierCommandValidator(context);

        // Silver already starts at 500 points
        var result = await validator.ValidateAsync(CreateCommand(minPoints: 500, discountPercent: 6m));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateLoyaltyTierCommand.MinPoints));
    }

    [Theory]
    [InlineData(3000, 8)]   // more points than Gold, but a smaller discount
    [InlineData(1000, 12)]  // fewer points than Gold, but a bigger discount
    [InlineData(1000, 5)]   // more points than Silver, but the same discount
    public async Task Create_DiscountNotInOrder_IsInvalid(int minPoints, decimal discountPercent)
    {
        var context = GetInMemoryDbContext();
        await AddDefaultTiersAsync(context);
        var validator = new CreateLoyaltyTierCommandValidator(context);

        var result = await validator.ValidateAsync(CreateCommand(minPoints: minPoints, discountPercent: discountPercent));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateLoyaltyTierCommand.DiscountPercent));
    }

    [Fact]
    public async Task Create_TierBetweenSilverAndGold_IsValid()
    {
        var context = GetInMemoryDbContext();
        await AddDefaultTiersAsync(context);
        var validator = new CreateLoyaltyTierCommandValidator(context);

        // 1000 points is between Silver (500) and Gold (1500), 7 % is between 5 % and 10 %
        var result = await validator.ValidateAsync(CreateCommand(minPoints: 1000, discountPercent: 7m));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Update_BaseTierMinPoints_IsInvalid()
    {
        var context = GetInMemoryDbContext();
        var bronzeId = await AddTierAsync(context, "Bronze", 0, 0m);
        var validator = new UpdateLoyaltyTierCommandValidator(context);

        var command = new UpdateLoyaltyTierCommand
        {
            Id = bronzeId,
            Name = "Bronze",
            MinPoints = 100,
            DiscountPercent = 0m
        };

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateLoyaltyTierCommand.MinPoints));
    }

    [Fact]
    public async Task Update_KeepingOwnValues_IsValid()
    {
        var context = GetInMemoryDbContext();
        await AddTierAsync(context, "Bronze", 0, 0m);
        var silverId = await AddTierAsync(context, "Silver", 500, 5m);
        await AddTierAsync(context, "Gold", 1500, 10m);
        var validator = new UpdateLoyaltyTierCommandValidator(context);

        // the same name, points and discount as the row itself
        var command = new UpdateLoyaltyTierCommand
        {
            Id = silverId,
            Name = "Silver",
            MinPoints = 500,
            DiscountPercent = 5m
        };

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }
}