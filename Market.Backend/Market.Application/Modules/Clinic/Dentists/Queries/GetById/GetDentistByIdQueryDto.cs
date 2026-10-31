using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Clinic.Dentists.Queries.GetById
{
    public sealed class GetDentistByIdQueryDto
    {
        public required int Id { get; init; }
        public required string Firstname { get; init; }
        public required string Lastname { get; init; }
        public required string Email { get; init; }
        public required int SpecializationId { get; init; }
        public required string SpecializationName { get; init; }
        public string? Bio { get; init; }
    }
}
