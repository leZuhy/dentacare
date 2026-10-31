using Market.Application.Modules.Clinic.Dentists.Commands.Create;
using Market.Domain.Entities.Catalog;
using Market.Domain.Entities.Identity;

namespace Market.Tests.ClinicTests.UnitTests;

/// <summary>
/// Creating a dentist: the email must be free (case-insensitive) and the specialization must exist and not be deleted.
/// </summary>
public class DentistValidatorUnitTests
{
    private static DatabaseContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Each test gets a new database
            .Options;

        return new DatabaseContext(options, TimeProvider.System);
    }

    private static CreateDentistCommand ValidCommand(int specializationId, string email = "novi@test.local") => new()
    {
        Firstname = "Ana",
        Lastname = "Kovač",
        Email = email,
        Password = "Dentist123!",
        SpecializationId = specializationId
    };

    [Fact]
    public async Task Create_EmailUsedInOtherCase_IsInvalid()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var specialization = new SpecializationEntity { Name = "Ortodoncija" };
        context.Specializations.Add(specialization);
        context.Users.Add(new MarketUserEntity
        {
            Firstname = "Postojeći",
            Lastname = "Korisnik",
            Email = "ana@test.local",
            PasswordHash = "x",
            Role = Role.Patient
        });
        await context.SaveChangesAsync();
        var validator = new CreateDentistCommandValidator(context);

        // Act
        var result = await validator.ValidateAsync(ValidCommand(specialization.Id, " ANA@test.local "));

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateDentistCommand.Email));
    }

    [Fact]
    public async Task Create_DeletedSpecialization_IsInvalid()
    {
        // Arrange: soft-deleted specialization is still in the table, but must not be selectable
        using var context = GetInMemoryDbContext();
        var specialization = new SpecializationEntity { Name = "Protetika" };
        context.Specializations.Add(specialization);
        await context.SaveChangesAsync();
        context.Specializations.Remove(specialization);
        await context.SaveChangesAsync();
        var validator = new CreateDentistCommandValidator(context);

        // Act
        var result = await validator.ValidateAsync(ValidCommand(specialization.Id));

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateDentistCommand.SpecializationId));
    }
}