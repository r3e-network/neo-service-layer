using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Api.Controllers;
using NeoServiceLayer.Services.Health;
using Xunit;

namespace NeoServiceLayer.Api.Tests.Controllers;

public class HealthControllerUnitTests
{
    private readonly Mock<ILogger<HealthController>> _mockLogger;
    private readonly Mock<IHealthService> _mockHealthService;
    private readonly HealthController _controller;

    public HealthControllerUnitTests()
    {
        _mockLogger = new Mock<ILogger<HealthController>>();
        _mockHealthService = new Mock<IHealthService>();
        _controller = new HealthController(_mockLogger.Object, _mockHealthService.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_InitializesCorrectly()
    {
        // Arrange & Act
        var controller = new HealthController(_mockLogger.Object, _mockHealthService.Object);

        // Assert
        controller.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Action act = () => new HealthController(null!, _mockHealthService.Object);
        act.Should().Throw<ArgumentNullException>().WithMessage("*logger*");
    }

    [Fact]
    public void Constructor_WithNullHealthService_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Action act = () => new HealthController(_mockLogger.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithMessage("*healthService*");
    }

    [Fact]
    public async Task GetHealthAsync_WhenServiceHealthy_ReturnsOkResult()
    {
        // Arrange
        var healthData = new Dictionary<string, object>
        {
            ["Status"] = "Healthy",
            ["Timestamp"] = DateTime.UtcNow,
            ["Services"] = new Dictionary<string, object>
            {
                ["Database"] = "Connected",
                ["Cache"] = "Connected"
            }
        };

        _mockHealthService.Setup(x => x.GetHealthAsync())
            .ReturnsAsync(healthData);

        // Act
        var result = await _controller.GetHealthAsync();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(healthData);

        _mockHealthService.Verify(x => x.GetHealthAsync(), Times.Once);
    }

    [Fact]
    public async Task GetHealthAsync_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockHealthService.Setup(x => x.GetHealthAsync())
            .ThrowsAsync(new InvalidOperationException("Health check failed"));

        // Act
        var result = await _controller.GetHealthAsync();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        objectResult.Value.Should().NotBeNull();
        
        var errorResponse = objectResult.Value as dynamic;
        errorResponse!.error.Should().Be("Health check failed");
    }

    [Fact]
    public async Task GetDetailedHealthAsync_WhenServiceHealthy_ReturnsDetailedHealth()
    {
        // Arrange
        var detailedHealth = new Dictionary<string, object>
        {
            ["Status"] = "Healthy",
            ["Timestamp"] = DateTime.UtcNow,
            ["Uptime"] = "02:30:15",
            ["Services"] = new Dictionary<string, object>
            {
                ["DatabaseHealth"] = new Dictionary<string, object>
                {
                    ["Status"] = "Connected",
                    ["ResponseTime"] = "15ms",
                    ["LastCheck"] = DateTime.UtcNow
                },
                ["CacheHealth"] = new Dictionary<string, object>
                {
                    ["Status"] = "Connected",
                    ["HitRate"] = "95%",
                    ["MemoryUsage"] = "512MB"
                }
            },
            ["SystemResources"] = new Dictionary<string, object>
            {
                ["CpuUsage"] = "25%",
                ["MemoryUsage"] = "60%",
                ["DiskSpace"] = "40GB Free"
            }
        };

        _mockHealthService.Setup(x => x.GetDetailedHealthAsync())
            .ReturnsAsync(detailedHealth);

        // Act
        var result = await _controller.GetDetailedHealthAsync();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(detailedHealth);

        _mockHealthService.Verify(x => x.GetDetailedHealthAsync(), Times.Once);
    }

    [Fact]
    public async Task GetServiceHealthAsync_WithValidServiceName_ReturnsServiceHealth()
    {
        // Arrange
        var serviceName = "DatabaseService";
        var serviceHealth = new Dictionary<string, object>
        {
            ["ServiceName"] = serviceName,
            ["Status"] = "Healthy",
            ["ResponseTime"] = "12ms",
            ["LastCheck"] = DateTime.UtcNow,
            ["Details"] = new Dictionary<string, object>
            {
                ["ConnectionCount"] = 5,
                ["ActiveQueries"] = 2
            }
        };

        _mockHealthService.Setup(x => x.GetServiceHealthAsync(serviceName))
            .ReturnsAsync(serviceHealth);

        // Act
        var result = await _controller.GetServiceHealthAsync(serviceName);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(serviceHealth);

        _mockHealthService.Verify(x => x.GetServiceHealthAsync(serviceName), Times.Once);
    }

    [Fact]
    public async Task GetServiceHealthAsync_WithNonExistingService_ReturnsNotFound()
    {
        // Arrange
        var serviceName = "NonExistingService";

        _mockHealthService.Setup(x => x.GetServiceHealthAsync(serviceName))
            .ReturnsAsync((Dictionary<string, object>?)null);

        // Act
        var result = await _controller.GetServiceHealthAsync(serviceName);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        
        var errorResponse = notFoundResult!.Value as dynamic;
        errorResponse!.error.Should().Be($"Service '{serviceName}' not found");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public async Task GetServiceHealthAsync_WithEmptyServiceName_ReturnsBadRequest(string serviceName)
    {
        // Act
        var result = await _controller.GetServiceHealthAsync(serviceName);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        
        var errorResponse = badRequestResult!.Value as dynamic;
        errorResponse!.error.Should().Be("Service name cannot be empty");
    }

    [Fact]
    public async Task GetLivenessAsync_WhenServiceRunning_ReturnsOk()
    {
        // Arrange
        _mockHealthService.Setup(x => x.IsRunning)
            .Returns(true);

        // Act
        var result = await _controller.GetLivenessAsync();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        
        var response = okResult!.Value as dynamic;
        response!.status.Should().Be("alive");
        response.timestamp.Should().NotBeNull();
    }

    [Fact]
    public async Task GetLivenessAsync_WhenServiceNotRunning_ReturnsServiceUnavailable()
    {
        // Arrange
        _mockHealthService.Setup(x => x.IsRunning)
            .Returns(false);

        // Act
        var result = await _controller.GetLivenessAsync();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(503); // Service Unavailable
        
        var response = objectResult.Value as dynamic;
        response!.status.Should().Be("not running");
    }

    [Fact]
    public async Task GetReadinessAsync_WhenServiceReady_ReturnsOk()
    {
        // Arrange
        var readinessStatus = new Dictionary<string, object>
        {
            ["IsReady"] = true,
            ["Services"] = new Dictionary<string, object>
            {
                ["Database"] = "Ready",
                ["Cache"] = "Ready",
                ["ExternalAPI"] = "Ready"
            }
        };

        _mockHealthService.Setup(x => x.GetReadinessAsync())
            .ReturnsAsync(readinessStatus);

        // Act
        var result = await _controller.GetReadinessAsync();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(readinessStatus);

        _mockHealthService.Verify(x => x.GetReadinessAsync(), Times.Once);
    }

    [Fact]
    public async Task GetReadinessAsync_WhenServiceNotReady_ReturnsServiceUnavailable()
    {
        // Arrange
        var readinessStatus = new Dictionary<string, object>
        {
            ["IsReady"] = false,
            ["Services"] = new Dictionary<string, object>
            {
                ["Database"] = "Ready",
                ["Cache"] = "Not Ready",
                ["ExternalAPI"] = "Timeout"
            },
            ["Reason"] = "Cache and ExternalAPI not ready"
        };

        _mockHealthService.Setup(x => x.GetReadinessAsync())
            .ReturnsAsync(readinessStatus);

        // Act
        var result = await _controller.GetReadinessAsync();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(503); // Service Unavailable
        objectResult.Value.Should().BeEquivalentTo(readinessStatus);

        _mockHealthService.Verify(x => x.GetReadinessAsync(), Times.Once);
    }

    [Fact]
    public async Task GetMetricsAsync_ReturnsHealthMetrics()
    {
        // Arrange
        var metrics = new Dictionary<string, object>
        {
            ["RequestCount"] = 1250,
            ["AverageResponseTime"] = 125.5,
            ["ErrorRate"] = 0.02,
            ["Uptime"] = "05:30:42",
            ["MemoryUsage"] = new Dictionary<string, object>
            {
                ["Used"] = "256MB",
                ["Available"] = "768MB",
                ["Percentage"] = "25%"
            },
            ["CpuUsage"] = "15%",
            ["ActiveConnections"] = 42
        };

        _mockHealthService.Setup(x => x.GetMetricsAsync())
            .ReturnsAsync(metrics);

        // Act
        var result = await _controller.GetMetricsAsync();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(metrics);

        _mockHealthService.Verify(x => x.GetMetricsAsync(), Times.Once);
    }
}