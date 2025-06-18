using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.ProofOfReserve;

/// <summary>
/// Configuration management for the Proof of Reserve Service.
/// </summary>
public partial class ProofOfReserveService
{
    /// <summary>
    /// Gets the monitoring interval from configuration.
    /// </summary>
    /// <returns>The monitoring interval.</returns>
    private TimeSpan GetMonitoringInterval()
    {
        try
        {
            if (_configurationService != null)
            {
                var config = _configurationService.Configuration;
                return TimeSpan.FromMinutes(config.Monitoring.IntervalMinutes);
            }

            // Default to 1 hour if no configuration service
            return TimeSpan.FromHours(1);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to get monitoring interval from configuration, using default");
            return TimeSpan.FromHours(1);
        }
    }

    /// <summary>
    /// Gets the maximum number of snapshots per asset from configuration.
    /// </summary>
    /// <returns>The maximum number of snapshots.</returns>
    private int GetMaxSnapshotsPerAsset()
    {
        try
        {
            if (_configurationService != null)
            {
                var config = _configurationService.Configuration;
                return config.Monitoring.MaxSnapshotsPerAsset;
            }

            return 1000; // Default
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to get max snapshots from configuration, using default");
            return 1000;
        }
    }

    /// <summary>
    /// Gets the resilience settings from configuration.
    /// </summary>
    /// <returns>The resilience settings.</returns>
    private (int MaxRetries, TimeSpan BaseDelay, bool Enabled) GetResilienceSettings()
    {
        try
        {
            if (_configurationService != null)
            {
                var config = _configurationService.Configuration;
                return (
                    config.Resilience.MaxRetries,
                    TimeSpan.FromMilliseconds(config.Resilience.BaseDelayMs),
                    config.Resilience.ResiliencePatternsEnabled
                );
            }

            return (3, TimeSpan.FromMilliseconds(200), true); // Defaults
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to get resilience settings from configuration, using defaults");
            return (3, TimeSpan.FromMilliseconds(200), true);
        }
    }

    /// <summary>
    /// Gets the alert settings from configuration.
    /// </summary>
    /// <returns>The alert settings.</returns>
    private (bool Enabled, decimal DefaultThreshold, TimeSpan CheckInterval) GetAlertSettings()
    {
        try
        {
            if (_configurationService != null)
            {
                var config = _configurationService.Configuration;
                return (
                    config.Alerts.AlertsEnabled,
                    config.Alerts.DefaultThreshold,
                    TimeSpan.FromMinutes(config.Alerts.CheckIntervalMinutes)
                );
            }

            return (true, 1.0m, TimeSpan.FromMinutes(5)); // Defaults
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to get alert settings from configuration, using defaults");
            return (true, 1.0m, TimeSpan.FromMinutes(5));
        }
    }

    /// <summary>
    /// Gets the storage settings from configuration.
    /// </summary>
    /// <returns>The storage settings.</returns>
    private (string EncryptionAlgorithm, bool CompressionEnabled, int RetentionDays) GetStorageSettings()
    {
        try
        {
            if (_configurationService != null)
            {
                var config = _configurationService.Configuration;
                return (
                    config.Storage.EncryptionAlgorithm,
                    config.Storage.CompressionEnabled,
                    config.Storage.ProofRetentionDays
                );
            }

            return ("AES-256-GCM", true, 365); // Defaults
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to get storage settings from configuration, using defaults");
            return ("AES-256-GCM", true, 365);
        }
    }

    /// <summary>
    /// Gets the cryptographic settings from configuration.
    /// </summary>
    /// <returns>The cryptographic settings.</returns>
    private (string SignatureAlgorithm, string HashAlgorithm, int KeySize) GetCryptographicSettings()
    {
        try
        {
            if (_configurationService != null)
            {
                var config = _configurationService.Configuration;
                return (
                    config.Cryptographic.SignatureAlgorithm,
                    config.Cryptographic.HashAlgorithm,
                    config.Cryptographic.KeySizeBits
                );
            }

            return ("ECDSA", "SHA256", 256); // Defaults
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to get cryptographic settings from configuration, using defaults");
            return ("ECDSA", "SHA256", 256);
        }
    }

    /// <summary>
    /// Gets the performance settings from configuration.
    /// </summary>
    /// <returns>The performance settings.</returns>
    private (int MaxConcurrentOps, int BatchSize, bool CachingEnabled, TimeSpan CacheExpiration) GetPerformanceSettings()
    {
        try
        {
            if (_configurationService != null)
            {
                var config = _configurationService.Configuration;
                return (
                    config.Performance.MaxConcurrentOperations,
                    config.Performance.BatchSize,
                    config.Performance.CachingEnabled,
                    TimeSpan.FromMinutes(config.Performance.CacheExpirationMinutes)
                );
            }

            return (10, 50, true, TimeSpan.FromMinutes(30)); // Defaults
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to get performance settings from configuration, using defaults");
            return (10, 50, true, TimeSpan.FromMinutes(30));
        }
    }

    /// <summary>
    /// Gets the environment settings from configuration.
    /// </summary>
    /// <returns>The environment settings.</returns>
    private (string Environment, bool IsProduction, bool DebugLogging) GetEnvironmentSettings()
    {
        try
        {
            if (_configurationService != null)
            {
                var config = _configurationService.Configuration;
                return (
                    config.Environment.Name,
                    config.Environment.IsProduction,
                    config.Environment.DebugLogging
                );
            }

            return ("Development", false, true); // Defaults
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to get environment settings from configuration, using defaults");
            return ("Development", false, true);
        }
    }

    /// <summary>
    /// Checks if a feature flag is enabled.
    /// </summary>
    /// <param name="featureName">The feature name.</param>
    /// <returns>True if the feature is enabled.</returns>
    private bool IsFeatureEnabled(string featureName)
    {
        try
        {
            if (_configurationService != null)
            {
                return _configurationService.IsFeatureEnabled(featureName);
            }

            return false; // Default to disabled
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to check feature flag {FeatureName}, defaulting to disabled", featureName);
            return false;
        }
    }

    /// <summary>
    /// Gets the blockchain query timeout from configuration.
    /// </summary>
    /// <returns>The blockchain query timeout.</returns>
    private TimeSpan GetBlockchainQueryTimeout()
    {
        try
        {
            if (_configurationService != null)
            {
                var config = _configurationService.Configuration;
                return TimeSpan.FromSeconds(config.Monitoring.BlockchainQueryTimeoutSeconds);
            }

            return TimeSpan.FromSeconds(30); // Default
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to get blockchain query timeout from configuration, using default");
            return TimeSpan.FromSeconds(30);
        }
    }

    /// <summary>
    /// Gets the circuit breaker settings from configuration.
    /// </summary>
    /// <returns>The circuit breaker settings.</returns>
    private (int FailureThreshold, TimeSpan Timeout) GetCircuitBreakerSettings()
    {
        try
        {
            if (_configurationService != null)
            {
                var config = _configurationService.Configuration;
                return (
                    config.Resilience.CircuitBreakerFailureThreshold,
                    TimeSpan.FromMinutes(config.Resilience.CircuitBreakerTimeoutMinutes)
                );
            }

            return (5, TimeSpan.FromMinutes(1)); // Defaults
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to get circuit breaker settings from configuration, using defaults");
            return (5, TimeSpan.FromMinutes(1));
        }
    }

    /// <summary>
    /// Handles configuration changes.
    /// </summary>
    /// <param name="newConfiguration">The new configuration.</param>
    private void OnConfigurationChanged(ProofOfReserveConfiguration newConfiguration)
    {
        try
        {
            Logger.LogInformation("Processing configuration changes for Proof of Reserve Service");

            // Update monitoring timer if interval changed
            var newInterval = TimeSpan.FromMinutes(newConfiguration.Monitoring.IntervalMinutes);
            var currentInterval = GetMonitoringInterval();

            if (newInterval != currentInterval)
            {
                Logger.LogInformation("Updating monitoring interval from {OldInterval} to {NewInterval}",
                    currentInterval, newInterval);

                // Note: Timer interval changes would require recreating the timer
                // For simplicity, we'll log this but not implement dynamic timer recreation
                Logger.LogWarning("Monitoring interval change detected but requires service restart to take effect");
            }

            // Update circuit breaker settings
            UpdateCircuitBreakerConfiguration(newConfiguration);

            // Update alert settings
            UpdateAlertConfiguration(newConfiguration);

            // Update performance settings
            UpdatePerformanceConfiguration(newConfiguration);

            Logger.LogInformation("Configuration changes processed successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing configuration changes");
        }
    }

    /// <summary>
    /// Updates circuit breaker configuration.
    /// </summary>
    /// <param name="configuration">The new configuration.</param>
    private void UpdateCircuitBreakerConfiguration(ProofOfReserveConfiguration configuration)
    {
        try
        {
            // Reset circuit breakers with new settings if needed
            if (configuration.Resilience.ResiliencePatternsEnabled)
            {
                Logger.LogDebug("Resilience patterns enabled with new configuration");
                // Circuit breakers will use new settings on next creation
            }
            else
            {
                Logger.LogWarning("Resilience patterns have been disabled");
                // Could reset all circuit breakers here if needed
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating circuit breaker configuration");
        }
    }

    /// <summary>
    /// Updates alert configuration.
    /// </summary>
    /// <param name="configuration">The new configuration.</param>
    private void UpdateAlertConfiguration(ProofOfReserveConfiguration configuration)
    {
        try
        {
            if (!configuration.Alerts.AlertsEnabled)
            {
                Logger.LogWarning("Alerts have been disabled");
            }
            else
            {
                Logger.LogDebug("Alert configuration updated - Threshold: {Threshold}, Interval: {Interval}min",
                    configuration.Alerts.DefaultThreshold, configuration.Alerts.CheckIntervalMinutes);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating alert configuration");
        }
    }

    /// <summary>
    /// Updates performance configuration.
    /// </summary>
    /// <param name="configuration">The new configuration.</param>
    private void UpdatePerformanceConfiguration(ProofOfReserveConfiguration configuration)
    {
        try
        {
            Logger.LogDebug("Performance configuration updated - Caching: {CachingEnabled}, Batch Size: {BatchSize}",
                configuration.Performance.CachingEnabled, configuration.Performance.BatchSize);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating performance configuration");
        }
    }

    /// <summary>
    /// Gets the configuration summary for monitoring.
    /// </summary>
    /// <returns>The configuration summary.</returns>
    public ConfigurationSummary? GetConfigurationSummary()
    {
        try
        {
            return _configurationService?.GetConfigurationSummary();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting configuration summary");
            return null;
        }
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The validation result.</returns>
    public ConfigurationValidationResult? ValidateCurrentConfiguration()
    {
        try
        {
            return _configurationService?.ValidateConfiguration();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error validating current configuration");
            return null;
        }
    }
}