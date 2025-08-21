using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Health;
using Xunit;
using Xunit.Abstractions;

namespace NeoServiceLayer.Integration.Tests.HealthChecks
{
    /// <summary>
    /// Integration tests for health check functionality
    /// </summary>
    public class HealthCheckIntegrationTests
    {
        private readonly ITestOutputHelper _output;

        public HealthCheckIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task HealthCheckOrchestrator_WithMultipleChecks_ShouldRunAllChecks()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddXUnit(_output));
            services.AddScoped<IHealthCheck, TestHealthCheck>();
            services.AddScoped<HealthCheckOrchestrator>();

            var serviceProvider = services.BuildServiceProvider();
            var orchestrator = serviceProvider.GetRequiredService<HealthCheckOrchestrator>();

            // Act
            var result = await orchestrator.CheckHealthAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsHealthy);
            Assert.Equal(HealthStatus.Healthy, result.Status);
            Assert.Single(result.Results);
            Assert.True(result.Results.ContainsKey("Test"));
        }

        [Fact]
        public async Task HealthCheckOrchestrator_WithFailingCheck_ShouldReturnUnhealthy()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddXUnit(_output));
            services.AddScoped<IHealthCheck, FailingHealthCheck>();
            services.AddScoped<HealthCheckOrchestrator>();

            var serviceProvider = services.BuildServiceProvider();
            var orchestrator = serviceProvider.GetRequiredService<HealthCheckOrchestrator>();

            // Act
            var result = await orchestrator.CheckHealthAsync();

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsHealthy);
            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Single(result.Results);
            Assert.True(result.Results.ContainsKey("Failing"));
        }

        [Fact]
        public async Task HealthCheckOrchestrator_WithSpecificCheckName_ShouldRunOnlyThatCheck()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddXUnit(_output));
            services.AddScoped<IHealthCheck, TestHealthCheck>();
            services.AddScoped<IHealthCheck, AnotherTestHealthCheck>();
            services.AddScoped<HealthCheckOrchestrator>();

            var serviceProvider = services.BuildServiceProvider();
            var orchestrator = serviceProvider.GetRequiredService<HealthCheckOrchestrator>();

            // Act
            var result = await orchestrator.CheckHealthAsync("Test");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(HealthStatus.Healthy, result.Status);
        }

        [Fact]
        public async Task HealthCheckOrchestrator_WithNonExistentCheckName_ShouldReturnNull()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddXUnit(_output));
            services.AddScoped<HealthCheckOrchestrator>();

            var serviceProvider = services.BuildServiceProvider();
            var orchestrator = serviceProvider.GetRequiredService<HealthCheckOrchestrator>();

            // Act
            var result = await orchestrator.CheckHealthAsync("NonExistent");

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Test health check that always succeeds
        /// </summary>
        private class TestHealthCheck : IHealthCheck
        {
            public string Name => "Test";

            public Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(HealthCheckResult.Healthy("Test health check passed"));
            }
        }

        /// <summary>
        /// Another test health check that always succeeds
        /// </summary>
        private class AnotherTestHealthCheck : IHealthCheck
        {
            public string Name => "AnotherTest";

            public Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(HealthCheckResult.Healthy("Another test health check passed"));
            }
        }

        /// <summary>
        /// Test health check that always fails
        /// </summary>
        private class FailingHealthCheck : IHealthCheck
        {
            public string Name => "Failing";

            public Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Test failure", 
                    new InvalidOperationException("Simulated failure")));
            }
        }
    }
}