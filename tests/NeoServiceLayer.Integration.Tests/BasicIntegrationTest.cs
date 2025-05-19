using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Api;
using Xunit;
using Xunit.Abstractions;

namespace NeoServiceLayer.Integration.Tests
{
    [Trait("Category", "Integration")]
    public class BasicIntegrationTest : IntegrationTestBase
    {
        private readonly ILogger<BasicIntegrationTest> _logger;
        private readonly ITestOutputHelper _output;

        public BasicIntegrationTest(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output)
            : base(factory)
        {
            _logger = LoggerFactory.CreateLogger<BasicIntegrationTest>();
            _output = output;
        }

        [Fact]
        public async Task HealthCheck_ReturnsOk()
        {
            // Act
            var response = await Client.GetAsync("/health");

            // Assert
            _output.WriteLine($"Response status code: {response.StatusCode}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public void SimpleTest_AlwaysPasses()
        {
            // This test always passes
            Assert.True(true);
        }
    }
}
