using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NeoServiceLayer.Services.ProofOfReserve;

/// <summary>
/// Configuration service for the Proof of Reserve Service with validation and hot reload support.
/// </summary>
public class ProofOfReserveConfigurationService : IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProofOfReserveConfigurationService> _logger;
    private readonly IDisposable? _configurationChangeToken;
    private ProofOfReserveConfiguration _currentConfiguration;
    private readonly object _configurationLock = new();
    private readonly List<Action<ProofOfReserveConfiguration>> _changeCallbacks = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ProofOfReserveConfigurationService"/> class.
    /// </summary>
    /// <param name="configuration">The configuration root.</param>
    /// <param name="logger">The logger.</param>
    public ProofOfReserveConfigurationService(
        IConfiguration configuration,
        ILogger<ProofOfReserveConfigurationService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Load initial configuration
        _currentConfiguration = LoadConfiguration();

        // Validate initial configuration
        ValidateConfiguration(_currentConfiguration);

        // Set up hot reload
        _configurationChangeToken = _configuration.GetReloadToken().RegisterChangeCallback(OnConfigurationChanged, null);

        _logger.LogInformation("Proof of Reserve configuration service initialized");
    }

    /// <summary>
    /// Gets the current configuration.
    /// </summary>
    public ProofOfReserveConfiguration Configuration
    {
        get
        {
            lock (_configurationLock)
            {
                return _currentConfiguration;
            }
        }
    }

    /// <summary>
    /// Event raised when configuration changes.
    /// </summary>
    public event Action<ProofOfReserveConfiguration>? ConfigurationChanged;

    /// <summary>
    /// Registers a callback to be invoked when configuration changes.
    /// </summary>
    /// <param name="callback">The callback to register.</param>
    public void RegisterChangeCallback(Action<ProofOfReserveConfiguration> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        lock (_configurationLock)
        {
            _changeCallbacks.Add(callback);
        }
    }

    /// <summary>
    /// Unregisters a change callback.
    /// </summary>
    /// <param name="callback">The callback to unregister.</param>
    public void UnregisterChangeCallback(Action<ProofOfReserveConfiguration> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        lock (_configurationLock)
        {
            _changeCallbacks.Remove(callback);
        }
    }

    /// <summary>
    /// Validates the configuration and returns validation results.
    /// </summary>
    /// <param name="configuration">The configuration to validate.</param>
    /// <returns>Validation results.</returns>
    public ConfigurationValidationResult ValidateConfiguration(ProofOfReserveConfiguration? configuration = null)
    {
        configuration ??= Configuration;

        var validationResults = configuration.Validate().ToList();
        var isValid = validationResults.Count == 0;

        if (!isValid)
        {
            _logger.LogWarning("Configuration validation failed with {ErrorCount} errors", validationResults.Count);
            foreach (var result in validationResults)
            {
                _logger.LogWarning("Configuration validation error: {Error}", result.ErrorMessage);
            }
        }
        else
        {
            _logger.LogDebug("Configuration validation passed");
        }

        return new ConfigurationValidationResult
        {
            IsValid = isValid,
            ValidationResults = validationResults,
            Configuration = configuration
        };
    }

    /// <summary>
    /// Reloads the configuration from the configuration providers.
    /// </summary>
    /// <returns>True if the configuration was successfully reloaded.</returns>
    public bool ReloadConfiguration()
    {
        try
        {
            _logger.LogInformation("Manually reloading Proof of Reserve configuration");

            var newConfiguration = LoadConfiguration();
            var validationResult = ValidateConfiguration(newConfiguration);

            if (!validationResult.IsValid)
            {
                _logger.LogError("Configuration reload failed due to validation errors");
                return false;
            }

            UpdateConfiguration(newConfiguration);
            _logger.LogInformation("Configuration reloaded successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload configuration");
            return false;
        }
    }

    /// <summary>
    /// Gets configuration summary for monitoring and diagnostics.
    /// </summary>
    /// <returns>Configuration summary.</returns>
    public ConfigurationSummary GetConfigurationSummary()
    {
        var config = Configuration;

        return new ConfigurationSummary
        {
            Environment = config.Environment.Name,
            IsProduction = config.Environment.IsProduction,
            MonitoringIntervalMinutes = config.Monitoring.IntervalMinutes,
            ResilienceEnabled = config.Resilience.ResiliencePatternsEnabled,
            AlertsEnabled = config.Alerts.AlertsEnabled,
            CachingEnabled = config.Performance.CachingEnabled,
            EncryptionAlgorithm = config.Storage.EncryptionAlgorithm,
            SignatureAlgorithm = config.Cryptographic.SignatureAlgorithm,
            FeatureFlags = config.Environment.FeatureFlags.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList(),
            LastValidated = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Gets the configuration value for a specific path.
    /// </summary>
    /// <typeparam name="T">The type of the configuration value.</typeparam>
    /// <param name="path">The configuration path.</param>
    /// <param name="defaultValue">The default value if not found.</param>
    /// <returns>The configuration value.</returns>
    public T GetConfigurationValue<T>(string path, T defaultValue = default!)
    {
        try
        {
            var fullPath = $"{ProofOfReserveConfiguration.SectionName}:{path}";
            var value = _configuration.GetValue<T>(fullPath);
            return value ?? defaultValue;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get configuration value for path: {Path}", path);
            return defaultValue;
        }
    }

    /// <summary>
    /// Checks if a feature flag is enabled.
    /// </summary>
    /// <param name="featureName">The feature flag name.</param>
    /// <returns>True if the feature is enabled.</returns>
    public bool IsFeatureEnabled(string featureName)
    {
        var config = Configuration;
        return config.Environment.FeatureFlags.TryGetValue(featureName, out var enabled) && enabled;
    }

    /// <summary>
    /// Updates environment-specific settings at runtime.
    /// </summary>
    /// <param name="updates">Dictionary of setting updates.</param>
    /// <returns>True if updates were applied successfully.</returns>
    public bool UpdateRuntimeSettings(Dictionary<string, object> updates)
    {
        try
        {
            lock (_configurationLock)
            {
                var config = _currentConfiguration;
                bool hasChanges = false;

                foreach (var update in updates)
                {
                    switch (update.Key.ToLowerInvariant())
                    {
                        case "monitoring.intervalminutes" when update.Value is int interval:
                            if (interval >= 1 && interval <= 1440)
                            {
                                config.Monitoring.IntervalMinutes = interval;
                                hasChanges = true;
                            }
                            break;

                        case "alerts.checkintervalminutes" when update.Value is int alertInterval:
                            if (alertInterval >= 1 && alertInterval <= 60)
                            {
                                config.Alerts.CheckIntervalMinutes = alertInterval;
                                hasChanges = true;
                            }
                            break;

                        case "performance.cachingenabled" when update.Value is bool caching:
                            config.Performance.CachingEnabled = caching;
                            hasChanges = true;
                            break;

                        case "environment.debuglogging" when update.Value is bool debug:
                            config.Environment.DebugLogging = debug;
                            hasChanges = true;
                            break;

                        default:
                            _logger.LogWarning("Unknown runtime setting: {Setting}", update.Key);
                            break;
                    }
                }

                if (hasChanges)
                {
                    var validationResult = ValidateConfiguration(config);
                    if (validationResult.IsValid)
                    {
                        NotifyConfigurationChanged(config);
                        _logger.LogInformation("Runtime settings updated successfully");
                        return true;
                    }
                    else
                    {
                        _logger.LogError("Runtime settings update failed validation");
                        return false;
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update runtime settings");
            return false;
        }
    }

    /// <summary>
    /// Loads configuration from the configuration providers.
    /// </summary>
    /// <returns>The loaded configuration.</returns>
    private ProofOfReserveConfiguration LoadConfiguration()
    {
        var configuration = new ProofOfReserveConfiguration();

        try
        {
            _configuration.GetSection(ProofOfReserveConfiguration.SectionName).Bind(configuration);
            _logger.LogDebug("Configuration loaded from section: {SectionName}", ProofOfReserveConfiguration.SectionName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to bind configuration section, using defaults");
        }

        return configuration;
    }

    /// <summary>
    /// Handles configuration change events.
    /// </summary>
    /// <param name="state">The state object.</param>
    private void OnConfigurationChanged(object? state)
    {
        try
        {
            _logger.LogInformation("Configuration change detected, reloading...");

            var newConfiguration = LoadConfiguration();
            var validationResult = ValidateConfiguration(newConfiguration);

            if (validationResult.IsValid)
            {
                UpdateConfiguration(newConfiguration);
                _logger.LogInformation("Configuration hot reload completed successfully");
            }
            else
            {
                _logger.LogError("Configuration hot reload failed validation, keeping current configuration");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during configuration hot reload");
        }
    }

    /// <summary>
    /// Updates the current configuration and notifies subscribers.
    /// </summary>
    /// <param name="newConfiguration">The new configuration.</param>
    private void UpdateConfiguration(ProofOfReserveConfiguration newConfiguration)
    {
        lock (_configurationLock)
        {
            _currentConfiguration = newConfiguration;
        }

        NotifyConfigurationChanged(newConfiguration);
    }

    /// <summary>
    /// Notifies all registered callbacks about configuration changes.
    /// </summary>
    /// <param name="configuration">The updated configuration.</param>
    private void NotifyConfigurationChanged(ProofOfReserveConfiguration configuration)
    {
        try
        {
            ConfigurationChanged?.Invoke(configuration);

            lock (_configurationLock)
            {
                foreach (var callback in _changeCallbacks)
                {
                    try
                    {
                        callback(configuration);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in configuration change callback");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying configuration changes");
        }
    }

    /// <summary>
    /// Disposes the configuration service.
    /// </summary>
    public void Dispose()
    {
        _configurationChangeToken?.Dispose();

        lock (_configurationLock)
        {
            _changeCallbacks.Clear();
        }
    }
}

/// <summary>
/// Configuration validation result.
/// </summary>
public class ConfigurationValidationResult
{
    /// <summary>
    /// Gets or sets whether the configuration is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the validation results.
    /// </summary>
    public List<ValidationResult> ValidationResults { get; set; } = new();

    /// <summary>
    /// Gets or sets the configuration that was validated.
    /// </summary>
    public ProofOfReserveConfiguration? Configuration { get; set; }
}

/// <summary>
/// Configuration summary for monitoring.
/// </summary>
public class ConfigurationSummary
{
    /// <summary>
    /// Gets or sets the environment name.
    /// </summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this is production.
    /// </summary>
    public bool IsProduction { get; set; }

    /// <summary>
    /// Gets or sets the monitoring interval.
    /// </summary>
    public int MonitoringIntervalMinutes { get; set; }

    /// <summary>
    /// Gets or sets whether resilience is enabled.
    /// </summary>
    public bool ResilienceEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether alerts are enabled.
    /// </summary>
    public bool AlertsEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether caching is enabled.
    /// </summary>
    public bool CachingEnabled { get; set; }

    /// <summary>
    /// Gets or sets the encryption algorithm.
    /// </summary>
    public string EncryptionAlgorithm { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the signature algorithm.
    /// </summary>
    public string SignatureAlgorithm { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the enabled feature flags.
    /// </summary>
    public List<string> FeatureFlags { get; set; } = new();

    /// <summary>
    /// Gets or sets when this summary was generated.
    /// </summary>
    public DateTime LastValidated { get; set; }
}
