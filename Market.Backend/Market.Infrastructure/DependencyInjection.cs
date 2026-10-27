using Market.Application.Abstractions;
using Market.Application.Abstractions.Caching;
using Market.Infrastructure.Caching;
using Market.Infrastructure.Common;
using Market.Infrastructure.Database;
using Market.Infrastructure.Email;
using Market.Shared.Constants;
using Market.Shared.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Market.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment env)
    {
        // Typed ConnectionStrings + validation
        services.AddOptions<ConnectionStringsOptions>()
            .Bind(configuration.GetSection(ConnectionStringsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // DbContext: InMemory for automated integration tests; SQL Server otherwise
        services.AddDbContext<DatabaseContext>((sp, options) =>
        {
            if (env.IsIntegrationTests())
            {
                options.UseInMemoryDatabase("IntegrationTestsDb");

                return;
            }

            var cs = sp.GetRequiredService<IOptions<ConnectionStringsOptions>>().Value.Main;
            options.UseSqlServer(cs);
        });

        // IAppDbContext mapping
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<DatabaseContext>());

        // Identity hasher
        services.AddScoped<IPasswordHasher<MarketUserEntity>, PasswordHasher<MarketUserEntity>>();

        // Token service (reads JwtOptions via IOptions<JwtOptions>)
        services.AddTransient<IJwtTokenService, JwtTokenService>();

        // HttpContext accessor + current user
        services.AddHttpContextAccessor();
        services.AddScoped<IAppCurrentUser, AppCurrentUser>();

        // TimeProvider (if used in handlers/services)
        services.AddSingleton<TimeProvider>(TimeProvider.System);

        // Currency conversion (reads rates from the database)
        services.AddScoped<ICurrencyConverter, CurrencyConverter>();

        // Uploaded files (product images) in wwwroot/uploads
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        AddCaching(services, configuration);
        AddExchangeRateProvider(services, configuration);
        AddEmail(services, configuration, env);

        return services;
    }

    /// <summary>
    /// Emails ("Email:Smtp" section) and links in them ("Frontend:BaseUrl").
    /// <list type="bullet">
    /// <item>Email:Smtp:Host set → <see cref="SmtpEmailSender"/> (real emails).</item>
    /// <item>Email:Smtp:Host empty → <see cref="LoggingEmailSender"/>: emails are only written to the log.
    /// Allowed only in Development, Testing and IntegrationTests (see <see cref="EnsureEmailIsConfigured"/>).</item>
    /// </list>
    /// </summary>
    private static void AddEmail(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
    {
        EnsureEmailIsConfigured(configuration, env);

        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<FrontendOptions>()
            .Bind(configuration.GetSection(FrontendOptions.SectionName));

        var smtpHost = configuration[$"{EmailOptions.SectionName}:{nameof(EmailOptions.Host)}"];

        if (string.IsNullOrWhiteSpace(smtpHost))
            services.AddSingleton<IEmailSender, LoggingEmailSender>();
        else
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
    }

    /// <summary>
    /// Stops the startup (before the database is touched), like the Jwt:Key check in Market.API, when the application
    /// runs outside Development, Testing and IntegrationTests and:
    /// <list type="bullet">
    /// <item>Email:Smtp:Host is empty — emails (with password reset links) would end up in the log instead of the inbox,</item>
    /// <item>Frontend:BaseUrl is empty — links in emails would not point to the application.</item>
    /// </list>
    /// </summary>
    private static void EnsureEmailIsConfigured(IConfiguration configuration, IHostEnvironment env)
    {
        if (env.IsDevelopment() || env.IsTesting() || env.IsIntegrationTests())
            return;

        if (string.IsNullOrWhiteSpace(configuration[$"{EmailOptions.SectionName}:{nameof(EmailOptions.Host)}"]))
        {
            throw new InvalidOperationException(
                $"Email:Smtp:Host nije postavljen u okruženju '{env.EnvironmentName}'. " +
                $"Postavite SMTP server (npr. varijabla okruženja Email__Smtp__Host); bez njega se emailovi samo ispisuju u log. / " +
                $"Email:Smtp:Host is not set in the '{env.EnvironmentName}' environment. " +
                $"Configure an SMTP server (e.g. environment variable Email__Smtp__Host); without it emails are only written to the log.");
        }

        if (string.IsNullOrWhiteSpace(configuration[$"{FrontendOptions.SectionName}:{nameof(FrontendOptions.BaseUrl)}"]))
        {
            throw new InvalidOperationException(
                $"Frontend:BaseUrl nije postavljen u okruženju '{env.EnvironmentName}'. " +
                $"Postavite adresu frontenda (npr. varijabla okruženja Frontend__BaseUrl) za linkove u emailovima. / " +
                $"Frontend:BaseUrl is not set in the '{env.EnvironmentName}' environment. " +
                $"Configure the frontend address (e.g. environment variable Frontend__BaseUrl) for links in emails.");
        }
    }

    /// <summary>
    /// Distributed cache: Redis when "ConnectionStrings:Redis" is set, otherwise memory.
    /// ICacheService works the same way on both, so the application starts without a Redis server.
    /// Nothing in the starter uses the cache yet (see ICacheService for an example).
    /// </summary>
    private static void AddCaching(IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString(nameof(ConnectionStringsOptions.Redis));

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            // Connects lazily (on the first cache call), not at startup.
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "Market:";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddSingleton<ICacheService, CacheService>();
        services.AddSingleton<ICatalogCacheVersionService, CatalogCacheVersionService>();
    }

    /// <summary>
    /// Optional download of exchange rates ("ExchangeRates" section). The typed HttpClient is only configured here;
    /// the network is used only when an administrator calls POST /Currencies/rates/refresh with Provider = "Frankfurter".
    /// </summary>
    private static void AddExchangeRateProvider(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ExchangeRatesOptions>()
            .Bind(configuration.GetSection(ExchangeRatesOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IExchangeRateProvider, FrankfurterExchangeRateProvider>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<ExchangeRatesOptions>>().Value;
            client.BaseAddress = new Uri(options.FrankfurterBaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });
    }
}
