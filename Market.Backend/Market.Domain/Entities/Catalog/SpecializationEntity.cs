using Market.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Domain.Entities.Catalog
{
    /// <summary>
    /// Represents a specialization entity in the catalog.
    ///</summary>
    public class SpecializationEntity : BaseEntity
    {
        public string Name { get; set; } = string.Empty;


        public static class Constraints
        {
            public const int NameMaxLength = 100;
        }
    }
}
