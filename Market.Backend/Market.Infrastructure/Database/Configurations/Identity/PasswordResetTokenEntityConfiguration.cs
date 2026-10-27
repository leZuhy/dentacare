namespace Market.Infrastructure.Database.Configurations.Identity;

public sealed class PasswordResetTokenEntityConfiguration : IEntityTypeConfiguration<PasswordResetTokenEntity>
{
    public void Configure(EntityTypeBuilder<PasswordResetTokenEntity> b)
    {
        b.ToTable("PasswordResetTokens");

        b.HasKey(x => x.Id);

        // The token is looked up by its hash (reset-password).
        b.HasIndex(x => x.TokenHash)
            .IsUnique();

        b.Property(x => x.TokenHash)
            .IsRequired()
            .HasMaxLength(PasswordResetTokenEntity.Constraints.TokenHashMaxLength);

        b.Property(x => x.ExpiresAtUtc)
            .IsRequired();

        // No navigation on the user side ("WithMany()"): the user does not need a list of its reset tokens.
        // Cascade — a hard DELETE of a user also deletes its reset tokens (same as refresh tokens).
        b.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
