using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Clinic.Dentists.Commands.Update
{
    public sealed class UpdateDentistCommand : IRequest<Unit>
    {
        [JsonIgnore]
        public int Id { get; set; }

        public string Firstname { get; init; } = string.Empty;
        public string Lastname { get; init; } = string.Empty;
        public int SpecializationId { get; init; }
        public string? Bio { get; init; }
    }
}
