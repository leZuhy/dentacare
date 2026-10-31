using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Clinic.Dentists.Commands.Delete
{
    public sealed class DeleteDentistCommandHandler(IAppDbContext ctx) : IRequestHandler<DeleteDentistCommand, Unit>
    {
        public async Task<Unit> Handle(DeleteDentistCommand request, CancellationToken ct)
        {
            var dentist = await ctx.Dentists
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

            if (dentist is null)
                throw new MarketNotFoundException($"Stomatolog sa Id-em {request.Id} ne postoji.");

            // TODO: when AppointmentEntity exists, reject deleting a dentist with future appointments (409).

            ctx.Dentists.Remove(dentist);
            ctx.Users.Remove(dentist.User);

            await ctx.SaveChangesAsync(ct);

            return Unit.Value;
        }
    }
}
