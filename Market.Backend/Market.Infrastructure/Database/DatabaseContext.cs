using Market.Application.Abstractions;
using Market.Domain.Entities.Finance;
using Market.Domain.Entities.Sales;

namespace Market.Infrastructure.Database;

public partial class DatabaseContext : DbContext, IAppDbContext
{
    public DbSet<ProductCategoryEntity> ProductCategories => Set<ProductCategoryEntity>();
    public DbSet<ProductEntity> Products => Set<ProductEntity>();
    public DbSet<PromotionEntity> Promotions => Set<PromotionEntity>();
    public DbSet<MarketUserEntity> Users => Set<MarketUserEntity>();
    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();
    public DbSet<PasswordResetTokenEntity> PasswordResetTokens => Set<PasswordResetTokenEntity>();

    public DbSet<OrderEntity> Orders => Set<OrderEntity>();
    public DbSet<OrderItemEntity> OrderItems => Set<OrderItemEntity>();

    public DbSet<CurrencyEntity> Currencies => Set<CurrencyEntity>();
    public DbSet<ExchangeRateEntity> ExchangeRates => Set<ExchangeRateEntity>();

    private readonly TimeProvider _clock;
    public DatabaseContext(DbContextOptions<DatabaseContext> options, TimeProvider clock) : base(options)
    {
        _clock = clock;
    }
}
