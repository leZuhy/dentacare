using Market.Domain.Entities.Sales;

namespace Market.Infrastructure.Database.Configurations.Sales;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItemEntity>
{
    public void Configure(EntityTypeBuilder<OrderItemEntity> builder)
    {
        builder
            .ToTable("OrderItems");

        builder
          .HasOne(x => x.Product)
          .WithMany() // ako nemamo navigaciju, onda stavimo samo WithMany()
          .HasForeignKey(x => x.ProductId)
          // Restrict — the database rejects a hard DELETE of a product that is used in order items.
          // Soft delete (IsDeleted = true) is an UPDATE and never reaches this FK rule,
          // so the same rule is also checked in DeleteProductCommandHandler.
          .OnDelete(DeleteBehavior.Restrict);


        builder
            .HasOne(x => x.Order)
            .WithMany(x => x.Items) // OrderEntity ima navigaciju Items, pa je navodimo
            .HasForeignKey(x => x.OrderId)
            // Cascade — a hard DELETE of an order also deletes its items in the database.
            // Soft delete of an order does NOT soft-delete its items automatically.
            .OnDelete(DeleteBehavior.Cascade);
    }
}