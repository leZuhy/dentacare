using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Application.Modules.Catalog.Specializations.Commands.Create
{
    public sealed class CreateSpecializationCommand : IRequest<int>
    {
        public string Name { get; set; } = string.Empty;
    }
}
