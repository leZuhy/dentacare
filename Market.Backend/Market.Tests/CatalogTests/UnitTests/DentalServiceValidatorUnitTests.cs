using Market.Application.Modules.Catalog.DentalServices.Commands.Create;
using Market.Application.Modules.Catalog.DentalServices.Commands.Update;
using Market.Domain.Entities.Catalog;

namespace Market.Tests.CatalogTests.UnitTests;

public sealed class DentalServiceValidatorUnitTests
{
    private static DatabaseContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) 
            .Options;

        return new DatabaseContext(options, TimeProvider.System);
    }

    private static async Task<int> AddCategoryAsync(DatabaseContext context)
    {
        var category = new ServiceCategoryEntity { Name = "Konzervativa" };
        context.ServiceCategories.Add(category);
        await context.SaveChangesAsync();
        return category.Id;
    }

    private static async Task<int> AddServiceAsync(DatabaseContext context, int categoryId, string name)
    {
        var service = new DentalServiceEntity
        {
            CategoryId = categoryId,
            Name = name,
            PriceBam = 80m,
            DurationMinutes = 45
        };
        context.DentalServices.Add(service);
        await context.SaveChangesAsync();
        return service.Id;
    }

    private static CreateDentalServiceCommand CreateCommand(
        int categoryId,
        string name = "Plomba",
        decimal price = 80m,
        int duration = 45) => new()
        {
            CategoryId = categoryId,
            Name = name,
            PriceBam = price,
            DurationMinutes = duration
        };

    [Fact]
    public async Task Create_ValidCommand_IsValid()
    {
        var context = GetInMemoryDbContext();
        var categoryId = await AddCategoryAsync(context);
        var validator = new CreateDentalServiceCommandValidator(context);

        var result = await validator.ValidateAsync(CreateCommand(categoryId));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Create_PriceNotPositive_IsInvalid(decimal price)
    {
        var context = GetInMemoryDbContext();
        var categoryId = await AddCategoryAsync(context);
        var validator = new CreateDentalServiceCommandValidator(context);

        var result = await validator.ValidateAsync(CreateCommand(categoryId, price: price));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateDentalServiceCommand.PriceBam));
    }

    [Fact]
    public async Task Create_PriceWithThreeDecimals_IsInvalid()
    {
        var context = GetInMemoryDbContext();
        var categoryId = await AddCategoryAsync(context);
        var validator = new CreateDentalServiceCommandValidator(context);

        var result = await validator.ValidateAsync(CreateCommand(categoryId, price: 80.555m));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateDentalServiceCommand.PriceBam));
    }

    [Theory]
    [InlineData(20)]
    [InlineData(50)]
    public async Task Create_DurationNotMultipleOf15_IsInvalid(int duration)
    {
        var context = GetInMemoryDbContext();
        var categoryId = await AddCategoryAsync(context);
        var validator = new CreateDentalServiceCommandValidator(context);

        var result = await validator.ValidateAsync(CreateCommand(categoryId, duration: duration));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateDentalServiceCommand.DurationMinutes));
    }

    [Fact]
    public async Task Create_DuplicateName_IsInvalid()
    {
        var context = GetInMemoryDbContext();
        var categoryId = await AddCategoryAsync(context);
        await AddServiceAsync(context, categoryId, "Plomba");
        var validator = new CreateDentalServiceCommandValidator(context);

        var result = await validator.ValidateAsync(CreateCommand(categoryId, name: " Plomba "));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateDentalServiceCommand.Name));
    }

    [Fact]
    public async Task Update_KeepingOwnName_IsValid()
    {
        var context = GetInMemoryDbContext();
        var categoryId = await AddCategoryAsync(context);
        var id = await AddServiceAsync(context, categoryId, "Plomba");
        var validator = new UpdateDentalServiceCommandValidator(context);

        var command = new UpdateDentalServiceCommand
        {
            Id = id,
            CategoryId = categoryId,
            Name = "Plomba",
            PriceBam = 90m,
            DurationMinutes = 45,
            IsActive = true
        };

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }
}
