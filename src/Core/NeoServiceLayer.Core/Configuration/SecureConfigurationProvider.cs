using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System;


namespace NeoServiceLayer.Core.Configuration
{
    /// <summary>
    /// Secure configuration provider that prioritizes environment variables and secure sources
    /// </summary>
    public class SecureConfigurationProvider : ISecureConfigurationProvider
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SecureConfigurationProvider> _logger;
        private readonly Dictionary<string, string> _cachedSecrets = new();

        public SecureConfigurationProvider(
            IConfiguration configuration,
            ILogger<SecureConfigurationProvider> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string?> GetSecureValueAsync(string key, string? defaultValue = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Configuration key cannot be empty", nameof(key));

            // Check environment variable first (highest priority)
            var envKey = key.Replace(":", "__").Replace(".", "__").ToUpper();
            var envValue = Environment.GetEnvironmentVariable(envKey);
            if (!string.IsNullOrEmpty(envValue))
            {
                _logger.LogDebug("Configuration '{Key}' loaded from environment variable", key);
                return envValue;
            }

            // Check cached secrets
            if (_cachedSecrets.TryGetValue(key, out var cachedValue))
            {
                return cachedValue;
            }

            // Check configuration (appsettings.json, etc.)
            var configValue = _configuration[key];
            if (!string.IsNullOrEmpty(configValue))
            {
                // Check if the value is a placeholder
                if (IsPlaceholder(configValue))
                {
                    _logger.LogWarning("Configuration '{Key}' contains placeholder value. Using default or throwing", key);

                    if (defaultValue != null)
                        return defaultValue;

                    throw new InvalidOperationException($"Configuration '{key}' not properly set. Found placeholder: {configValue}");
                }

                return configValue;
            }

            // Return default value
            if (defaultValue != null)
            {
                _logger.LogDebug("Configuration '{Key}' not found, using default value", key);
                return defaultValue;
            }

            _logger.LogWarning("Configuration '{Key}' not found and no default provided", key);
            return null;
        }

        public async Task<string?> GetConnectionStringAsync(string name)
        {
            // Check environment variable first
            var envKey = $"CONNECTIONSTRINGS__{name.ToUpper()}";
            var envValue = Environment.GetEnvironmentVariable(envKey);
            if (!string.IsNullOrEmpty(envValue))
            {
                _logger.LogDebug("Connection string '{Name}' loaded from environment variable", name);
                return envValue;
            }

            // Check configuration
            var connectionString = _configuration.GetConnectionString(name);
            if (!string.IsNullOrEmpty(connectionString))
            {
                if (IsPlaceholder(connectionString))
                {
                    throw new InvalidOperationException($"Connection string '{name}' contains placeholder value");
                }
                return connectionString;
            }

            _logger.LogWarning("Connection string '{Name}' not found", name);
            return null;
        }

        public async Task<string?> GetSecretAsync(string secretName)
        {
            // In production, this would integrate with Azure Key Vault, AWS Secrets Manager, etc.
            // For now, we'll use environment variables with a specific prefix
            var envKey = $"SECRET__{secretName.ToUpper()}";
            var secretValue = Environment.GetEnvironmentVariable(envKey);

            if (!string.IsNullOrEmpty(secretValue))
            {
                _logger.LogDebug("Secret '{SecretName}' loaded from secure source", secretName);
                _cachedSecrets[secretName] = secretValue;
                return secretValue;
            }

            // Check if there's a fallback in configuration (not recommended for production)
            var configKey = $"Secrets:{secretName}";
            var configValue = await GetSecureValueAsync(configKey);

            if (!string.IsNullOrEmpty(configValue))
            {
                _logger.LogWarning("Secret '{SecretName}' loaded from configuration. This should be moved to a secure source", secretName);
                return configValue;
            }

            _logger.LogError("Secret '{SecretName}' not found in any secure source", secretName);
            return null;
        }

        public async Task<bool> ExistsAsync(string key)
        {
            var value = await GetSecureValueAsync(key);
            return !string.IsNullOrEmpty(value);
        }

        private bool IsPlaceholder(string value)
        {
            // Common placeholder patterns
            var placeholders = new[]
            {
                "your-",
                "placeholder",
                "example",
                "changeme",
                "todo",
                "fixme",
                "xxx",
                "dummy",
                "test-",
                "sample",
                "demo",
                "<",
                "{{",
                "__"
            };

            var lowerValue = value.ToLower();
            foreach (var placeholder in placeholders)
            {
                if (lowerValue.Contains(placeholder))
                    return true;
            }

            // Check for localhost in production
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            if (environment.Equals("Production", StringComparison.OrdinalIgnoreCase) &&
                lowerValue.Contains("localhost"))
            {
                return true;
            }

            return false;
        }
    }
}
