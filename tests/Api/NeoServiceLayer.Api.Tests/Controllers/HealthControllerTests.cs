using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Api.Controllers;
using NeoServiceLayer.Services.Health;
using Xunit;

namespace NeoServiceLayer.Api.Tests.Controllers
{
    public class HealthControllerTests
    {
        private readonly Mock<IHealthService> _mockHealthService;
        private readonly Mock<ILogger<HealthController>> _mockLogger;
        private readonly Mock<HealthCheckService> _mockHealthCheckService;
        private readonly HealthController _controller;

        public HealthControllerTests()
        {
            _mockHealthService = new Mock<IHealthService>();
            _mockLogger = new Mock<ILogger<HealthController>>();
            _mockHealthCheckService = new Mock<HealthCheckService>();
            _controller = new HealthController(_mockHealthService.Object, _mockLogger.Object, _mockHealthCheckService.Object);
        }

        [Fact]
        public async Task GetHealthAsync_WhenHealthy_ReturnsOkResult()
        {
            // Arrange
            var healthStatus = new HealthStatusResponse
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Services = new Dictionary<string, ServiceHealthInfo>
                {
                    ["Database"] = new ServiceHealthInfo { Status = "Healthy", ResponseTime = 50 },
                    ["Cache"] = new ServiceHealthInfo { Status = "Healthy", ResponseTime = 10 }
                }
            };
            _mockHealthService.Setup(x => x.GetHealthStatusAsync())
                .ReturnsAsync(healthStatus);

            // Act
            var result = await _controller.GetHealthAsync();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().BeEquivalentTo(healthStatus);
        }

        [Fact]
        public async Task GetHealthAsync_WhenUnhealthy_ReturnsServiceUnavailable()
        {
            // Arrange
            var healthStatus = new HealthStatusResponse
            {
                Status = "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Services = new Dictionary<string, ServiceHealthInfo>
                {
                    ["Database"] = new ServiceHealthInfo { Status = "Unhealthy", ResponseTime = 5000, Error = "Connection timeout" }
                }
            };
            _mockHealthService.Setup(x => x.GetHealthStatusAsync())
                .ReturnsAsync(healthStatus);

            // Act
            var result = await _controller.GetHealthAsync();

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.StatusCode.Should().Be(503); // Service Unavailable
            objectResult.Value.Should().BeEquivalentTo(healthStatus);
        }

        [Fact]
        public async Task GetHealthAsync_WhenDegraded_ReturnsOkWithWarning()
        {
            // Arrange
            var healthStatus = new HealthStatusResponse
            {
                Status = "Degraded",
                Timestamp = DateTime.UtcNow,
                Services = new Dictionary<string, ServiceHealthInfo>
                {
                    ["Database"] = new ServiceHealthInfo { Status = "Healthy", ResponseTime = 50 },
                    ["Cache"] = new ServiceHealthInfo { Status = "Degraded", ResponseTime = 1000, Warning = "High latency" }
                }
            };
            _mockHealthService.Setup(x => x.GetHealthStatusAsync())
                .ReturnsAsync(healthStatus);

            // Act
            var result = await _controller.GetHealthAsync();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().BeEquivalentTo(healthStatus);
        }

        [Fact]
        public async Task GetHealthAsync_WithException_ReturnsInternalServerError()
        {
            // Arrange
            _mockHealthService.Setup(x => x.GetHealthStatusAsync())
                .ThrowsAsync(new Exception("Health check failed"));

            // Act
            var result = await _controller.GetHealthAsync();

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.StatusCode.Should().Be(500);
            objectResult.Value.Should().BeOfType<ProblemDetails>();
        }

        [Fact]
        public async Task GetLivenessAsync_WhenAlive_ReturnsOk()
        {
            // Act
            var result = await _controller.GetLivenessAsync();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult.Value as dynamic;
            response.status.Should().Be("alive");
        }

        [Fact]
        public async Task GetReadinessAsync_WhenReady_ReturnsOk()
        {
            // Arrange
            _mockHealthService.Setup(x => x.IsReadyAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _controller.GetReadinessAsync();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult.Value as dynamic;
            response.status.Should().Be("ready");
        }

        [Fact]
        public async Task GetReadinessAsync_WhenNotReady_ReturnsServiceUnavailable()
        {
            // Arrange
            _mockHealthService.Setup(x => x.IsReadyAsync())
                .ReturnsAsync(false);

            // Act
            var result = await _controller.GetReadinessAsync();

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.StatusCode.Should().Be(503);
            var response = objectResult.Value as dynamic;
            response.status.Should().Be("not ready");
        }

        [Fact]
        public async Task GetDetailedHealthAsync_ReturnsDetailedInformation()
        {
            // Arrange
            var detailedHealth = new DetailedHealthResponse
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0",
                Environment = "Test",
                Services = new Dictionary<string, DetailedServiceHealth>
                {
                    ["Database"] = new DetailedServiceHealth
                    {
                        Status = "Healthy",
                        ResponseTime = 50,
                        LastChecked = DateTime.UtcNow,
                        Dependencies = new[] { "Network", "Storage" },
                        Metrics = new Dictionary<string, object>
                        {
                            ["ConnectionsActive"] = 10,
                            ["ConnectionsIdle"] = 40
                        }
                    }
                }
            };
            _mockHealthService.Setup(x => x.GetDetailedHealthAsync())
                .ReturnsAsync(detailedHealth);

            // Act
            var result = await _controller.GetDetailedHealthAsync();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().BeEquivalentTo(detailedHealth);
        }

        [Fact]
        public async Task RunDiagnosticsAsync_WithValidKey_RunsDiagnostics()
        {
            // Arrange
            var diagnosticsKey = "valid-key";
            var diagnosticsResult = new DiagnosticsResult
            {
                StartTime = DateTime.UtcNow.AddMinutes(-1),
                EndTime = DateTime.UtcNow,
                Duration = TimeSpan.FromMinutes(1),
                Tests = new[]
                {
                    new DiagnosticTest { Name = "Database Connection", Passed = true, Duration = 100 },
                    new DiagnosticTest { Name = "Cache Connection", Passed = true, Duration = 50 }
                }
            };
            _mockHealthService.Setup(x => x.RunDiagnosticsAsync(diagnosticsKey))
                .ReturnsAsync(diagnosticsResult);

            // Act
            var result = await _controller.RunDiagnosticsAsync(diagnosticsKey);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().BeEquivalentTo(diagnosticsResult);
        }

        [Fact]
        public async Task RunDiagnosticsAsync_WithInvalidKey_ReturnsUnauthorized()
        {
            // Arrange
            var invalidKey = "invalid-key";
            _mockHealthService.Setup(x => x.RunDiagnosticsAsync(invalidKey))
                .ThrowsAsync(new UnauthorizedAccessException("Invalid diagnostics key"));

            // Act
            var result = await _controller.RunDiagnosticsAsync(invalidKey);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetMetricsAsync_ReturnsSystemMetrics()
        {
            // Arrange
            var metrics = new SystemMetrics
            {
                Timestamp = DateTime.UtcNow,
                CpuUsage = 45.2,
                MemoryUsage = 60.5,
                DiskUsage = 30.1,
                NetworkLatency = 20,
                ActiveConnections = 150,
                RequestsPerSecond = 1000
            };
            _mockHealthService.Setup(x => x.GetSystemMetricsAsync())
                .ReturnsAsync(metrics);

            // Act
            var result = await _controller.GetMetricsAsync();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().BeEquivalentTo(metrics);
        }

        [Fact]
        public async Task ResetHealthAsync_WithAdminPrivileges_ResetsHealth()
        {
            // Arrange
            _mockHealthService.Setup(x => x.ResetHealthStatusAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ResetHealthAsync();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult.Value as dynamic;
            response.message.Should().Be("Health status reset successfully");
        }
    }

    // Test models
    public class HealthStatusResponse
    {
        public string Status { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, ServiceHealthInfo> Services { get; set; }
    }

    public class ServiceHealthInfo
    {
        public string Status { get; set; }
        public int ResponseTime { get; set; }
        public string Error { get; set; }
        public string Warning { get; set; }
    }

    public class DetailedHealthResponse : HealthStatusResponse
    {
        public string Version { get; set; }
        public string Environment { get; set; }
        public new Dictionary<string, DetailedServiceHealth> Services { get; set; }
    }

    public class DetailedServiceHealth : ServiceHealthInfo
    {
        public DateTime LastChecked { get; set; }
        public string[] Dependencies { get; set; }
        public Dictionary<string, object> Metrics { get; set; }
    }

    public class DiagnosticsResult
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public DiagnosticTest[] Tests { get; set; }
    }

    public class DiagnosticTest
    {
        public string Name { get; set; }
        public bool Passed { get; set; }
        public int Duration { get; set; }
    }

    public class SystemMetrics
    {
        public DateTime Timestamp { get; set; }
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double DiskUsage { get; set; }
        public int NetworkLatency { get; set; }
        public int ActiveConnections { get; set; }
        public int RequestsPerSecond { get; set; }
    }
}