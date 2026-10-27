using Market.Application.Abstractions;
using Market.Infrastructure;
using Market.Infrastructure.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Market.Tests.ConfigurationTests.UnitTests;

/// <summary>
/// AddInfrastructure chooses the email sender by "Email:Smtp:Host". An empty host (emails only in the log)
/// is accepted only in Development, Testing and IntegrationTests; elsewhere the startup stops,
/// and it also stops without "Frontend:BaseUrl" (links in emails).
/// </summary>
public class EmailGuardUnitTests
{
    private const string SmtpHost = "smtp.example.com";
    private const string FrontendBaseUrl = "https://market.example.com";

    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("IntegrationTests")]
    public void AddInfrastructure_EmptySmtpHostInDevelopmentEnvironment_UsesLoggingEmailSender(string environmentName)
    {
        // Act
        var services = AddInfrastructure(environmentName, smtpHost: "", frontendBaseUrl: "");

        // Assert
        Assert.Equal(typeof(LoggingEmailSender), GetEmailSenderType(services));
    }

    [Fact]
    public void AddInfrastructure_SmtpHostSet_UsesSmtpEmailSender()
    {
        // Act
        var services = AddInfrastructure("Production", SmtpHost, FrontendBaseUrl);

        // Assert
        Assert.Equal(typeof(SmtpEmailSender), GetEmailSenderType(services));
    }

    [Theory]
    [InlineData("", FrontendBaseUrl)]
    [InlineData(SmtpHost, "")]
    public void AddInfrastructure_MissingEmailSettingsInProduction_StopsStartup(string smtpHost, string frontendBaseUrl)
    {
        // Act + Assert
        var exception = Assert.Throws<InvalidOperationException>(() => AddInfrastructure("Production", smtpHost, frontendBaseUrl));
        Assert.Contains("'Production'", exception.Message);
    }

    private static IServiceCollection AddInfrastructure(string environmentName, string smtpHost, string frontendBaseUrl)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:Smtp:Host"] = smtpHost,
                ["Frontend:BaseUrl"] = frontendBaseUrl
            })
            .Build();

        return new ServiceCollection().AddInfrastructure(configuration, new FakeHostEnvironment(environmentName));
    }

    private static Type? GetEmailSenderType(IServiceCollection services)
    {
        return services.Single(d => d.ServiceType == typeof(IEmailSender)).ImplementationType;
    }
}
