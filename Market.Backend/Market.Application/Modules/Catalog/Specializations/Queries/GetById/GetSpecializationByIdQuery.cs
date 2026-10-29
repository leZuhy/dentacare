using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Catalog.Specializations.Queries.GetById
{
    public sealed class GetSpecializationByIdQuery : IRequest<GetSpecializationByIdQueryDto>
    {
        public required int Id { get; init; }
    }
}
