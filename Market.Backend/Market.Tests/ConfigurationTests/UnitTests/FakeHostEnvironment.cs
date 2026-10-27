using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Market.Tests.ConfigurationTests.UnitTests;

/// <summary>
/// Minimal <see cref="IHostEnvironment"/> for the startup checks in AddAPI and AddInfrastructure.
/// </summary>
internal sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = environmentName;
    public string ApplicationName { get; set; } = "Market.Tests";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
