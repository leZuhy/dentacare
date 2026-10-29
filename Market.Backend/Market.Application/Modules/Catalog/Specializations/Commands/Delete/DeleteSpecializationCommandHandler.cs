using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Catalog.Specializations.Commands.Delete
{
    public sealed class DeleteSpecializationCommandHandler(IAppDbContext ctx) : IRequestHandler<DeleteSpecializationCommand, Unit>
    {
        public async Task<Unit> Handle(DeleteSpecializationCommand request, CancellationToken ct)
        {
            var specialization = await ctx.Specializations.FirstOrDefaultAsync(x => x.Id == request.Id, ct);


            if(specialization is null)
                throw new MarketNotFoundException($"Specijalizacija sa Id-em {request.Id} ne postoji.");

            // TODO: when DentistEntity exists, reject deleting a specialization that dentists still use (409).

            // Soft delete: DatabaseContext sets IsDeleted = true instead of removing the row.

            ctx.Specializations.Remove(specialization);
            await ctx.SaveChangesAsync(ct);

            return Unit.Value;
        }
    }
}
