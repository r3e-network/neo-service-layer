using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Infrastructure.Secrets.Providers;

/// <summary>
/// Environment variable based secret provider for development/testing.
/// </summary>
public class EnvironmentSecretProvider : ISecretProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EnvironmentSecretProvider> _logger;
    private readonly string _prefix;

    public EnvironmentSecretProvider(
        IConfiguration configuration,
        ILogger<EnvironmentSecretProvider> logger,
        string prefix = "NSL_SECRET_")
    {
        _configuration = configuration;
        _logger = logger;
        _prefix = prefix;
    }

    public Task<string?> GetSecretAsync(string key)
    {
        var envKey = $"{_prefix}{key.ToUpperInvariant().Replace(":", "_")}";
        var value = Environment.GetEnvironmentVariable(envKey) ?? _configuration[key];
        
        if (string.IsNullOrEmpty(value))
        {
            _logger.LogWarning("Secret {Key} not found in environment or configuration", key);
        }
        
        return Task.FromResult(value);
    }

    public Task<bool> SetSecretAsync(string key, string value)
    {
        var envKey = $"{_prefix}{key.ToUpperInvariant().Replace(":", "_")}";
        Environment.SetEnvironmentVariable(envKey, value);
        _logger.LogInformation("Secret {Key} set in environment", key);
        return Task.FromResult(true);
    }

    public Task<bool> DeleteSecretAsync(string key)
    {
        var envKey = $"{_prefix}{key.ToUpperInvariant().Replace(":", "_")}";
        Environment.SetEnvironmentVariable(envKey, null);
        _logger.LogInformation("Secret {Key} removed from environment", key);
        return Task.FromResult(true);
    }

    public Task<bool> ExistsAsync(string key)
    {
        var envKey = $"{_prefix}{key.ToUpperInvariant().Replace(":", "_")}";
        var exists = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envKey)) || 
                     !string.IsNullOrEmpty(_configuration[key]);
        return Task.FromResult(exists);
    }
}