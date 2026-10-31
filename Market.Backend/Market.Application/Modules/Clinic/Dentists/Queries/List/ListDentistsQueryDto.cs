using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Clinic.Dentists.Queries.List
{
    public sealed class ListDentistsQueryDto
    {
        public required int Id { get; init; }
        public required string Firstname { get; init; }
        public required string Lastname { get; init; }
        public required string Email { get; init; }
        public required string SpecializationName { get; init; }
    }
}
