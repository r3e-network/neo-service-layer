using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Core.Configuration;

/// <summary>
/// Exception thrown when configuration operations fail.
/// </summary>
public class ConfigurationException : Exception
{
    public ConfigurationException(string message) : base(message) { }
    public ConfigurationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Service configuration implementation with secure secrets management.
/// </summary>
public class ServiceConfiguration : IServiceConfiguration
{
    private readonly IConfiguration _configuration;
    private readonly ISecretsManager? _secretsManager;
    private readonly ILogger<ServiceConfiguration> _logger;
    private readonly Dictionary<string, object> _cachedValues = new();
    private readonly object _lockObject = new();

    public ServiceConfiguration(
        IConfiguration configuration, 
        ILogger<ServiceConfiguration> logger,
        ISecretsManager? secretsManager = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _secretsManager = secretsManager;
    }

    /// <inheritdoc/>
    public T GetValue<T>(string key, T defaultValue = default!)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        try
        {
            lock (_lockObject)
            {
                // Check cache first
                if (_cachedValues.TryGetValue(key, out var cachedValue) && cachedValue is T cached)
                {
                    return cached;
                }

                // Try to get from secrets manager first for sensitive keys
                if (_secretsManager != null && IsSensitiveKey(key))
                {
                    var secret = _secretsManager.GetSecretAsync(key).GetAwaiter().GetResult();
                    if (!string.IsNullOrEmpty(secret))
                    {
                        var convertedSecret = ConvertValue<T>(secret);
                        _cachedValues[key] = convertedSecret!;
                        return convertedSecret;
                    }
                }

                // Get from configuration
                var value = _configuration.GetValue<T>(key, defaultValue);
                
                // Cache non-sensitive values
                if (!IsSensitiveKey(key))
                {
                    _cachedValues[key] = value!;
                }

                return value;
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation while getting configuration value for key: {Key}", key);
            return defaultValue;
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Format error while converting configuration value for key: {Key}", key);
            return defaultValue;
        }
        catch (Exception ex) when (!(ex is ArgumentException))
        {
            _logger.LogError(ex, "Unexpected error while getting configuration value for key: {Key}", key);
            return defaultValue;
        }
    }

    /// <inheritdoc/>
    public void SetValue<T>(string key, T value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        try
        {
            lock (_lockObject)
            {
                if (IsSensitiveKey(key) && _secretsManager != null)
                {
                    // Store sensitive values in secrets manager
                    var stringValue = value?.ToString() ?? "";
                    _secretsManager.SetSecretAsync(key, stringValue).GetAwaiter().GetResult();
                }
                else
                {
                    // Store in cache for non-sensitive values
                    _cachedValues[key] = value!;
                }
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation while setting configuration value for key: {Key}", key);
            throw new ConfigurationException($"Cannot set configuration value for key '{key}'", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied while setting configuration value for key: {Key}", key);
            throw new ConfigurationException($"Access denied for configuration key '{key}'", ex);
        }
        catch (Exception ex) when (!(ex is ArgumentException))
        {
            _logger.LogError(ex, "Unexpected error while setting configuration value for key: {Key}", key);
            throw new ConfigurationException($"Failed to set configuration value for key '{key}'", ex);
        }
    }

    /// <inheritdoc/>
    public bool HasValue(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        try
        {
            lock (_lockObject)
            {
                // Check cache
                if (_cachedValues.ContainsKey(key))
                    return true;

                // Check secrets manager for sensitive keys
                if (_secretsManager != null && IsSensitiveKey(key))
                {
                    var secret = _secretsManager.GetSecretAsync(key).GetAwaiter().GetResult();
                    return !string.IsNullOrEmpty(secret);
                }

                // Check configuration
                var section = _configuration.GetSection(key);
                return section.Exists();
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation while checking configuration key existence: {Key}", key);
            return false;
        }
        catch (Exception ex) when (!(ex is ArgumentException))
        {
            _logger.LogError(ex, "Unexpected error while checking configuration key existence: {Key}", key);
            return false;
        }
    }

    /// <inheritdoc/>
    public IConfigurationSection GetSection(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        return _configuration.GetSection(key);
    }

    /// <inheritdoc/>
    public void Reload()
    {
        try
        {
            lock (_lockObject)
            {
                // Clear cache to force reload from sources
                _cachedValues.Clear();
                
                // Reload configuration if supported
                if (_configuration is IConfigurationRoot root)
                {
                    root.Reload();
                }

                _logger.LogInformation("Configuration reloaded successfully");
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation during configuration reload");
            throw new ConfigurationException("Configuration reload failed due to invalid operation", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied during configuration reload");
            throw new ConfigurationException("Configuration reload failed due to access restrictions", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during configuration reload");
            throw new ConfigurationException("Configuration reload failed", ex);
        }
    }

    /// <inheritdoc/>
    public IDictionary<string, object> GetAllValues(string? sectionKey = null)
    {
        var result = new Dictionary<string, object>();

        try
        {
            IConfiguration section = string.IsNullOrWhiteSpace(sectionKey) 
                ? _configuration 
                : _configuration.GetSection(sectionKey);

            foreach (var child in section.GetChildren())
            {
                if (child.Value != null && !IsSensitiveKey(child.Key))
                {
                    result[child.Key] = child.Value;
                }
                else if (child.GetChildren().Any())
                {
                    // Recursively get nested values
                    var nestedValues = GetAllValues(child.Path);
                    foreach (var nested in nestedValues)
                    {
                        result[nested.Key] = nested.Value;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all configuration values");
        }

        return result;
    }

    /// <summary>
    /// Determines if a configuration key contains sensitive information.
    /// </summary>
    private static bool IsSensitiveKey(string key)
    {
        var sensitiveKeywords = new[]
        {
            "password", "pwd", "secret", "key", "token", "credential",
            "connectionstring", "connstr", "apikey", "privatekey",
            "certificate", "cert", "auth", "oauth", "jwt"
        };

        var lowerKey = key.ToLowerInvariant();
        return Array.Exists(sensitiveKeywords, keyword => lowerKey.Contains(keyword));
    }

    /// <summary>
    /// Converts a string value to the specified type.
    /// </summary>
    private static T ConvertValue<T>(string value)
    {
        try
        {
            var targetType = typeof(T);
            
            if (targetType == typeof(string))
                return (T)(object)value;
            
            if (targetType == typeof(bool) || targetType == typeof(bool?))
                return (T)(object)bool.Parse(value);
            
            if (targetType == typeof(int) || targetType == typeof(int?))
                return (T)(object)int.Parse(value);
            
            if (targetType == typeof(long) || targetType == typeof(long?))
                return (T)(object)long.Parse(value);
            
            if (targetType == typeof(double) || targetType == typeof(double?))
                return (T)(object)double.Parse(value);
            
            if (targetType == typeof(decimal) || targetType == typeof(decimal?))
                return (T)(object)decimal.Parse(value);
            
            if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
                return (T)(object)DateTime.Parse(value);
            
            if (targetType == typeof(TimeSpan) || targetType == typeof(TimeSpan?))
                return (T)(object)TimeSpan.Parse(value);
            
            if (targetType == typeof(Guid) || targetType == typeof(Guid?))
                return (T)(object)Guid.Parse(value);

            // Try generic conversion
            return (T)Convert.ChangeType(value, targetType);
        }
        catch (FormatException)
        {
            return default!;
        }
        catch (InvalidCastException)
        {
            return default!;
        }
        catch (OverflowException)
        {
            return default!;
        }
        catch (ArgumentException)
        {
            return default!;
        }
    }
}

/// <summary>
/// Secrets manager interface for secure configuration values.
/// </summary>
public interface ISecretsManager
{
    Task<string?> GetSecretAsync(string key);
    Task SetSecretAsync(string key, string value);
    Task<bool> HasSecretAsync(string key);
    Task DeleteSecretAsync(string key);
}

/// <summary>
/// Simple in-memory secrets manager (for development/testing).
/// In production, this should be replaced with Azure Key Vault, AWS Secrets Manager, etc.
/// </summary>
public class SecretsManager : ISecretsManager
{
    private readonly Dictionary<string, string> _secrets = new();
    private readonly object _lockObject = new();
    private readonly ILogger<SecretsManager> _logger;

    public SecretsManager(ILogger<SecretsManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<string?> GetSecretAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return Task.FromResult<string?>(null);

        lock (_lockObject)
        {
            _secrets.TryGetValue(key, out var value);
            return Task.FromResult(value);
        }
    }

    public Task SetSecretAsync(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        lock (_lockObject)
        {
            _secrets[key] = value ?? "";
            _logger.LogDebug("Secret set for key: {Key}", key);
        }

        return Task.CompletedTask;
    }

    public Task<bool> HasSecretAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return Task.FromResult(false);

        lock (_lockObject)
        {
            return Task.FromResult(_secrets.ContainsKey(key));
        }
    }

    public Task DeleteSecretAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return Task.CompletedTask;

        lock (_lockObject)
        {
            if (_secrets.Remove(key))
            {
                _logger.LogDebug("Secret deleted for key: {Key}", key);
            }
        }

        return Task.CompletedTask;
    }
}