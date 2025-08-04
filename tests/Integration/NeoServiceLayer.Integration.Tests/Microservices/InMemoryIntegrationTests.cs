using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Notification.Models;
using Xunit;
using Xunit.Abstractions;

namespace NeoServiceLayer.Integration.Tests.Microservices
{
    /// <summary>
    /// Integration tests that can run without external infrastructure using in-memory test server
    /// </summary>
    [Collection("Integration")]
    public class InMemoryIntegrationTests : IAsyncLifetime
    {
        private readonly ITestOutputHelper _output;
        private readonly WebApplicationFactory<Program> _factory;
        private HttpClient _client = null!;

        public InMemoryIntegrationTests(ITestOutputHelper output)
        {
            _output = output;

            // Generate a unique test JWT secret key
            var testJwtSecretKey = "TestJwtSecretKeyForIntegrationTests_" + Guid.NewGuid().ToString("N");

            // Create a test server with mock services
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseTestServer();
                    builder.UseEnvironment("Test");

                    // Override JWT configuration for tests
                    Environment.SetEnvironmentVariable("JWT_SECRET_KEY", testJwtSecretKey);

                    builder.ConfigureServices(services =>
                    {
                        // Override any services needed for testing
                        services.AddLogging(logging =>
                        {
                            logging.ClearProviders();
                            logging.AddDebug();
                        });
                    });
                });
        }

        public Task InitializeAsync()
        {
            _client = _factory.CreateClient();
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            _client?.Dispose();
            _factory?.Dispose();
            return Task.CompletedTask;
        }

        [Fact]
        public async Task GatewayHealthCheck_ShouldReturnHealthy()
        {
            // Act
            var response = await _client.GetAsync("/health");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Healthy");
        }

        [Fact]
        public async Task GatewayInfo_ShouldReturnServiceDetails()
        {
            // Act
            var response = await _client.GetAsync("/api/info");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var info = await response.Content.ReadFromJsonAsync<ApiInfo>();
            info.Should().NotBeNull();
            info!.Name.Should().Be("Neo Service Layer API");
            info.Version.Should().Be("1.0.0");
        }

        [Fact]
        public async Task NotificationService_SendNotification_ShouldRequireAuthentication()
        {
            // Arrange
            var request = new SendNotificationRequest
            {
                Channel = NotificationChannel.Email,
                Recipient = "test@example.com",
                Subject = "Integration Test",
                Message = "This is a test notification",
                Priority = NotificationPriority.Normal
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/notifications/send", request);

            // Assert - should require authentication
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task HealthEndpoint_ShouldReturnHealthyStatus()
        {
            // Act
            var response = await _client.GetAsync("/health");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var health = await response.Content.ReadFromJsonAsync<HealthStatus>();
            health.Should().NotBeNull();
            // Health status can be "Healthy" or "Degraded" depending on external services
            health!.Status.Should().BeOneOf("Healthy", "Degraded");
        }

        // Note: Metrics endpoint is not currently implemented in the API
        // This test can be re-enabled when Prometheus metrics are added
        // [Fact]
        // public async Task Metrics_PrometheusEndpoint_ShouldReturnMetrics()
        // {
        //     // Act
        //     var response = await _client.GetAsync("/metrics");
        //
        //     // Assert
        //     response.StatusCode.Should().Be(HttpStatusCode.OK);
        //     var content = await response.Content.ReadAsStringAsync();
        //
        //     // Prometheus metrics format checks
        //     content.Should().Contain("# HELP");
        //     content.Should().Contain("# TYPE");
        //     content.Should().Contain("http_requests_total");
        //     content.Should().Contain("http_request_duration_seconds");
        // }

        // Helper classes
        private class ApiInfo
        {
            public string Name { get; set; } = string.Empty;
            public string Version { get; set; } = string.Empty;
            public string Environment { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
        }

        private class HealthStatus
        {
            public string Status { get; set; } = string.Empty;
        }
    }
}
