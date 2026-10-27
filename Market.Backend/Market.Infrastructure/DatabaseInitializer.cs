using Market.Infrastructure.Database;
using Market.Infrastructure.Database.Seeders;
using Market.Shared.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Market.Infrastructure;

public static class DatabaseInitializer
{
    /// <summary>
    /// Centralized migration and seeding.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, IHostEnvironment env)
    {
        await using var scope = services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        if (env.IsIntegrationTests())
        {
            // In-memory database: no migrations, the schema is created from the model
            await ctx.Database.EnsureCreatedAsync();
            await DynamicDataSeeder.SeedAsync(ctx);
            return;
        }

        // SQL Server: applies pending migrations (same as "dotnet ef database update")
        await ctx.Database.MigrateAsync();

        // Demo data (the accounts from README) in Development and Testing, never in Production
        if (env.IsDevelopment() || env.IsTesting())
        {
            await DynamicDataSeeder.SeedAsync(ctx);
        }
    }
}
