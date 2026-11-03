using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Market.Application.Modules.Clinic.WorkingHours.Commands.Create
{
    public sealed class CreateWorkingHoursCommandValidator : AbstractValidator<CreateWorkingHoursCommand>
    {
        public CreateWorkingHoursCommandValidator(IAppDbContext ctx)
        {
            RuleFor(x => x.DentistId)
                .MustAsync(async (id, ct) => await ctx.Dentists.AnyAsync(x => x.Id == id, ct))
                .WithMessage("Stomatolog ne postoji");

            RuleFor(x => x.DayOfWeek)
                .IsInEnum()
                .WithMessage("Dan u sedmici nije validan");

            RuleFor(x => x.EndTime)
                .GreaterThan(x => x.StartTime)
                .WithMessage("Kraj radnog vremena mora biti poslije početka");

            RuleFor(x => x.StartTime)
                .MustAsync(async (command, startTime, ct) =>

                    !await ctx.WorkingHours.AnyAsync(x =>
                    x.DentistId == command.DentistId &&
                    x.DayOfWeek == command.DayOfWeek &&
                    startTime < x.EndTime &&
                    x.StartTime < command.EndTime, ct))
                .WithMessage("Interval se preklapa sa postojećim radnim vremenom ovog stomatologa")
                .When(x => x.StartTime < x.EndTime);
        }
    }
}
