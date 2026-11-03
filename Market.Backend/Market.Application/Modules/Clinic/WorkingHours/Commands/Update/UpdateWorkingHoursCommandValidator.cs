namespace Market.Application.Modules.Clinic.WorkingHours.Commands.Update;

public sealed class UpdateWorkingHoursCommandValidator : AbstractValidator<UpdateWorkingHoursCommand>
{
    public UpdateWorkingHoursCommandValidator(IAppDbContext ctx)
    {
        RuleFor(x => x.DayOfWeek)
            .IsInEnum()
            .WithMessage("Dan u sedmici nije ispravan");

        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .WithMessage("Vrijeme završetka mora biti nakon vremena početka");

        RuleFor(x => x.StartTime)
            .MustAsync(async (command, startTime, ct) =>
            {
                var dentistId = await ctx.WorkingHours
                .Where(x => x.Id == command.Id)
                .Select(x => (int?)x.DentistId)
                .FirstOrDefaultAsync(ct);

                if (dentistId is null)
                    return true;

                return !await ctx.WorkingHours.AnyAsync(x =>
                    x.Id != command.Id &&
                    x.DentistId == dentistId &&
                    x.DayOfWeek == command.DayOfWeek &&
                    startTime < x.EndTime &&
                    x.StartTime < command.EndTime, ct);
            })
            .WithMessage("Interval se preklapa s postojećim radnim vremenom ovog stomatologa")
            .When(x => x.StartTime < x.EndTime);
    }
}
