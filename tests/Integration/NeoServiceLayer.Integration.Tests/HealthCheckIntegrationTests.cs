using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace NeoServiceLayer.Integration.Tests;

/// <summary>
/// Integration tests for health check endpoints.
/// </summary>
public class HealthCheckIntegrationTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private readonly TestWebApplicationFactory _factory;
    private HttpClient _client;

    public HealthCheckIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _factory = new TestWebApplicationFactory(output);
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Health_Endpoint_Should_Return_Detailed_Status()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var healthReport = JsonSerializer.Deserialize<HealthReportResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(healthReport);
        Assert.NotNull(healthReport.Status);
        Assert.NotNull(healthReport.Checks);
        Assert.True(healthReport.TotalDuration >= 0);

        _output.WriteLine($"Health Status: {healthReport.Status}");
        _output.WriteLine($"Total Duration: {healthReport.TotalDuration}ms");
        _output.WriteLine($"Checks Count: {healthReport.Checks.Count}");

        foreach (var check in healthReport.Checks)
        {
            _output.WriteLine($"  - {check.Component}: {check.Status} ({check.Duration}ms)");
            if (!string.IsNullOrEmpty(check.Description))
            {
                _output.WriteLine($"    Description: {check.Description}");
            }
        }
    }

    [Fact]
    public async Task Health_Ready_Endpoint_Should_Check_Dependencies()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.ServiceUnavailable);
        
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Ready Check Status: {response.StatusCode}");
        _output.WriteLine($"Ready Check Response: {content}");

        // If healthy, should return OK
        if (response.StatusCode == HttpStatusCode.OK)
        {
            Assert.Equal("Healthy", content);
        }
        else
        {
            // If unhealthy, should return ServiceUnavailable with details
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }
    }

    [Fact]
    public async Task Health_Live_Endpoint_Should_Always_Return_OK()
    {
        // Act
        var response = await _client.GetAsync("/health/live");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", content);

        _output.WriteLine($"Liveness Check: {response.StatusCode} - {content}");
    }

    [Fact]
    public async Task Info_Endpoint_Should_Return_Service_Information()
    {
        // Act
        var response = await _client.GetAsync("/info");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var info = JsonSerializer.Deserialize<ServiceInfoResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(info);
        Assert.NotNull(info.Service);
        Assert.NotNull(info.Version);
        Assert.NotNull(info.Environment);
        Assert.True(info.Timestamp > DateTime.MinValue);

        _output.WriteLine($"Service: {info.Service}");
        _output.WriteLine($"Version: {info.Version}");
        _output.WriteLine($"Environment: {info.Environment}");
        _output.WriteLine($"Timestamp: {info.Timestamp}");
    }

    [Fact]
    public async Task Health_Endpoints_Should_Have_Correct_Cache_Headers()
    {
        // Arrange
        var endpoints = new[] { "/health", "/health/ready", "/health/live" };

        foreach (var endpoint in endpoints)
        {
            // Act
            var response = await _client.GetAsync(endpoint);

            // Assert
            Assert.True(response.Headers.Contains("Cache-Control") || 
                       !response.Headers.CacheControl?.NoCache == false,
                $"Health endpoint {endpoint} should have appropriate cache headers");

            _output.WriteLine($"Endpoint {endpoint}: {response.StatusCode}, Cache-Control: {response.Headers.CacheControl}");
        }
    }

    [Fact]
    public async Task Health_Endpoints_Should_Be_Fast()
    {
        // Arrange
        var endpoints = new[] { "/health/ready", "/health/live" };
        const int maxResponseTimeMs = 5000; // 5 seconds max for health checks

        foreach (var endpoint in endpoints)
        {
            // Act
            var startTime = DateTime.UtcNow;
            var response = await _client.GetAsync(endpoint);
            var duration = DateTime.UtcNow - startTime;

            // Assert
            Assert.True(duration.TotalMilliseconds < maxResponseTimeMs,
                $"Health endpoint {endpoint} took {duration.TotalMilliseconds}ms, which exceeds {maxResponseTimeMs}ms");

            _output.WriteLine($"Endpoint {endpoint} responded in {duration.TotalMilliseconds}ms");
        }
    }

    [Fact]
    public async Task Health_Endpoint_Should_Include_Self_Check()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var healthReport = JsonSerializer.Deserialize<HealthReportResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(healthReport);
        Assert.Contains(healthReport.Checks, check => 
            check.Component.Equals("self", StringComparison.OrdinalIgnoreCase));

        var selfCheck = healthReport.Checks.First(c => 
            c.Component.Equals("self", StringComparison.OrdinalIgnoreCase));
        
        Assert.Equal("Healthy", selfCheck.Status);
        _output.WriteLine($"Self check status: {selfCheck.Status}");
    }

    [Fact]
    public async Task Multiple_Concurrent_Health_Checks_Should_Succeed()
    {
        // Arrange
        const int concurrentRequests = 10;
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act
        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks.Add(_client.GetAsync("/health/ready"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        Assert.All(responses, response =>
        {
            Assert.True(response.StatusCode == HttpStatusCode.OK || 
                       response.StatusCode == HttpStatusCode.ServiceUnavailable);
        });

        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        _output.WriteLine($"Concurrent health checks: {successCount}/{concurrentRequests} successful");

        // Cleanup
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }
}

/// <summary>
/// Health report response model for deserialization.
/// </summary>
public class HealthReportResponse
{
    public string Status { get; set; } = string.Empty;
    public List<HealthCheckResponse> Checks { get; set; } = new();
    public double TotalDuration { get; set; }
}

/// <summary>
/// Individual health check response model.
/// </summary>
public class HealthCheckResponse
{
    public string Component { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Duration { get; set; }
}

/// <summary>
/// Service info response model for deserialization.
/// </summary>
public class ServiceInfoResponse
{
    public string Service { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}