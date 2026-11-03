using Market.Domain.Entities.Clinic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Clinic.WorkingHours.Commands.Create
{
    public sealed class CreateWorkingHoursCommandHandler(IAppDbContext ctx) : IRequestHandler<CreateWorkingHoursCommand,int>
    {
        public async Task<int> Handle(CreateWorkingHoursCommand request,CancellationToken ct)
        {
            var workingHours = new WorkingHoursEntity
            {
                DentistId = request.DentistId,
                DayOfWeek = request.DayOfWeek,
                StartTime = request.StartTime,
                EndTime = request.EndTime
            };

            ctx.WorkingHours.Add(workingHours);

            await ctx.SaveChangesAsync(ct);

            return workingHours.Id;
        }
    }
}
