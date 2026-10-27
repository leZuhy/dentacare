using Market.API.Filters;
using Market.Application.Abstractions;
using Market.Infrastructure.Common;
using Market.Shared.Constants;
using Market.Shared.Dtos;
using Market.Shared.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;

namespace Market.API;

public static class DependencyInjection
{
    /// <summary>
    /// Prefix of the JWT key from appsettings.Development.json. The key is public (it is in the repository),
    /// so it is allowed only in Development and IntegrationTests.
    /// </summary>
    private const string DevOnlyJwtKeyPrefix = "DEV_ONLY";

    /// <summary>
    /// Prefix of the JWT key from appsettings.Testing.json. The key is public (it is in the repository),
    /// so it is allowed only in Testing.
    /// </summary>
    private const string TestingOnlyJwtKeyPrefix = "TESTING_ONLY_";

    private const string SwaggerBearerSchemeId = "Bearer";

    public static IServiceCollection AddAPI(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment env)
    {
        EnsureJwtKeyIsAllowed(configuration, env);

        // Controllers + 204 for actions without a return value + uniform BadRequest (same ErrorDto shape as FluentValidation errors)
        services.AddControllers(options => options.Filters.Add<EmptyResultToNoContentFilter>())
            .ConfigureApiBehaviorOptions(opts =>
            {
                opts.InvalidModelStateResponseFactory = ctx =>
                {
                    var errors = ctx.ModelState
                        .Where(kv => kv.Value is not null && kv.Value.Errors.Count > 0)
                        .SelectMany(kv => kv.Value!.Errors.Select(e => new FieldErrorDto
                        {
                            Field = FieldErrorDto.ToCamelCasePath(kv.Key),
                            Message = string.IsNullOrWhiteSpace(e.ErrorMessage)
                                ? "Neispravna vrijednost."
                                : e.ErrorMessage
                        }))
                        .ToList();

                    return new BadRequestObjectResult(new ErrorDto
                    {
                        Code = "validation.error",
                        Message = string.Join(" ", errors.Select(e => e.Message).Distinct()),
                        TraceId = Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier,
                        Errors = errors
                    });
                };
            });

        // Typed options + validation on startup
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // JWT auth
        services.AddAuthentication(o =>
        {
            o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer((o) =>
        {
            var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;

            o.TokenValidationParameters = new()
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            o.Events = new JwtBearerEvents
            {
                OnTokenValidated = ValidateUserAndTokenVersionAsync
            };
        });

        services.AddAuthorization(o =>
        {
            // Every endpoint requires a logged-in user, unless it has [AllowAnonymous]
            o.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            // [Authorize(Policy = PolicyNames.Admin)] → 401 without a token, 403 for a non-admin user
            o.AddPolicy(PolicyNames.Admin, policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim("is_admin", "true"));
        });

        // Swagger with Bearer auth
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Market API", Version = "v1" });
            var xml = Path.Combine(AppContext.BaseDirectory, "Market.API.xml");
            if (File.Exists(xml))
                c.IncludeXmlComments(xml, includeControllerXmlComments: true);

            c.AddSecurityDefinition(SwaggerBearerSchemeId, new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Unesi JWT token. Format: **Bearer {token}**",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            // Every endpoint shows the lock icon; Swagger UI sends the token from "Authorize" with each request
            c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(SwaggerBearerSchemeId, document)] = []
            });
        });

        services.AddExceptionHandler<MarketExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }

    /// <summary>
    /// Runs after the JWT signature and lifetime are valid. Rejects the token when the user
    /// no longer exists, is disabled, or the token's "ver" claim differs from the user's TokenVersion
    /// (TokenVersion is incremented e.g. when refresh token reuse is detected → all old access tokens stop working).
    /// One small AsNoTracking query per authenticated request.
    /// </summary>
    private static async Task ValidateUserAndTokenVersionAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;

        if (!int.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ||
            !int.TryParse(principal?.FindFirstValue("ver"), out var tokenVersion))
        {
            context.Fail("Token ne sadrži ispravne podatke o korisniku.");
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<IAppDbContext>();

        var user = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.TokenVersion, u.IsEnabled, u.IsDeleted })
            .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

        if (user is null || !user.IsEnabled || user.IsDeleted || user.TokenVersion != tokenVersion)
        {
            context.Fail("Token više nije važeći.");
        }
    }

    /// <summary>
    /// Stops the startup (before the database is touched) when Jwt:Key is missing, or when it is one of the
    /// public keys from the repository and the environment is not the one the key is meant for:
    /// <list type="bullet">
    /// <item>DEV_ONLY… (appsettings.Development.json) only in Development and IntegrationTests,</item>
    /// <item>TESTING_ONLY_… (appsettings.Testing.json) only in Testing.</item>
    /// </list>
    /// Everywhere else (Production, Staging...) a real secret must be configured
    /// (environment variable Jwt__Key, user secrets, key vault...).
    /// </summary>
    private static void EnsureJwtKeyIsAllowed(IConfiguration configuration, IHostEnvironment env)
    {
        var key = configuration[$"{JwtOptions.SectionName}:{nameof(JwtOptions.Key)}"];

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException(
                $"Jwt:Key nije postavljen u okruženju '{env.EnvironmentName}'. " +
                $"Postavite tajni ključ od najmanje 32 znaka (npr. varijabla okruženja Jwt__Key). / " +
                $"Jwt:Key is not set in the '{env.EnvironmentName}' environment. " +
                $"Configure a secret key of at least 32 characters (e.g. environment variable Jwt__Key).");
        }

        if (key.StartsWith(DevOnlyJwtKeyPrefix, StringComparison.Ordinal) && !(env.IsDevelopment() || env.IsIntegrationTests()))
            throw PublicJwtKeyNotAllowed(DevOnlyJwtKeyPrefix, env);

        if (key.StartsWith(TestingOnlyJwtKeyPrefix, StringComparison.Ordinal) && !env.IsTesting())
            throw PublicJwtKeyNotAllowed(TestingOnlyJwtKeyPrefix, env);
    }

    private static InvalidOperationException PublicJwtKeyNotAllowed(string prefix, IHostEnvironment env) => new(
        $"Jwt:Key je javni ključ iz repozitorija ({prefix}...) i ne smije se koristiti u okruženju '{env.EnvironmentName}'. " +
        $"Postavite pravi tajni ključ (npr. varijabla okruženja Jwt__Key). / " +
        $"Jwt:Key is a public key from the repository ({prefix}...) and must not be used in the '{env.EnvironmentName}' environment. " +
        $"Configure a real secret (e.g. environment variable Jwt__Key).");
}
