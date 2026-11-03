using Market.Application.Modules.Clinic.WorkingHours.Commands.Create;
using Market.Application.Modules.Clinic.WorkingHours.Commands.Update;
using Market.Domain.Entities.Catalog;
using Market.Domain.Entities.Clinic;
using Market.Domain.Entities.Identity;

namespace Market.Tests.ClinicTests.UnitTests;

/// <summary>
/// Working hours: end must be after start, and intervals of the same dentist on the same day must not overlap.
/// Touching intervals (08-12 and 12-16) are allowed.
/// </summary>
public class WorkingHoursValidatorUnitTests
{
    private static DatabaseContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Each test gets a new database
            .Options;

        return new DatabaseContext(options, TimeProvider.System);
    }

    private static async Task<int> AddDentistAsync(DatabaseContext context)
    {
        var dentist = new DentistEntity
        {
            User = new MarketUserEntity
            {
                Firstname = "Ana",
                Lastname = "Kovač",
                Email = "ana@test.local",
                PasswordHash = "x",
                Role = Role.Dentist
            },
            Specialization = new SpecializationEntity { Name = "Ortodoncija" }
        };
        context.Dentists.Add(dentist);
        await context.SaveChangesAsync();
        return dentist.Id;
    }

    // Adds a Monday interval, e.g. AddMondayAsync(context, dentistId, 8, 12) is 08:00-12:00
    private static async Task<int> AddMondayAsync(DatabaseContext context, int dentistId, int startHour, int endHour)
    {
        var workingHours = new WorkingHoursEntity
        {
            DentistId = dentistId,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(startHour, 0),
            EndTime = new TimeOnly(endHour, 0)
        };
        context.WorkingHours.Add(workingHours);
        await context.SaveChangesAsync();
        return workingHours.Id;
    }

    private static CreateWorkingHoursCommand MondayCommand(int dentistId, int startHour, int endHour) => new()
    {
        DentistId = dentistId,
        DayOfWeek = DayOfWeek.Monday,
        StartTime = new TimeOnly(startHour, 0),
        EndTime = new TimeOnly(endHour, 0)
    };

    [Fact]
    public async Task Create_EndBeforeStart_IsInvalid()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var dentistId = await AddDentistAsync(context);
        var validator = new CreateWorkingHoursCommandValidator(context);

        // Act
        var result = await validator.ValidateAsync(MondayCommand(dentistId, 12, 8));

        // Assert: only the end-time error, the overlap check is skipped
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateWorkingHoursCommand.EndTime));
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(CreateWorkingHoursCommand.StartTime));
    }

    [Fact]
    public async Task Create_OverlappingInterval_IsInvalid()
    {
        // Arrange: Monday 08-12 exists
        using var context = GetInMemoryDbContext();
        var dentistId = await AddDentistAsync(context);
        await AddMondayAsync(context, dentistId, 8, 12);
        var validator = new CreateWorkingHoursCommandValidator(context);

        // Act: 11-16 overlaps 11-12
        var result = await validator.ValidateAsync(MondayCommand(dentistId, 11, 16));

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateWorkingHoursCommand.StartTime));
    }

    [Fact]
    public async Task Create_TouchingInterval_IsValid()
    {
        // Arrange: Monday 08-12 exists
        using var context = GetInMemoryDbContext();
        var dentistId = await AddDentistAsync(context);
        await AddMondayAsync(context, dentistId, 8, 12);
        var validator = new CreateWorkingHoursCommandValidator(context);

        // Act: 12-16 starts exactly when 08-12 ends
        var result = await validator.ValidateAsync(MondayCommand(dentistId, 12, 16));

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Update_ExtendingOwnInterval_IsValid()
    {
        // Arrange: the only row is Monday 08-12
        using var context = GetInMemoryDbContext();
        var dentistId = await AddDentistAsync(context);
        var id = await AddMondayAsync(context, dentistId, 8, 12);
        var validator = new UpdateWorkingHoursCommandValidator(context);

        // Act: 08-13 overlaps the old version of the same row, which must be ignored
        var result = await validator.ValidateAsync(new UpdateWorkingHoursCommand
        {
            Id = id,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(13, 0)
        });

        // Assert
        Assert.True(result.IsValid);
    }
}