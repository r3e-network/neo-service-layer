using System.Net.Http;
using System.Threading.Tasks;
using NeoServiceLayer.Api.Models;
using NeoServiceLayer.Shared.Models;
using NeoServiceLayer.Integration.Tests.Models;
using Xunit;

namespace NeoServiceLayer.Integration.Tests.Controllers
{
    public class HealthControllerTests : IntegrationTestBase
    {
        public HealthControllerTests(CustomWebApplicationFactory<Api.Program> factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task GetHealth_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await Client.GetAsync("/api/health");

            // Assert
            response.EnsureSuccessStatusCode();
            var apiResponse = await DeserializeResponse<ApiResponse<HealthStatus>>(response);
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data);
            Assert.Equal("healthy", apiResponse.Data.Status);
            Assert.NotNull(apiResponse.Data.Version);
            Assert.NotNull(apiResponse.Data.Timestamp);
        }

        [Fact]
        public async Task GetDetailedHealth_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await Client.GetAsync("/api/health/details");

            // Assert
            response.EnsureSuccessStatusCode();
            var apiResponse = await DeserializeResponse<ApiResponse<DetailedHealthStatus>>(response);
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data);
            Assert.Equal("healthy", apiResponse.Data.Status);
            Assert.NotNull(apiResponse.Data.Version);
            Assert.NotNull(apiResponse.Data.Timestamp);
            Assert.NotNull(apiResponse.Data.Components);
            Assert.True(apiResponse.Data.Components.ContainsKey("api"));
            Assert.Equal("healthy", apiResponse.Data.Components["api"].Status);
        }
    }
}
