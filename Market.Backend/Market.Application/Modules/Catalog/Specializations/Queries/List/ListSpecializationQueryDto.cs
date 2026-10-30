using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Catalog.Specializations.Queries.List
{
    public sealed class ListSpecializationQueryDto
    {
        public required int Id { get; init; }
        public required string Name { get; init; }
    }
}
