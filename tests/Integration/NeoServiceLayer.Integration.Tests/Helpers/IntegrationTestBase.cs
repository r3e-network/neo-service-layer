using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

using NeoServiceLayer.Web;

namespace NeoServiceLayer.Integration.Tests.Helpers;

public abstract class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;
    private readonly string _testJwtSecretKey;

    protected IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        // Generate a unique test JWT secret key
        _testJwtSecretKey = "TestJwtSecretKeyForIntegrationTests_" + Guid.NewGuid().ToString("N");
        
        Factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
            
            // Set JWT secret key as environment variable
            Environment.SetEnvironmentVariable("JWT_SECRET_KEY", _testJwtSecretKey);
            
            builder.ConfigureServices(services =>
            {
                // Override services for testing if needed
                // For example, replace real services with mocks
            });
        });

        Client = Factory.CreateClient();
    }

    protected string GenerateJwtToken(string userId = "test-user", string role = "User", int expirationMinutes = 60)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_testJwtSecretKey);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userId),
            new Claim(ClaimTypes.Role, role)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = "NeoServiceLayer",
            Audience = "NeoServiceLayerUsers"
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    protected void SetAuthorizationHeader(string token)
    {
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    protected void SetAuthorizationHeader(string userId, string role)
    {
        var token = GenerateJwtToken(userId, role);
        SetAuthorizationHeader(token);
    }

    protected async Task<T?> GetResponseContent<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return System.Text.Json.JsonSerializer.Deserialize<T>(content, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    protected class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
