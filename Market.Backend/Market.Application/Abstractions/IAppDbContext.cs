using Market.Domain.Entities.Finance;
using Market.Domain.Entities.Catalog;
using Market.Domain.Entities.Clinic;
using Market.Domain.Entities.Loyalty;
using Market.Domain.Entities.Patients;

namespace Market.Application.Abstractions;

// Application layer
public interface IAppDbContext
{
    DbSet<MarketUserEntity> Users { get; }
    DbSet<RefreshTokenEntity> RefreshTokens { get; }
    DbSet<PasswordResetTokenEntity> PasswordResetTokens { get; }

    DbSet<CurrencyEntity> Currencies { get; }
    DbSet<ExchangeRateEntity> ExchangeRates { get; }

    DbSet<SpecializationEntity> Specializations { get; }
    DbSet<ServiceCategoryEntity> ServiceCategories { get; }

    DbSet<DentistEntity> Dentists { get; }
    DbSet<WorkingHoursEntity> WorkingHours { get; }
    DbSet<DentalServiceEntity> DentalServices { get; }
    DbSet<LoyaltyTierEntity> LoyaltyTiers { get; }
    DbSet<LoyaltyAccountEntity> LoyaltyAccounts { get; }
    DbSet<PatientEntity> Patients { get; }
    Task<int> SaveChangesAsync(CancellationToken ct);
}
