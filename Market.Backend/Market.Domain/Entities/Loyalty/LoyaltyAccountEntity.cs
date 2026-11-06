using Market.Domain.Common;
using Market.Domain.Entities.Patients;
using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Domain.Entities.Loyalty;

/// <summary>
/// Loyalty account of a patient (1:1). Every new patient gets one on the base tier (MinPoints = 0).
/// PointsBalance goes down when points are redeemed; LifetimePoints only grows and decides the tier.
/// </summary>
public sealed class LoyaltyAccountEntity : BaseEntity
{
    public int PatientId { get; set; }
    public PatientEntity Patient { get; set; } = null!;
    public int TierId { get; set; }
    public LoyaltyTierEntity Tier { get; set; } = null!;
    public int PointsBalance { get; set; }
    public int LifetimePoints { get; set; }
}
