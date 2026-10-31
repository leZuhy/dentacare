using Market.Domain.Entities.Clinic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Clinic.Dentists.Commands.Create
{
    public sealed class CreateDentistCommandHandler
        (IAppDbContext ctx,IPasswordHasher<MarketUserEntity> hasher) : IRequestHandler<CreateDentistCommand, int>
    {
        public async Task<int> Handle(CreateDentistCommand request,CancellationToken ct)
        {
            var user = new MarketUserEntity
            {
                Firstname = request.Firstname.Trim(),
                Lastname = request.Lastname.Trim(),
                Email = request.Email.Trim().ToLowerInvariant(),
                Role = Role.Dentist,
                IsEnabled = true
            };

            user.PasswordHash = hasher.HashPassword(user, request.Password);

            var dentist = new DentistEntity
            {
                User = user,
                SpecializationId = request.SpecializationId,
                Bio = request.Bio.TrimToNull()
            };

            //Adding the dentist will also add the user because of the relationship between them
            ctx.Dentists.Add(dentist);

            await ctx.SaveChangesAsync(ct);

            return dentist.Id;
        }
    }
}
