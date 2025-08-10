using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NeoServiceLayer.Api;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using Xunit;

namespace NeoServiceLayer.Integration.Tests;

/// <summary>
/// Integration tests for Neo Service Layer microservices.
/// </summary>
public class ServiceIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ServiceIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Override services for testing
                services.AddSingleton<IServiceConfiguration, TestServiceConfiguration>();
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Health_Endpoint_Returns_Healthy()
    {
        // Arrange
        var request = "/health";

        // Act
        var response = await _client.GetAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }

    [Fact]
    public async Task Storage_Service_StoreAndRetrieve_Success()
    {
        // Arrange
        var key = $"test-key-{Guid.NewGuid()}";
        var data = "Test data content";
        var storeRequest = new
        {
            key = key,
            data = Convert.ToBase64String(Encoding.UTF8.GetBytes(data)),
            options = new
            {
                encrypt = true,
                compress = true
            },
            blockchainType = "NeoN3"
        };

        // Act - Store data
        var storeResponse = await _client.PostAsync(
            "/api/v1/storage/store",
            new StringContent(JsonSerializer.Serialize(storeRequest), Encoding.UTF8, "application/json"));

        // Assert - Store successful
        Assert.Equal(HttpStatusCode.OK, storeResponse.StatusCode);

        // Act - Retrieve data
        var getResponse = await _client.GetAsync($"/api/v1/storage/get/{key}?blockchainType=NeoN3");

        // Assert - Retrieve successful
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var retrievedContent = await getResponse.Content.ReadAsStringAsync();
        Assert.NotEmpty(retrievedContent);
    }

    [Fact]
    public async Task KeyManagement_Service_GenerateKey_Success()
    {
        // Arrange
        var request = new
        {
            keyType = "Secp256k1",
            keyName = $"test-key-{Guid.NewGuid()}",
            blockchainType = "NeoN3"
        };

        // Act
        var response = await _client.PostAsync(
            "/api/v1/keymanagement/generate",
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        Assert.True(result.TryGetProperty("keyId", out _));
    }

    [Fact]
    public async Task Configuration_Service_SetAndGet_Success()
    {
        // Arrange
        var key = $"test.config.{Guid.NewGuid()}";
        var value = "test-value";
        var setRequest = new
        {
            key = key,
            value = value,
            isEncrypted = false,
            blockchainType = "NeoN3"
        };

        // Act - Set configuration
        var setResponse = await _client.PostAsync(
            "/api/v1/configuration/set",
            new StringContent(JsonSerializer.Serialize(setRequest), Encoding.UTF8, "application/json"));

        // Assert - Set successful
        Assert.Equal(HttpStatusCode.OK, setResponse.StatusCode);

        // Act - Get configuration
        var getResponse = await _client.GetAsync($"/api/v1/configuration/get/{key}?blockchainType=NeoN3");

        // Assert - Get successful
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var content = await getResponse.Content.ReadAsStringAsync();
        Assert.Contains(value, content);
    }

    [Fact]
    public async Task CrossChain_Service_ValidateRoute_Success()
    {
        // Arrange
        var request = new
        {
            sourceChain = "NeoN3",
            targetChain = "NeoX",
            amount = "100",
            asset = "NEO"
        };

        // Act
        var response = await _client.PostAsync(
            "/api/v1/crosschain/validate-route",
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        Assert.True(result.TryGetProperty("isValid", out _));
    }

    [Fact]
    public async Task RateLimiting_Enforced()
    {
        // Arrange
        var requests = new List<Task<HttpResponseMessage>>();
        
        // Act - Send many requests quickly
        for (int i = 0; i < 150; i++)
        {
            requests.Add(_client.GetAsync("/api/v1/health"));
        }
        
        var responses = await Task.WhenAll(requests);
        
        // Assert - Some requests should be rate limited
        var rateLimitedCount = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        Assert.True(rateLimitedCount > 0, "Rate limiting should have been triggered");
    }

    [Fact]
    public async Task Security_Headers_Present()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/health");

        // Assert
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.True(response.Headers.Contains("X-XSS-Protection"));
        Assert.False(response.Headers.Contains("Server"));
    }
}

/// <summary>
/// Test service configuration for integration tests.
/// </summary>
public class TestServiceConfiguration : IServiceConfiguration
{
    private readonly Dictionary<string, object> _values = new()
    {
        ["Storage:MaxStorageItemCount"] = "1000",
        ["Storage:MaxStorageItemSizeBytes"] = "1048576",
        ["KeyManagement:MaxKeyCount"] = "100",
        ["Configuration:MaxConfigCount"] = "500"
    };

    public T? GetValue<T>(string key)
    {
        if (_values.TryGetValue(key, out var value))
        {
            return (T?)Convert.ChangeType(value, typeof(T));
        }
        return default(T);
    }

    public T GetValue<T>(string key, T defaultValue)
    {
        if (_values.TryGetValue(key, out var value))
        {
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    public void SetValue<T>(string key, T value)
    {
        _values[key] = value!;
    }

    public bool ContainsKey(string key)
    {
        return _values.ContainsKey(key);
    }

    public bool RemoveKey(string key)
    {
        return _values.Remove(key);
    }

    public IEnumerable<string> GetAllKeys()
    {
        return _values.Keys;
    }

    public IServiceConfiguration? GetSection(string sectionName)
    {
        var section = new TestServiceConfiguration();
        var prefix = sectionName + ":";
        foreach (var kvp in _values.Where(x => x.Key.StartsWith(prefix)))
        {
            var newKey = kvp.Key.Substring(prefix.Length);
            section._values[newKey] = kvp.Value;
        }
        return section._values.Any() ? section : null;
    }

    public string GetConnectionString(string name)
    {
        return GetValue<string>($"ConnectionStrings:{name}", "");
    }
}