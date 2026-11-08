using Market.Domain.Common;
using Market.Domain.Entities.Catalog;

namespace Market.Domain.Entities.Appointments;

/// <summary>
/// One dental service within an appointment (one row = one procedure).
/// The price is copied when the service is added, so later price changes don't affect old invoices.
/// </summary>
public sealed class AppointmentServiceEntity : BaseEntity
{
    public int AppointmentId { get; set; }
    public AppointmentEntity Appointment { get; set; } = null!;

    public int DentalServiceId { get; set; }
    public DentalServiceEntity DentalService { get; set; } = null!;

    public decimal PriceAtTimeBam { get; set; }
}