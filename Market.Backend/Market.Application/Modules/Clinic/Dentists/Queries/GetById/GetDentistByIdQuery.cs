using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Clinic.Dentists.Queries.GetById
{
    public sealed class GetDentistByIdQuery : IRequest<GetDentistByQueryDto>
    {
        public required int Id { get; init; }
    }
}
