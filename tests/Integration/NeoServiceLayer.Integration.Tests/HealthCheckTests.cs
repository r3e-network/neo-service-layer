using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NeoServiceLayer.Web;
using Xunit;

namespace NeoServiceLayer.Integration.Tests;

public class HealthCheckTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public HealthCheckTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        var healthResponse = JsonSerializer.Deserialize<HealthCheckResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(healthResponse);
        Assert.NotNull(healthResponse.Status);
        Assert.NotNull(healthResponse.Checks);
    }

    [Fact]
    public async Task HealthReady_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        // Service might not be ready immediately, but should not return server error
        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                   response.StatusCode == HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task HealthLive_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Health_ContainsExpectedChecks()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();
        var healthResponse = JsonSerializer.Deserialize<HealthCheckResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(healthResponse?.Checks);

        // Verify core checks exist
        Assert.Contains("self", healthResponse.Checks.Keys);
        Assert.Contains("blockchain", healthResponse.Checks.Keys);
        Assert.Contains("storage", healthResponse.Checks.Keys);
        Assert.Contains("configuration", healthResponse.Checks.Keys);
        Assert.Contains("neo-services", healthResponse.Checks.Keys);
        Assert.Contains("resources", healthResponse.Checks.Keys);
        Assert.Contains("sgx", healthResponse.Checks.Keys);

        // Verify new service checks exist
        Assert.Contains("security-services", healthResponse.Checks.Keys);
        Assert.Contains("blockchain-services", healthResponse.Checks.Keys);
        Assert.Contains("data-services", healthResponse.Checks.Keys);
        Assert.Contains("advanced-services", healthResponse.Checks.Keys);
    }

    [Fact]
    public async Task ApiInfo_ReturnsVersionInfo()
    {
        // Act
        var response = await _client.GetAsync("/api/info");

        // Assert
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var info = JsonSerializer.Deserialize<ApiInfo>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(info);
        Assert.Equal("Neo Service Layer API", info.Name);
        Assert.Equal("1.0.0", info.Version);
        Assert.NotNull(info.Environment);
    }

    private class HealthCheckResponse
    {
        public string Status { get; set; } = string.Empty;
        public double TotalDuration { get; set; }
        public Dictionary<string, CheckResult> Checks { get; set; } = new();
        public DateTime Timestamp { get; set; }
    }

    private class CheckResult
    {
        public string Status { get; set; } = string.Empty;
        public double Duration { get; set; }
        public string? Description { get; set; }
        public Dictionary<string, object>? Data { get; set; }
        public string? Exception { get; set; }
        public List<string>? Tags { get; set; }
    }

    private class ApiInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Environment { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
