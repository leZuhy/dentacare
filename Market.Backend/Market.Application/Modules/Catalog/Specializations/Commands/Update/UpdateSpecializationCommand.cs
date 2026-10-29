using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Market.Application.Modules.Catalog.Specializations.Commands.Update
{
    public sealed class UpdateSpecializationCommand : IRequest<Unit>
    {
        //Id gets taken from the route URL, not from the request body, so we don't want it to be serialized in the request body.
        [JsonIgnore]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
