using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Catalog.Specializations.Commands.Update
{
    public sealed class UpdateSpecializationCommandHandler(IAppDbContext ctx) : IRequestHandler<UpdateSpecializationCommand, Unit>
    {
        public async Task<Unit> Handle(UpdateSpecializationCommand request, CancellationToken ct)
        {
            var specialization = await ctx.Specializations.FirstOrDefaultAsync(x => x.Id == request.Id, ct);

            if(specialization is null)
                throw new MarketNotFoundException($"Specijalizacija sa Id-em {request.Id} ne postoji.");

            specialization.Name = request.Name.Trim();
            await ctx.SaveChangesAsync(ct);

            return Unit.Value;
        }
    }
}
