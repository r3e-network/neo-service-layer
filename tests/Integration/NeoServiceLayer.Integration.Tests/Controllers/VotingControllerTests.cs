using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NeoServiceLayer.Core;
using NeoServiceLayer.Web;
using Xunit;

namespace NeoServiceLayer.Integration.Tests.Controllers;

public class VotingControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public VotingControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetCouncilNodes_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/voting/council-nodes");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateVotingStrategy_WithoutAdminRole_ReturnsForbidden()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateTestToken("user"));

        var request = new
        {
            name = "Test Strategy",
            description = "Test voting strategy"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/voting/strategies", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetNetworkHealth_WithValidAuth_ReturnsSuccess()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateTestToken("user"));

        // Act
        var response = await _client.GetAsync("/api/v1/voting/network-health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("success", content, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("strategies", "user")]
    [InlineData("council-nodes", "user")]
    [InlineData("network-health", "user")]
    [InlineData("alerts", "user")]
    public async Task VotingEndpoints_WithValidAuth_ReturnsSuccess(string endpoint, string role)
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateTestToken(role));

        // Act
        var response = await _client.GetAsync($"/api/v1/voting/{endpoint}?ownerAddress=test");

        // Assert
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    private string GenerateTestToken(string role)
    {
        // In a real test, this would generate a valid JWT token with the specified role
        // For now, returning a dummy token
        return "test-token-" + role;
    }
}
