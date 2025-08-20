using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NeoServiceLayer.Core;
using NeoServiceLayer.Integration.Tests.Helpers;
using NeoServiceLayer.Web;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Integration.Tests.Controllers;

public class VotingControllerTests : Helpers.IntegrationTestBase
{
    public VotingControllerTests() : base()
    {
    }

    [Fact]
    public async Task GetCouncilNodes_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/v1/voting/council-nodes");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateVotingStrategy_WithoutAdminRole_ReturnsForbidden()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateJwtToken("test-user", "User"));

        var request = new
        {
            name = "Test Strategy",
            description = "Test voting strategy"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/voting/strategies", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetNetworkHealth_WithValidAuth_ReturnsSuccess()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateJwtToken("test-user", "User"));

        // Act
        var response = await Client.GetAsync("/api/v1/voting/network-health");

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
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateJwtToken("test-user", role));

        // Act
        var response = await Client.GetAsync($"/api/v1/voting/{endpoint}?ownerAddress=test");

        // Assert
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

}
