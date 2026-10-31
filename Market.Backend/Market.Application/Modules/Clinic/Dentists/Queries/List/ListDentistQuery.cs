using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Clinic.Dentists.Queries.List
{
    public sealed class ListDentistQuery : BasePagedQuery<ListDentistQueryDto>
    {
        public string? Search { get; init; }
        public int? SpecializationId { get; init; }
    }
}
