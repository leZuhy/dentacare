using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Clinic.Dentists.Commands.Create
{
    public sealed class CreateDentistCommand : IRequest<int>
    {
        public string Firstname { get; init; } = string.Empty;
        public string Lastname { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;

        public string Password { get; init; } = string.Empty;

        public int SpecializationId { get; init; }
        public string? Bio { get; init; }
    }
}
