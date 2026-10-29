using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Catalog.Specializations.Queries.GetById
{
    public sealed class GetSpecializationByIdQueryDto
    {
        public required int Id { get; init; }
        public required string Name { get; init; }
    }
}
