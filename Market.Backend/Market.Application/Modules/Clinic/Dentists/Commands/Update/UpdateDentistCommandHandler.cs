using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Clinic.Dentists.Commands.Update
{
    public sealed class UpdateDentistCommandHandler(IAppDbContext ctx) : IRequestHandler<UpdateDentistCommand, Unit>
    {
        public async Task<Unit> Handle(UpdateDentistCommand request, CancellationToken ct)
        {
            //Both the dentist and user are changed
            var dentist = await ctx.Dentists
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

            if (dentist is null)
                throw new MarketNotFoundException($"Stomatolog sa Id-em {request.Id} ne postoji.");

            dentist.User.Firstname = request.Firstname;
            dentist.User.Lastname = request.Lastname;
            dentist.SpecializationId = request.SpecializationId;
            dentist.Bio = request.Bio.TrimToNull();

            await ctx.SaveChangesAsync(ct);

            return Unit.Value;
        }
    }
}
