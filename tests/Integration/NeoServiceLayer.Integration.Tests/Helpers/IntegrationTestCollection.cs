using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Integration.Tests.Helpers;

// Removed ICollectionFixture to avoid sharing the factory between tests
// Each test class will now create its own factory instance for better isolation
// [Collection("Integration Tests")] // XUnit attribute not available
public class IntegrationTestCollection
{
}
