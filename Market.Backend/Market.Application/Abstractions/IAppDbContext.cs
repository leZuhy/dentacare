using Market.Domain.Entities.Finance;
using Market.Domain.Entities.Catalog;

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

    Task<int> SaveChangesAsync(CancellationToken ct);
}
