using Market.Domain.Entities.Catalog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Catalog.Specializations.Commands.Create
{
    public sealed class CreateSpecializationCommandHandler(IAppDbContext ctx) 
        : IRequestHandler<CreateSpecializationCommand, int>
    {
        public async Task<int> Handle(CreateSpecializationCommand request, CancellationToken ct)
        {
            var specialization = new SpecializationEntity
            {
                Name = request.Name.Trim()
            };

            ctx.Specializations.Add(specialization);
            await ctx.SaveChangesAsync(ct);

            return specialization.Id;
        }
    }
}
