using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.ServiceFramework;

/// <summary>
/// Implementation of the service configuration.
/// </summary>
public class ServiceConfiguration : IServiceConfiguration
{
    private readonly Dictionary<string, object> _values = new();
    private readonly Dictionary<string, ServiceConfiguration> _sections = new();
    private readonly ILogger<ServiceConfiguration> _logger;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceConfiguration"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public ServiceConfiguration(ILogger<ServiceConfiguration> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public T? GetValue<T>(string key)
    {
        return GetValue(key, default(T)!);
    }

    /// <inheritdoc/>
    public T GetValue<T>(string key, T defaultValue)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        lock (_lock)
        {
            if (!_values.TryGetValue(key, out var value))
            {
                _logger.LogDebug("Configuration key {Key} not found.", key);
                return defaultValue;
            }

            if (value is T typedValue)
            {
                return typedValue;
            }

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert configuration value for key {Key} to type {Type}.", key, typeof(T).Name);
                return defaultValue;
            }
        }
    }

    /// <inheritdoc/>
    public void SetValue<T>(string key, T value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        lock (_lock)
        {
            _values[key] = value!;
            _logger.LogDebug("Configuration key {Key} set to {Value}.", key, value);
        }
    }

    /// <inheritdoc/>
    public bool Exists(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        lock (_lock)
        {
            return _values.ContainsKey(key);
        }
    }

    /// <inheritdoc/>
    public bool ContainsKey(string key)
    {
        return Exists(key);
    }

    /// <inheritdoc/>
    public bool Remove(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        lock (_lock)
        {
            if (!_values.ContainsKey(key))
            {
                _logger.LogDebug("Configuration key {Key} not found.", key);
                return false;
            }

            _values.Remove(key);
            _logger.LogDebug("Configuration key {Key} removed.", key);
            return true;
        }
    }

    /// <inheritdoc/>
    public bool RemoveKey(string key)
    {
        return Remove(key);
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetAllKeys()
    {
        lock (_lock)
        {
            return _values.Keys.ToList();
        }
    }

    /// <summary>
    /// Gets a configuration subsection.
    /// </summary>
    public IServiceConfiguration? GetSubSection(string sectionName)
    {
        ArgumentException.ThrowIfNullOrEmpty(sectionName);

        lock (_lock)
        {
            if (!_sections.TryGetValue(sectionName, out var section))
            {
                section = new ServiceConfiguration(_logger);
                _sections[sectionName] = section;
            }

            return section;
        }
    }

    /// <inheritdoc/>
    public string GetConnectionString(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var connectionString = GetValue<string>($"ConnectionStrings:{name}");
        return connectionString ?? string.Empty;
    }

    /// <inheritdoc/>
    public bool HasValue(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        lock (_lock)
        {
            return _values.ContainsKey(key);
        }
    }

    /// <inheritdoc/>
    public IConfigurationSection? GetSection(string sectionName)
    {
        ArgumentException.ThrowIfNullOrEmpty(sectionName);

        // Return a configuration section wrapper
        return new ConfigurationSectionWrapper(this, sectionName);
    }

    /// <inheritdoc/>
    public void Reload()
    {
        lock (_lock)
        {
            _logger.LogInformation("Reloading configuration");
            // In a real implementation, this would reload from configuration sources
            // For now, we just clear the cache
            _values.Clear();
            _sections.Clear();
        }
    }

    /// <inheritdoc/>
    public IDictionary<string, object> GetAllValues(string? sectionKey = null)
    {
        lock (_lock)
        {
            if (string.IsNullOrEmpty(sectionKey))
            {
                return new Dictionary<string, object>(_values);
            }

            if (_sections.TryGetValue(sectionKey, out var section))
            {
                return section.GetAllValues();
            }

            return new Dictionary<string, object>();
        }
    }
}

/// <summary>
/// Wrapper for IConfigurationSection compatibility.
/// </summary>
internal class ConfigurationSectionWrapper : IConfigurationSection
{
    private readonly ServiceConfiguration _configuration;
    private readonly string _key;

    public ConfigurationSectionWrapper(ServiceConfiguration configuration, string key)
    {
        _configuration = configuration;
        _key = key;
    }

    public string Key => _key;
    public string Path => _key;
    public string? Value 
    { 
        get => _configuration.GetValue<string>(_key);
        set => _configuration.SetValue(_key, value);
    }

    public string? this[string key]
    {
        get => _configuration.GetValue<string>($"{_key}:{key}");
        set => _configuration.SetValue($"{_key}:{key}", value);
    }

    public IEnumerable<IConfigurationSection> GetChildren()
    {
        return Enumerable.Empty<IConfigurationSection>();
    }

    public IChangeToken GetReloadToken()
    {
        return new ReloadToken();
    }

    public IConfigurationSection GetSection(string key)
    {
        return new ConfigurationSectionWrapper(_configuration, $"{_key}:{key}");
    }
}

/// <summary>
/// Simple implementation of IChangeToken.
/// </summary>
internal class ReloadToken : IChangeToken
{
    public bool HasChanged => false;
    public bool ActiveChangeCallbacks => false;
    public IDisposable RegisterChangeCallback(Action<object?> callback, object? state)
    {
        return new NullDisposable();
    }
}

/// <summary>
/// Null disposable implementation.
/// </summary>
internal class NullDisposable : IDisposable
{
    public void Dispose() { }
}
