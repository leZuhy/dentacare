using System;
using System.Collections.Generic;
using System.Text;
using Market.Domain.Entities.Appointments;

namespace Market.Application.Modules.Clinic.Dentists.Commands.Delete
{
    public sealed class DeleteDentistCommandHandler(IAppDbContext ctx) : IRequestHandler<DeleteDentistCommand, Unit>
    {
        public async Task<Unit> Handle(DeleteDentistCommand request, CancellationToken ct)
        {
            var dentist = await ctx.Dentists
                .Include(x => x.User)
                .Include(x => x.WorkingHours)
                .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

            if (dentist is null)
                throw new MarketNotFoundException($"Stomatolog sa Id-em {request.Id} ne postoji.");

            var hasScheduledAppointments = await ctx.Appointments
                .AnyAsync(x => x.DentistId == dentist.Id
                && x.Status == AppointmentStatus.Scheduled, ct);

            if (hasScheduledAppointments)
                throw new MarketConflictException("Stomatolog ima zakazane termine i ne može se obrisati.");

            ctx.WorkingHours.RemoveRange(dentist.WorkingHours);
            ctx.Dentists.Remove(dentist);
            ctx.Users.Remove(dentist.User);

            await ctx.SaveChangesAsync(ct);

            return Unit.Value;
        }
    }
}
