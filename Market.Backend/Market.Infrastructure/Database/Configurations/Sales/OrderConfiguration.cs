using Market.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;

namespace Market.Infrastructure.Database.Configurations.Sales;

public class OrderConfiguration : IEntityTypeConfiguration<OrderEntity>
{
    public void Configure(EntityTypeBuilder<OrderEntity> builder)
    {
        builder
            .ToTable("Orders");

        builder
            .Property(x => x.ReferenceNumber)
            .HasMaxLength(OrderEntity.Constraints.ReferenceMaxLength);

        builder
          .HasOne(x => x.MarketUser)
          .WithMany() // ako nemamo navigaciju, onda stavimo samo WithMany()
          .HasForeignKey(x => x.MarketUserId)
          // Restrict — the database rejects a hard DELETE of a user that has orders.
          // Soft delete (IsDeleted = true) is an UPDATE and never reaches this FK rule.
          .OnDelete(DeleteBehavior.Restrict);

    }
}
