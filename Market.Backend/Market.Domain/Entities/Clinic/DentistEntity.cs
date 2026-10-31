using Market.Domain.Common;
using Market.Domain.Entities.Catalog;
using Market.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Domain.Entities.Clinic
{
    //Dentist profile, extends the user entity with additional information specific to dentists.
    public sealed class DentistEntity : BaseEntity
    {
        public int UserId { get; set; }
        public MarketUserEntity User { get; set; } = null!;

        public int SpecializationId { get; set; }
        public SpecializationEntity Specialization { get; set; } = null!;

        public string? Bio { get; set; }

        public static class Constraints
        {
            public const int BioMaxLength = 1000;
        }
    }
}
