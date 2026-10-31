using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Clinic.Dentists.Commands.Delete
{
    public sealed class DeleteDentistCommand : IRequest<Unit>
    {
        public required int Id { get; set; }
    }
}
