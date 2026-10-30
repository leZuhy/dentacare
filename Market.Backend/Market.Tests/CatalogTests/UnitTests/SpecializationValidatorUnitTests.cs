using Market.Application.Modules.Catalog.Specializations.Commands.Create;
using Market.Application.Modules.Catalog.Specializations.Commands.Update;
using Market.Domain.Entities.Catalog;

namespace Market.Tests.CatalogTests.UnitTests;

/// <summary>
/// Unique specialization name: case-insensitive, ignores the edited row and deleted rows.
/// </summary>
public class SpecializationValidatorUnitTests
{
    private static DatabaseContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Each test gets a new database
            .Options;

        return new DatabaseContext(options, TimeProvider.System);
    }

    private static async Task<SpecializationEntity> AddSpecializationAsync(DatabaseContext context, string name)
    {
        var specialization = new SpecializationEntity { Name = name };
        context.Specializations.Add(specialization);
        await context.SaveChangesAsync();
        return specialization;
    }

    [Fact]
    public async Task Create_NameExistsInOtherCase_IsInvalid()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        await AddSpecializationAsync(context, "Ortodoncija");
        var validator = new CreateSpecializationCommandValidator(context);

        // Act
        var result = await validator.ValidateAsync(new CreateSpecializationCommand { Name = " ortodoncija " });

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateSpecializationCommand.Name));
    }

    [Fact]
    public async Task Create_NameOfDeletedSpecialization_IsValid()
    {
        // Arrange: soft-deleted row keeps the name in the table
        using var context = GetInMemoryDbContext();
        var deleted = await AddSpecializationAsync(context, "Protetika");
        context.Specializations.Remove(deleted);
        await context.SaveChangesAsync();
        var validator = new CreateSpecializationCommandValidator(context);

        // Act
        var result = await validator.ValidateAsync(new CreateSpecializationCommand { Name = "Protetika" });

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Update_SameNameOnSameSpecialization_IsValid()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var existing = await AddSpecializationAsync(context, "Ortodoncija");
        var validator = new UpdateSpecializationCommandValidator(context);

        // Act: saving without changing the name must not report a duplicate of itself
        var result = await validator.ValidateAsync(
            new UpdateSpecializationCommand { Id = existing.Id, Name = "Ortodoncija" });

        // Assert
        Assert.True(result.IsValid);
    }
}