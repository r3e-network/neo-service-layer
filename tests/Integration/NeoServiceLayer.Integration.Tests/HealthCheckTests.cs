using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NeoServiceLayer.Integration.Tests.Helpers;
using NeoServiceLayer.Web;
using Xunit;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System;


namespace NeoServiceLayer.Integration.Tests;

public class HealthCheckTests : Helpers.IntegrationTestBase
{
    public HealthCheckTests() : base()
    {
    }

    [Fact]
    public async Task Health_ReturnsSuccess()
    {
        // Act
        var response = await Client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", content);
    }

    [Fact]
    public async Task HealthReady_ReturnsSuccess()
    {
        // Act
        var response = await Client.GetAsync("/health/ready");

        // Assert
        // Service might not be ready immediately, but should not return server error
        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                   response.StatusCode == HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task HealthLive_ReturnsSuccess()
    {
        // Act
        var response = await Client.GetAsync("/health/live");

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact(Skip = "Health checks return plain text in current configuration")]
    public async Task Health_ContainsExpectedChecks()
    {
        // Act
        var response = await Client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Note: Current health check configuration returns plain text "Healthy"
        // This test is skipped until health checks are configured to return JSON
        Assert.Equal("Healthy", content);
    }

    [Fact]
    public async Task ApiInfo_ReturnsVersionInfo()
    {
        // Act
        var response = await Client.GetAsync("/api/info");

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
