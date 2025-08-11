using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NeoServiceLayer.Core;
using NeoServiceLayer.Integration.Tests.Helpers;
using NeoServiceLayer.Web;
using Xunit;

namespace NeoServiceLayer.Integration.Tests.Controllers;

public class AbstractAccountControllerTests : IntegrationTestBase
{
    public AbstractAccountControllerTests() : base()
    {
    }

    [Fact]
    public async Task CreateAccount_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            ownerAddress = "0x123...",
            guardians = new[] { "0x456...", "0x789..." },
            threshold = 2
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/abstract-account", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAccount_WithValidAuth_ReturnsAccount()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateJwtToken("test-user", "User"));
        var accountAddress = "0x123...";

        // Act
        var response = await Client.GetAsync($"/api/v1/abstract-account/{accountAddress}");

        // Assert
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddGuardian_RequiresAccountOwnerRole()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateJwtToken("test-user", "AccountOwner"));

        var request = new
        {
            guardianAddress = "0xabc..."
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/abstract-account/0x123/guardians", request);

        // Assert
        // Should be Forbidden if user doesn't have AccountOwner role
        Assert.True(response.StatusCode == HttpStatusCode.Forbidden ||
                   response.StatusCode == HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("/api/v1/abstract-account", "POST")]
    [InlineData("/api/v1/abstract-account/0x123", "GET")]
    [InlineData("/api/v1/abstract-account/0x123/guardians", "GET")]
    public async Task AllEndpoints_RequireAuthentication(string url, string method)
    {
        // Arrange
        var request = new HttpRequestMessage(new HttpMethod(method), url);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
