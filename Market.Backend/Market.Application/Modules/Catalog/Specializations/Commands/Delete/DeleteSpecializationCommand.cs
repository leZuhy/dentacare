using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Catalog.Specializations.Commands.Delete
{
    public sealed class DeleteSpecializationCommand : IRequest<Unit>
    {
        public required int Id { get; set; }
    }
}
