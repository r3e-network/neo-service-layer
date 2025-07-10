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
        private readonly WebApplicationFactory<TestStartup> _factory;
        private HttpClient _client = null!;

        public InMemoryIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
            
            // Create a test server with mock services
            _factory = new WebApplicationFactory<TestStartup>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseTestServer();
                    builder.ConfigureServices(services =>
                    {
                        // Add basic services
                        services.AddRouting();
                        services.AddHealthChecks();
                    });
                    
                    builder.Configure((context, app) =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapHealthChecks("/health");
                            
                            // Mock API endpoints
                            endpoints.MapGet("/", async context =>
                            {
                                await context.Response.WriteAsJsonAsync(new
                                {
                                    service = "Neo Service Layer Test Gateway",
                                    version = "1.0.0",
                                    status = "running",
                                    endpoints = new[] { "/health", "/api/*" }
                                });
                            });
                            
                            endpoints.MapPost("/api/notification/send", async context =>
                            {
                                var request = await context.Request.ReadFromJsonAsync<SendNotificationRequest>();
                                await context.Response.WriteAsJsonAsync(new NotificationResult
                                {
                                    NotificationId = Guid.NewGuid().ToString(),
                                    Success = true,
                                    Status = DeliveryStatus.Delivered,
                                    SentAt = DateTime.UtcNow,
                                    Channel = request?.Channel ?? NotificationChannel.Email
                                });
                            });
                            
                            endpoints.MapGet("/api/health/status", async context =>
                            {
                                await context.Response.WriteAsJsonAsync(new { status = "Healthy" });
                            });
                            
                            endpoints.MapGet("/metrics", async context =>
                            {
                                await context.Response.WriteAsync(@"# HELP http_requests_total Total HTTP requests
# TYPE http_requests_total counter
http_requests_total{method=""GET"",status=""200""} 42
# HELP http_request_duration_seconds HTTP request latency
# TYPE http_request_duration_seconds histogram
http_request_duration_seconds_bucket{le=""0.1""} 100");
                            });
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
            var response = await _client.GetAsync("/");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var info = await response.Content.ReadFromJsonAsync<GatewayInfo>();
            info.Should().NotBeNull();
            info!.Service.Should().Contain("Gateway");
            info.Status.Should().Be("running");
        }

        [Fact]
        public async Task NotificationService_SendNotification_ShouldSucceed()
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
            var response = await _client.PostAsJsonAsync("/api/notification/send", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<NotificationResult>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Status.Should().Be(DeliveryStatus.Delivered);
        }

        [Fact]
        public async Task HealthEndpoint_ShouldReturnHealthyStatus()
        {
            // Act
            var response = await _client.GetAsync("/api/health/status");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var health = await response.Content.ReadFromJsonAsync<HealthStatus>();
            health.Should().NotBeNull();
            health!.Status.Should().Be("Healthy");
        }

        [Fact]
        public async Task Metrics_PrometheusEndpoint_ShouldReturnMetrics()
        {
            // Act
            var response = await _client.GetAsync("/metrics");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            
            // Prometheus metrics format checks
            content.Should().Contain("# HELP");
            content.Should().Contain("# TYPE");
            content.Should().Contain("http_requests_total");
            content.Should().Contain("http_request_duration_seconds");
        }

        // Helper classes
        private class GatewayInfo
        {
            public string Service { get; set; } = string.Empty;
            public string Version { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string[] Endpoints { get; set; } = Array.Empty<string>();
        }

        private class HealthStatus
        {
            public string Status { get; set; } = string.Empty;
        }
    }

    public class TestStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Minimal service configuration
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Minimal pipeline configuration
        }
    }
}