using Market.Application.Abstractions;
using Market.Domain.Entities.Finance;
using Market.Domain.Entities.Catalog;
using Market.Domain.Entities.Clinic;

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

    private readonly TimeProvider _clock;
    public DatabaseContext(DbContextOptions<DatabaseContext> options, TimeProvider clock) : base(options)
    {
        _clock = clock;
    }
}
