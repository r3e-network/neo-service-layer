using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.ServiceDiscovery;
using NeoServiceLayer.SDK;
using NeoServiceLayer.Services.Notification.Models;
using Xunit;
using Xunit.Abstractions;

namespace NeoServiceLayer.Integration.Tests.Microservices
{
    /// <summary>
    /// Integration tests for microservices architecture
    /// </summary>
    [Collection("Integration")]
    public class MicroservicesIntegrationTests : IAsyncLifetime
    {
        private readonly ITestOutputHelper _output;
        private readonly HttpClient _httpClient;
        private readonly ILogger<MicroservicesIntegrationTests> _logger;
        private readonly List<IDisposable> _disposables = new();

        public MicroservicesIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:7000") };

            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());

            var serviceProvider = services.BuildServiceProvider();
            _logger = serviceProvider.GetRequiredService<ILogger<MicroservicesIntegrationTests>>();
        }

        public Task InitializeAsync()
        {
            _logger.LogInformation("Initializing microservices integration tests");
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            _httpClient?.Dispose();
            foreach (var disposable in _disposables)
            {
                disposable?.Dispose();
            }
            return Task.CompletedTask;
        }

        [Fact(Skip = "Requires running microservices infrastructure")]
        public async Task GatewayHealthCheck_ShouldReturnHealthy()
        {
            // Act
            var response = await _httpClient.GetAsync("/health");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Healthy");
        }

        [Fact(Skip = "Requires running microservices infrastructure")]
        public async Task GatewayInfo_ShouldReturnServiceDetails()
        {
            // Act
            var response = await _httpClient.GetAsync("/");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var info = await response.Content.ReadFromJsonAsync<GatewayInfo>();
            info.Should().NotBeNull();
            info.Service.Should().Be("Neo Service Layer API Gateway");
            info.Status.Should().Be("running");
            info.RateLimiting.Should().NotBeNull();
        }

        [Fact(Skip = "Requires running microservices infrastructure")]
        public async Task NotificationService_SendNotification_ShouldSucceed()
        {
            // Arrange
            var request = new SendNotificationRequest
            {
                Channel = NotificationChannel.Email,
                Recipient = "test@example.com",
                Subject = "Integration Test",
                Message = "This is a test notification from integration tests",
                Priority = NotificationPriority.Normal
            };

            // Act
            var response = await _httpClient.PostAsJsonAsync("/api/notification/send", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<NotificationResult>();
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Status.Should().Be(DeliveryStatus.Delivered);
        }

        [Fact(Skip = "Requires running microservices infrastructure")]
        public async Task RateLimiting_ShouldEnforceLimit()
        {
            // Arrange
            var tasks = new List<Task<HttpResponseMessage>>();

            // Act - Send 150 requests rapidly (exceeding the 100/minute limit)
            for (int i = 0; i < 150; i++)
            {
                tasks.Add(_httpClient.GetAsync("/api/health/status"));
            }

            var responses = await Task.WhenAll(tasks);

            // Assert
            var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
            var rateLimitedCount = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);

            rateLimitedCount.Should().BeGreaterThan(0, "Some requests should be rate limited");
            _logger.LogInformation($"Success: {successCount}, Rate Limited: {rateLimitedCount}");
        }

        [Fact(Skip = "Requires running microservices infrastructure")]
        public async Task ServiceDiscovery_ShouldDiscoverRunningServices()
        {
            // Act
            var response = await _httpClient.GetAsync("/api/discovery/services");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var services = await response.Content.ReadFromJsonAsync<List<ServiceInfo>>();
            services.Should().NotBeNull();
            services.Should().NotBeEmpty();

            // Should have at least notification and health services
            services.Should().Contain(s => s.ServiceType.Contains("notification", StringComparison.OrdinalIgnoreCase));
            services.Should().Contain(s => s.ServiceType.Contains("health", StringComparison.OrdinalIgnoreCase));
        }

        [Fact(Skip = "Requires running microservices infrastructure")]
        public async Task Authentication_UnauthorizedRequest_ShouldReturn401()
        {
            // Act
            var response = await _httpClient.GetAsync("/api/keymanagement/keys");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact(Skip = "Requires running microservices infrastructure")]
        public async Task Authentication_WithValidToken_ShouldSucceed()
        {
            // Arrange - First get a token
            var authRequest = new
            {
                Username = "testuser",
                Password = "testpass123"
            };

            var authResponse = await _httpClient.PostAsJsonAsync("/api/auth/login", authRequest);
            authResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var authResult = await authResponse.Content.ReadFromJsonAsync<AuthResult>();
            authResult.Should().NotBeNull();
            authResult.Token.Should().NotBeNullOrEmpty();

            // Act - Use the token
            using var authenticatedClient = new HttpClient { BaseAddress = new Uri("http://localhost:7000") };
            authenticatedClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResult.Token);

            var response = await authenticatedClient.GetAsync("/api/keymanagement/keys");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact(Skip = "Requires running microservices infrastructure")]
        public async Task CircuitBreaker_ShouldOpenAfterFailures()
        {
            // This test would simulate a failing service
            // The circuit breaker should open after 5 consecutive failures

            // Arrange - Point to a non-existent service endpoint
            var tasks = new List<Task<HttpResponseMessage>>();

            // Act - Send 10 requests to trigger circuit breaker
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(_httpClient.GetAsync("/api/failing-service/test"));
            }

            var responses = await Task.WhenAll(tasks);

            // Assert - After 5 failures, circuit should open and return 503
            var serviceUnavailableCount = responses.Count(r => r.StatusCode == HttpStatusCode.ServiceUnavailable);
            serviceUnavailableCount.Should().BeGreaterThan(0, "Circuit breaker should open after failures");
        }

        [Fact(Skip = "Requires running microservices infrastructure")]
        public async Task Metrics_PrometheusEndpoint_ShouldReturnMetrics()
        {
            // Act
            var response = await _httpClient.GetAsync("/metrics");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();

            // Prometheus metrics format checks
            content.Should().Contain("# HELP");
            content.Should().Contain("# TYPE");
            content.Should().Contain("http_requests_total");
            content.Should().Contain("http_request_duration_seconds");
        }

        [Fact(Skip = "Requires running microservices infrastructure")]
        public async Task MultiService_Orchestration_ShouldWork()
        {
            // This test simulates a real-world scenario involving multiple services

            // Step 1: Create a configuration
            var configRequest = new
            {
                Key = "integration-test-config",
                Value = "test-value",
                Environment = "test"
            };

            var configResponse = await _httpClient.PostAsJsonAsync("/api/configuration/settings", configRequest);
            configResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Step 2: Send a notification about the configuration
            var notificationRequest = new SendNotificationRequest
            {
                Channel = NotificationChannel.InApp,
                Recipient = "admin",
                Subject = "Configuration Updated",
                Message = $"Configuration {configRequest.Key} has been updated",
                Priority = NotificationPriority.High,
                Metadata = new Dictionary<string, object>
                {
                    ["config_key"] = configRequest.Key,
                    ["timestamp"] = DateTime.UtcNow
                }
            };

            var notificationResponse = await _httpClient.PostAsJsonAsync("/api/notification/send", notificationRequest);
            notificationResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Step 3: Check health of both services
            var healthResponse = await _httpClient.GetAsync("/api/health/services/status");
            healthResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var healthStatus = await healthResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            healthStatus.Should().ContainKey("configuration");
            healthStatus.Should().ContainKey("notification");
            healthStatus["configuration"].Should().Be("Healthy");
            healthStatus["notification"].Should().Be("Healthy");
        }

        // Helper classes for JSON deserialization
        private class GatewayInfo
        {
            public string Service { get; set; } = string.Empty;
            public string Version { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string[] Endpoints { get; set; } = Array.Empty<string>();
            public RateLimitingInfo RateLimiting { get; set; } = new();
        }

        private class RateLimitingInfo
        {
            public string GlobalLimit { get; set; } = string.Empty;
            public string AuthEndpoints { get; set; } = string.Empty;
            public string HealthEndpoints { get; set; } = string.Empty;
        }

        private class ServiceInfo
        {
            public string ServiceType { get; set; } = string.Empty;
            public string ServiceName { get; set; } = string.Empty;
            public string HostName { get; set; } = string.Empty;
            public int Port { get; set; }
        }

        private class AuthResult
        {
            public string Token { get; set; } = string.Empty;
            public DateTime ExpiresAt { get; set; }
        }
    }
}
