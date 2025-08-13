using Xunit;

namespace NeoServiceLayer.Integration.Tests.Helpers;

// Removed ICollectionFixture to avoid sharing the factory between tests
// Each test class will now create its own factory instance for better isolation
[CollectionDefinition("Integration Tests")]
public class IntegrationTestCollection
{
}
