using Market.Application.Abstractions;
using Market.Domain.Entities.Appointments;
using Market.Domain.Entities.Catalog;
using Market.Domain.Entities.Clinic;
using Market.Domain.Entities.Finance;
using Market.Domain.Entities.Loyalty;
using Market.Domain.Entities.Patients;

namespace Market.Infrastructure.Database;

public partial class DatabaseContext : DbContext, IAppDbContext
{
    public DbSet<MarketUserEntity> Users => Set<MarketUserEntity>();
    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();
    public DbSet<PasswordResetTokenEntity> PasswordResetTokens => Set<PasswordResetTokenEntity>();

    public DbSet<CurrencyEntity> Currencies => Set<CurrencyEntity>();
    public DbSet<ExchangeRateEntity> ExchangeRates => Set<ExchangeRateEntity>();
    public DbSet<SpecializationEntity> Specializations => Set<SpecializationEntity>();
    public DbSet<ServiceCategoryEntity> ServiceCategories => Set<ServiceCategoryEntity>();
    public DbSet<DentistEntity> Dentists => Set<DentistEntity>();
    public DbSet<WorkingHoursEntity> WorkingHours => Set<WorkingHoursEntity>();
    public DbSet<DentalServiceEntity> DentalServices => Set<DentalServiceEntity>();
    public DbSet<LoyaltyTierEntity> LoyaltyTiers => Set<LoyaltyTierEntity>();
    public DbSet<LoyaltyAccountEntity> LoyaltyAccounts => Set<LoyaltyAccountEntity>();
    public DbSet<PatientEntity> Patients => Set<PatientEntity>();
    public DbSet<AppointmentEntity> Appointments => Set<AppointmentEntity>();
    public DbSet<AppointmentServiceEntity> AppointmentServices => Set<AppointmentServiceEntity>();

    private readonly TimeProvider _clock;
    public DatabaseContext(DbContextOptions<DatabaseContext> options, TimeProvider clock) : base(options)
    {
        _clock = clock;
    }
}
