using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.ServiceDiscovery;
using NeoServiceLayer.SDK;
using Xunit;
using Xunit.Abstractions;

namespace NeoServiceLayer.Integration.Tests
{
    /// <summary>
    /// Integration tests for microservices architecture
    /// </summary>
    [Collection("Microservices")]
    public class MicroservicesIntegrationTests : IAsyncLifetime
    {
        private readonly ITestOutputHelper _output;
        private readonly HttpClient _httpClient;
        private readonly NeoServiceClient _serviceClient;
        private readonly IServiceRegistry _serviceRegistry;
        private readonly ILogger<MicroservicesIntegrationTests> _logger;

        public MicroservicesIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
            
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddXUnit(output));
            services.AddSingleton<IServiceRegistry, ConsulServiceRegistry>();
            
            var serviceProvider = services.BuildServiceProvider();
            _logger = serviceProvider.GetRequiredService<ILogger<MicroservicesIntegrationTests>>();
            _serviceRegistry = serviceProvider.GetRequiredService<IServiceRegistry>();
            
            _serviceClient = new NeoServiceClient(_httpClient, _serviceRegistry, _logger);
        }

        public async Task InitializeAsync()
        {
            // Wait for services to be ready
            await WaitForServicesAsync(TimeSpan.FromMinutes(2));
        }

        public Task DisposeAsync()
        {
            _httpClient?.Dispose();
            return Task.CompletedTask;
        }

        [Fact]
        public async Task ApiGateway_ShouldBeAccessible()
        {
            // Act
            var response = await _httpClient.GetAsync("/health");
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Healthy");
        }

        [Fact]
        public async Task ServiceDiscovery_ShouldDiscoverAllServices()
        {
            // Act
            var services = await _serviceRegistry.GetAllServicesAsync();
            
            // Assert
            services.Should().NotBeEmpty();
            services.Should().Contain(s => s.ServiceType == "Notification");
            services.Should().Contain(s => s.ServiceType == "Configuration");
            services.Should().Contain(s => s.ServiceType == "SmartContracts");
            
            _output.WriteLine($"Discovered {services.Count()} services:");
            foreach (var service in services)
            {
                _output.WriteLine($"- {service.ServiceName} ({service.ServiceType}) at {service.HostName}:{service.Port} - {service.Status}");
            }
        }

        [Fact]
        public async Task NotificationService_ShouldSendNotification()
        {
            // Arrange
            var notificationService = _serviceClient.GetService<INotificationService>();
            var request = new
            {
                recipient = "test@example.com",
                subject = "Integration Test",
                message = "This is a test notification",
                channel = "Email"
            };
            
            // Act
            var result = await notificationService.PostAsync<dynamic>(
                "/api/notification/send", 
                request);
            
            // Assert
            result.Should().NotBeNull();
            result.success.Should().BeTrue();
            result.notificationId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task ServiceCommunication_ShouldWorkBetweenServices()
        {
            // This tests that services can discover and communicate with each other
            
            // First, set a configuration value
            var configService = _serviceClient.GetService<IConfigurationService>();
            var configKey = $"test-key-{Guid.NewGuid()}";
            var configValue = "test-value";
            
            await configService.PostAsync<bool>(
                $"/api/configuration/integration-test/{configKey}",
                new { value = configValue });
            
            // Then, verify it can be read back
            var retrieved = await configService.GetAsync<dynamic>(
                $"/api/configuration/integration-test/{configKey}");
            
            retrieved.Should().NotBeNull();
            retrieved.value.Should().Be(configValue);
        }

        [Fact]
        public async Task LoadBalancing_ShouldDistributeRequests()
        {
            // Make multiple requests and verify they're distributed across instances
            var responses = new List<string>();
            
            for (int i = 0; i < 10; i++)
            {
                var response = await _httpClient.GetAsync("/api/gateway/services/Notification");
                var services = await response.Content.ReadFromJsonAsync<List<ServiceInfo>>();
                
                if (services?.Any() == true)
                {
                    // Collect instance IDs
                    responses.AddRange(services.Select(s => s.ServiceId));
                }
            }
            
            // Should have hit multiple instances
            responses.Distinct().Count().Should().BeGreaterThan(1);
        }

        [Fact]
        public async Task CircuitBreaker_ShouldProtectAgainstFailures()
        {
            // Test that circuit breaker opens after consecutive failures
            var failingService = _serviceClient.GetService<ITestService>();
            
            var exceptions = new List<Exception>();
            
            // Make requests that will fail
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    await failingService.GetAsync<object>("/api/test/fail");
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
            
            // Circuit breaker should have opened
            exceptions.Should().Contain(ex => ex.Message.Contains("circuit breaker"));
        }

        [Fact]
        public async Task HealthChecks_AllServicesShouldBeHealthy()
        {
            // Get health status from API Gateway
            var response = await _httpClient.GetAsync("/api/gateway/health");
            response.EnsureSuccessStatusCode();
            
            var health = await response.Content.ReadFromJsonAsync<HealthSummary>();
            
            // Assert
            health.Should().NotBeNull();
            health.TotalServices.Should().BeGreaterThan(0);
            health.UnhealthyServices.Should().Be(0);
            
            _output.WriteLine($"Health Summary: {health.HealthyServices}/{health.TotalServices} services healthy");
            
            foreach (var service in health.Services)
            {
                _output.WriteLine($"- {service.Type}: {service.Healthy}/{service.Total} healthy");
            }
        }

        [Theory]
        [InlineData("Notification", "/api/notification/metrics")]
        [InlineData("Configuration", "/api/configuration/metrics")]
        [InlineData("SmartContracts", "/api/smart-contracts/metrics")]
        public async Task Metrics_ShouldBeExposed(string serviceType, string metricsPath)
        {
            // Discover service
            var services = await _serviceRegistry.DiscoverServicesAsync(serviceType);
            var service = services.FirstOrDefault(s => s.Status == ServiceStatus.Healthy);
            
            if (service != null)
            {
                // Get metrics
                var response = await _httpClient.GetAsync($"http://{service.HostName}:{service.Port}{metricsPath}");
                
                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                var content = await response.Content.ReadAsStringAsync();
                content.Should().Contain("_total");  // Prometheus format
            }
        }

        [Fact]
        public async Task ServiceRegistration_NewServiceShouldAutoRegister()
        {
            // This would test that a new service automatically registers itself
            // In a real test, you would spin up a test service container
            
            var testServiceInfo = new ServiceInfo
            {
                ServiceName = "TestService",
                ServiceType = "Test",
                HostName = "test-service",
                Port = 8080,
                Status = ServiceStatus.Healthy
            };
            
            // Register
            var registered = await _serviceRegistry.RegisterServiceAsync(testServiceInfo);
            registered.Should().BeTrue();
            
            // Verify it can be discovered
            var services = await _serviceRegistry.DiscoverServicesAsync("Test");
            services.Should().Contain(s => s.ServiceId == testServiceInfo.ServiceId);
            
            // Cleanup
            await _serviceRegistry.DeregisterServiceAsync(testServiceInfo.ServiceId);
        }

        private async Task WaitForServicesAsync(TimeSpan timeout)
        {
            var startTime = DateTime.UtcNow;
            var requiredServices = new[] { "Notification", "Configuration", "SmartContracts" };
            
            while (DateTime.UtcNow - startTime < timeout)
            {
                try
                {
                    var services = await _serviceRegistry.GetAllServicesAsync();
                    var healthyServices = services.Where(s => s.Status == ServiceStatus.Healthy).ToList();
                    
                    var allServicesReady = requiredServices.All(required => 
                        healthyServices.Any(s => s.ServiceType == required));
                    
                    if (allServicesReady)
                    {
                        _logger.LogInformation("All required services are ready");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error checking service status");
                }
                
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
            
            throw new TimeoutException($"Services did not become ready within {timeout}");
        }

        // Helper classes
        private interface INotificationService { }
        private interface IConfigurationService { }
        private interface ITestService { }
        
        private class HealthSummary
        {
            public int TotalServices { get; set; }
            public int HealthyServices { get; set; }
            public int UnhealthyServices { get; set; }
            public List<ServiceHealthInfo> Services { get; set; } = new();
        }
        
        private class ServiceHealthInfo
        {
            public string Type { get; set; } = string.Empty;
            public int Total { get; set; }
            public int Healthy { get; set; }
        }
    }
}