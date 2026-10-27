namespace Market.Infrastructure.Database.Configurations.Identity;

public sealed class UserEntityConfiguration : IEntityTypeConfiguration<MarketUserEntity>
{
    public void Configure(EntityTypeBuilder<MarketUserEntity> b)
    {
        b.ToTable("Users");

        b.HasKey(x => x.Id);

        // Unique only among non-deleted users: a soft-deleted user keeps its row,
        // and without the filter its email could never be registered again.
        b.HasIndex(x => x.Email)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        b.Property(x => x.Firstname)
            .IsRequired()
            .HasMaxLength(MarketUserEntity.Constraints.FirstnameMaxLength);

        b.Property(x => x.Lastname)
            .IsRequired()
            .HasMaxLength(MarketUserEntity.Constraints.LastnameMaxLength);

        b.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(MarketUserEntity.Constraints.EmailMaxLength);

        b.Property(x => x.PasswordHash)
            .IsRequired();

        b.Property(x => x.Address)
            .HasMaxLength(MarketUserEntity.Constraints.AddressMaxLength);

        b.Property(x => x.City)
            .HasMaxLength(MarketUserEntity.Constraints.CityMaxLength);

        // Roles
        b.Property(x => x.IsAdmin)
            .HasDefaultValue(false);

        b.Property(x => x.IsManager)
            .HasDefaultValue(false);

        // No HasDefaultValue(true) for IsEmployee/IsEnabled: EF Core does not send a bool that has the
        // CLR default (false) when the column has a database default, so "false" could never be saved -
        // the database would silently store "true". Defaults belong in the entity (IsEnabled = true).

        b.Property(x => x.TokenVersion)
            .HasDefaultValue(0);

        // Navigation
        b.HasMany(x => x.RefreshTokens)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId);
    }
}
