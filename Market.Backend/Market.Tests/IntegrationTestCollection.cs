namespace Market.Tests;

/// <summary>
/// All integration test classes share one API instance (and its InMemory database) and run one after another.
/// Without it, xUnit would start one API per class in parallel, and their seeders would race on the same database.
/// Usage: <c>[Collection(IntegrationTestCollection.Name)]</c> on the test class.
/// </summary>
[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory<Program>>
{
    public const string Name = "Integration tests";
}
