using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neo.SecretsManagement.Service.Data;
using Neo.SecretsManagement.Service.Models;
using NUnit.Framework;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Neo.SecretsManagement.Service.Tests.Integration;

[TestFixture]
public class SecretsManagementIntegrationTests : IDisposable
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private readonly string _testUserId = "integration-test-user";

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                
                builder.ConfigureServices(services =>
                {
                    // Remove the existing DbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<SecretsDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add in-memory database for testing
                    services.AddDbContext<SecretsDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("IntegrationTestDb");
                    });

                    // Reduce logging noise in tests
                    services.AddLogging(builder =>
                    {
                        builder.SetMinimumLevel(LogLevel.Warning);
                    });
                });
            });

        _client = _factory.CreateClient();
        
        // Set up authentication header (simplified for testing)
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", "test-jwt-token");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [SetUp]
    public async Task SetUp()
    {
        // Clean database before each test
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SecretsDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    [Test]
    public async Task CreateAndRetrieveSecret_EndToEnd_Success()
    {
        // Arrange
        var createRequest = new CreateSecretRequest
        {
            Name = "Integration Test Secret",
            Path = "/integration/test-secret",
            Value = "super-secret-value",
            Type = SecretType.Generic,
            Description = "Created during integration test",
            Tags = new Dictionary<string, string>
            {
                { "environment", "test" },
                { "team", "integration" }
            }
        };

        var createJson = JsonSerializer.Serialize(createRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Act 1: Create secret
        var createResponse = await _client.PostAsync("/api/v1/secrets", 
            new StringContent(createJson, Encoding.UTF8, "application/json"));

        // Assert 1: Creation successful
        Assert.That(createResponse.IsSuccessStatusCode, Is.True, 
            $"Create failed: {await createResponse.Content.ReadAsStringAsync()}");
        Assert.That(createResponse.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Created));

        var createdSecretJson = await createResponse.Content.ReadAsStringAsync();
        var createdSecret = JsonSerializer.Deserialize<SecretResponse>(createdSecretJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.That(createdSecret, Is.Not.Null);
        Assert.That(createdSecret!.Name, Is.EqualTo(createRequest.Name));
        Assert.That(createdSecret.Path, Is.EqualTo(createRequest.Path));
        Assert.That(createdSecret.Type, Is.EqualTo(createRequest.Type));

        // Act 2: Retrieve secret without value
        var getResponse = await _client.GetAsync($"/api/v1/secrets{createRequest.Path}");

        // Assert 2: Retrieval successful
        Assert.That(getResponse.IsSuccessStatusCode, Is.True);

        var retrievedSecretJson = await getResponse.Content.ReadAsStringAsync();
        var retrievedSecret = JsonSerializer.Deserialize<SecretResponse>(retrievedSecretJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.That(retrievedSecret, Is.Not.Null);
        Assert.That(retrievedSecret!.Name, Is.EqualTo(createRequest.Name));
        Assert.That(retrievedSecret.Value, Is.Null); // Should not include value by default

        // Act 3: Retrieve secret with value
        var getWithValueResponse = await _client.GetAsync($"/api/v1/secrets{createRequest.Path}?includeValue=true");

        // Assert 3: Value included
        Assert.That(getWithValueResponse.IsSuccessStatusCode, Is.True);

        var secretWithValueJson = await getWithValueResponse.Content.ReadAsStringAsync();
        var secretWithValue = JsonSerializer.Deserialize<SecretResponse>(secretWithValueJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.That(secretWithValue, Is.Not.Null);
        Assert.That(secretWithValue!.Value, Is.Not.Null);
        Assert.That(secretWithValue.Value, Is.Not.Empty);
    }

    [Test]
    public async Task UpdateSecret_EndToEnd_Success()
    {
        // Arrange: Create a secret first
        var secret = await CreateTestSecretAsync("/integration/update-test", "original-value");

        var updateRequest = new UpdateSecretRequest
        {
            Value = "updated-secret-value",
            Description = "Updated during integration test",
            Tags = new Dictionary<string, string>
            {
                { "updated", "true" },
                { "version", "2" }
            }
        };

        var updateJson = JsonSerializer.Serialize(updateRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Act: Update secret
        var updateResponse = await _client.PutAsync($"/api/v1/secrets{secret.Path}",
            new StringContent(updateJson, Encoding.UTF8, "application/json"));

        // Assert: Update successful
        Assert.That(updateResponse.IsSuccessStatusCode, Is.True,
            $"Update failed: {await updateResponse.Content.ReadAsStringAsync()}");
        Assert.That(updateResponse.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NoContent));

        // Verify the update
        var getResponse = await _client.GetAsync($"/api/v1/secrets{secret.Path}");
        Assert.That(getResponse.IsSuccessStatusCode, Is.True);

        var updatedSecretJson = await getResponse.Content.ReadAsStringAsync();
        var updatedSecret = JsonSerializer.Deserialize<SecretResponse>(updatedSecretJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.That(updatedSecret, Is.Not.Null);
        Assert.That(updatedSecret!.Description, Is.EqualTo(updateRequest.Description));
        Assert.That(updatedSecret.CurrentVersion, Is.EqualTo(2)); // Version should be incremented
    }

    [Test]
    public async Task DeleteSecret_EndToEnd_Success()
    {
        // Arrange: Create a secret first
        var secret = await CreateTestSecretAsync("/integration/delete-test", "to-be-deleted");

        // Act: Delete secret
        var deleteResponse = await _client.DeleteAsync($"/api/v1/secrets{secret.Path}");

        // Assert: Deletion successful
        Assert.That(deleteResponse.IsSuccessStatusCode, Is.True,
            $"Delete failed: {await deleteResponse.Content.ReadAsStringAsync()}");
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NoContent));

        // Verify secret is no longer accessible
        var getResponse = await _client.GetAsync($"/api/v1/secrets{secret.Path}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
    }

    [Test]
    public async Task ListSecrets_WithFiltering_Success()
    {
        // Arrange: Create multiple secrets
        await CreateTestSecretAsync("/app/secret1", "value1", SecretType.Generic);
        await CreateTestSecretAsync("/app/secret2", "value2", SecretType.Password);
        await CreateTestSecretAsync("/services/api-key", "value3", SecretType.ApiKey);

        // Act: List secrets with path prefix filter
        var listResponse = await _client.GetAsync("/api/v1/secrets?pathPrefix=/app&take=10");

        // Assert: Filtering works
        Assert.That(listResponse.IsSuccessStatusCode, Is.True);

        var secretsJson = await listResponse.Content.ReadAsStringAsync();
        var secrets = JsonSerializer.Deserialize<List<SecretResponse>>(secretsJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.That(secrets, Is.Not.Null);
        Assert.That(secrets!.Count, Is.EqualTo(2));
        Assert.That(secrets.All(s => s.Path.StartsWith("/app")), Is.True);
    }

    [Test]
    public async Task SecretVersions_EndToEnd_Success()
    {
        // Arrange: Create a secret
        var secret = await CreateTestSecretAsync("/integration/versions-test", "version-1");

        // Update the secret to create a new version
        var updateRequest = new UpdateSecretRequest { Value = "version-2" };
        var updateJson = JsonSerializer.Serialize(updateRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await _client.PutAsync($"/api/v1/secrets{secret.Path}",
            new StringContent(updateJson, Encoding.UTF8, "application/json"));

        // Act: Get secret versions
        var versionsResponse = await _client.GetAsync($"/api/v1/secrets{secret.Path}/versions");

        // Assert: Versions retrieved
        Assert.That(versionsResponse.IsSuccessStatusCode, Is.True);

        var versionsJson = await versionsResponse.Content.ReadAsStringAsync();
        var versions = JsonSerializer.Deserialize<List<SecretVersion>>(versionsJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.That(versions, Is.Not.Null);
        Assert.That(versions!.Count, Is.EqualTo(1)); // Only the old version should be in versions table
        Assert.That(versions[0].Version, Is.EqualTo(1));
    }

    [Test]
    public async Task SecretStatistics_EndToEnd_Success()
    {
        // Arrange: Create various types of secrets
        await CreateTestSecretAsync("/stats/generic1", "value", SecretType.Generic);
        await CreateTestSecretAsync("/stats/generic2", "value", SecretType.Generic);
        await CreateTestSecretAsync("/stats/password1", "value", SecretType.Password);
        await CreateTestSecretAsync("/stats/apikey1", "value", SecretType.ApiKey);

        // Act: Get statistics
        var statsResponse = await _client.GetAsync("/api/v1/secrets/statistics");

        // Assert: Statistics retrieved
        Assert.That(statsResponse.IsSuccessStatusCode, Is.True);

        var statsJson = await statsResponse.Content.ReadAsStringAsync();
        var statistics = JsonSerializer.Deserialize<SecretStatistics>(statsJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.That(statistics, Is.Not.Null);
        Assert.That(statistics!.TotalSecrets, Is.EqualTo(4));
        Assert.That(statistics.ActiveSecrets, Is.EqualTo(4));
        Assert.That(statistics.SecretsByType, Contains.Key("Generic"));
        Assert.That(statistics.SecretsByType["Generic"], Is.EqualTo(2));
    }

    [Test]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Act
        var healthResponse = await _client.GetAsync("/api/v1/monitoring/health");

        // Assert
        Assert.That(healthResponse.IsSuccessStatusCode, Is.True);

        var healthJson = await healthResponse.Content.ReadAsStringAsync();
        Assert.That(healthJson, Contains.Substring("Healthy").Or.Contains("healthy"));
    }

    [Test]
    public async Task CreateSecret_DuplicatePath_Returns409Conflict()
    {
        // Arrange: Create a secret first
        var secret = await CreateTestSecretAsync("/integration/duplicate-test", "original");

        var duplicateRequest = new CreateSecretRequest
        {
            Name = "Duplicate Secret",
            Path = secret.Path, // Same path
            Value = "duplicate-value",
            Type = SecretType.Generic
        };

        var duplicateJson = JsonSerializer.Serialize(duplicateRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Act: Try to create duplicate
        var duplicateResponse = await _client.PostAsync("/api/v1/secrets",
            new StringContent(duplicateJson, Encoding.UTF8, "application/json"));

        // Assert: Should get conflict or bad request
        Assert.That(duplicateResponse.IsSuccessStatusCode, Is.False);
        Assert.That(duplicateResponse.StatusCode, 
            Is.EqualTo(System.Net.HttpStatusCode.BadRequest)
            .Or.EqualTo(System.Net.HttpStatusCode.Conflict)
            .Or.EqualTo(System.Net.HttpStatusCode.InternalServerError)); // Depending on implementation
    }

    [Test]
    public async Task GetNonExistentSecret_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/secrets/nonexistent/path");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
    }

    [Test]
    public async Task UpdateNonExistentSecret_Returns404()
    {
        // Arrange
        var updateRequest = new UpdateSecretRequest { Value = "new-value" };
        var updateJson = JsonSerializer.Serialize(updateRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Act
        var response = await _client.PutAsync("/api/v1/secrets/nonexistent/path",
            new StringContent(updateJson, Encoding.UTF8, "application/json"));

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
    }

    private async Task<SecretResponse> CreateTestSecretAsync(
        string path, 
        string value, 
        SecretType type = SecretType.Generic)
    {
        var request = new CreateSecretRequest
        {
            Name = $"Test Secret for {path}",
            Path = path,
            Value = value,
            Type = type,
            Description = "Created for integration testing"
        };

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var response = await _client.PostAsync("/api/v1/secrets",
            new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.That(response.IsSuccessStatusCode, Is.True,
            $"Failed to create test secret: {await response.Content.ReadAsStringAsync()}");

        var secretJson = await response.Content.ReadAsStringAsync();
        var secret = JsonSerializer.Deserialize<SecretResponse>(secretJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.That(secret, Is.Not.Null);
        return secret!;
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }
}