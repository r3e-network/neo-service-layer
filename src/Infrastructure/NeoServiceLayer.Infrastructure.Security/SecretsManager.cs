using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Infrastructure.Security;

/// <summary>
/// Production-ready secrets management with support for multiple providers
/// </summary>
public class SecretsManager : ISecretsManager
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SecretsManager> _logger;
    private readonly Dictionary<string, string> _secretsCache = new();
    private readonly object _cacheLock = new();
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);
    private DateTime _lastCacheRefresh = DateTime.MinValue;

    public SecretsManager(IConfiguration configuration, ILogger<SecretsManager> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GetSecretAsync(string secretName)
    {
        // Check cache first
        if (TryGetFromCache(secretName, out var cachedSecret))
        {
            return cachedSecret;
        }

        // Determine secret provider
        var provider = _configuration["Secrets:Provider"] ?? "Environment";
        
        string secretValue = provider switch
        {
            "AzureKeyVault" => await GetFromAzureKeyVaultAsync(secretName).ConfigureAwait(false),
            "AWSSecretsManager" => await GetFromAWSSecretsManagerAsync(secretName).ConfigureAwait(false),
            "Environment" => GetFromEnvironment(secretName),
            _ => throw new NotSupportedException($"Secret provider '{provider}' is not supported")
        };

        if (string.IsNullOrEmpty(secretValue))
        {
            throw new InvalidOperationException($"Secret '{secretName}' not found");
        }

        // Cache the secret
        CacheSecret(secretName, secretValue);

        return secretValue;
    }

    public async Task<T> GetSecretAsync<T>(string secretName) where T : class
    {
        var secretValue = await GetSecretAsync(secretName).ConfigureAwait(false);
        
        try
        {
            return JsonSerializer.Deserialize<T>(secretValue) 
                ?? throw new InvalidOperationException($"Failed to deserialize secret '{secretName}'");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize secret {SecretName} to type {Type}", secretName, typeof(T).Name);
            throw;
        }
    }

    public async Task SetSecretAsync(string secretName, string secretValue)
    {
        var provider = _configuration["Secrets:Provider"] ?? "Environment";
        
        switch (provider)
        {
            case "AzureKeyVault":
                await SetInAzureKeyVaultAsync(secretName, secretValue).ConfigureAwait(false);
                break;
            case "AWSSecretsManager":
                await SetInAWSSecretsManagerAsync(secretName, secretValue).ConfigureAwait(false);
                break;
            case "Environment":
                throw new NotSupportedException("Cannot set environment variables at runtime");
            default:
                throw new NotSupportedException($"Secret provider '{provider}' is not supported");
        }

        // Update cache
        CacheSecret(secretName, secretValue);
    }

    public async Task<bool> SecretExistsAsync(string secretName)
    {
        try
        {
            var secret = await GetSecretAsync(secretName).ConfigureAwait(false);
            return !string.IsNullOrEmpty(secret);
        }
        catch
        {
            return false;
        }
    }

    public void ClearCache()
    {
        lock (_cacheLock)
        {
            _secretsCache.Clear();
            _lastCacheRefresh = DateTime.MinValue;
        }
    }

    private bool TryGetFromCache(string secretName, out string secretValue)
    {
        lock (_cacheLock)
        {
            if (DateTime.UtcNow - _lastCacheRefresh > _cacheExpiration)
            {
                _secretsCache.Clear();
                _lastCacheRefresh = DateTime.UtcNow;
                secretValue = string.Empty;
                return false;
            }

            return _secretsCache.TryGetValue(secretName, out secretValue);
        }
    }

    private void CacheSecret(string secretName, string secretValue)
    {
        lock (_cacheLock)
        {
            _secretsCache[secretName] = secretValue;
        }
    }

    private string GetFromEnvironment(string secretName)
    {
        // Convert secret name to environment variable format
        var envVarName = secretName.Replace(":", "_").ToUpperInvariant();
        var value = Environment.GetEnvironmentVariable(envVarName);
        
        if (string.IsNullOrEmpty(value))
        {
            // Try configuration as fallback
            value = _configuration[secretName];
        }

        return value ?? string.Empty;
    }

    private async Task<string> GetFromAzureKeyVaultAsync(string secretName)
    {
        var keyVaultUrl = _configuration["Secrets:AzureKeyVault:Url"] 
            ?? throw new InvalidOperationException("Azure Key Vault URL not configured");

        var client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
        
        try
        {
            var response = await client.GetSecretAsync(secretName).ConfigureAwait(false);
            return response.Value.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve secret {SecretName} from Azure Key Vault", secretName);
            throw;
        }
    }

    private async Task SetInAzureKeyVaultAsync(string secretName, string secretValue)
    {
        var keyVaultUrl = _configuration["Secrets:AzureKeyVault:Url"] 
            ?? throw new InvalidOperationException("Azure Key Vault URL not configured");

        var client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
        
        try
        {
            await client.SetSecretAsync(secretName, secretValue).ConfigureAwait(false);
            _logger.LogInformation("Successfully stored secret {SecretName} in Azure Key Vault", secretName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store secret {SecretName} in Azure Key Vault", secretName);
            throw;
        }
    }

    private async Task<string> GetFromAWSSecretsManagerAsync(string secretName)
    {
        var region = _configuration["Secrets:AWS:Region"] ?? "us-east-1";
        using var client = new AmazonSecretsManagerClient(Amazon.RegionEndpoint.GetBySystemName(region));
        
        var request = new GetSecretValueRequest
        {
            SecretId = secretName
        };

        try
        {
            var response = await client.GetSecretValueAsync(request).ConfigureAwait(false);
            return response.SecretString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve secret {SecretName} from AWS Secrets Manager", secretName);
            throw;
        }
    }

    private async Task SetInAWSSecretsManagerAsync(string secretName, string secretValue)
    {
        var region = _configuration["Secrets:AWS:Region"] ?? "us-east-1";
        using var client = new AmazonSecretsManagerClient(Amazon.RegionEndpoint.GetBySystemName(region));
        
        try
        {
            // Try to update existing secret
            var updateRequest = new UpdateSecretRequest
            {
                SecretId = secretName,
                SecretString = secretValue
            };
            
            await client.UpdateSecretAsync(updateRequest).ConfigureAwait(false);
            _logger.LogInformation("Successfully updated secret {SecretName} in AWS Secrets Manager", secretName);
        }
        catch (ResourceNotFoundException)
        {
            // Create new secret if it doesn't exist
            var createRequest = new CreateSecretRequest
            {
                Name = secretName,
                SecretString = secretValue
            };
            
            await client.CreateSecretAsync(createRequest).ConfigureAwait(false);
            _logger.LogInformation("Successfully created secret {SecretName} in AWS Secrets Manager", secretName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store secret {SecretName} in AWS Secrets Manager", secretName);
            throw;
        }
    }
}