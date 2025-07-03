using Microsoft.Extensions.Logging;

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
        ArgumentException.ThrowIfNullOrEmpty(key);

        lock (_lock)
        {
            if (!_values.TryGetValue(key, out var value))
            {
                _logger.LogDebug("Configuration key {Key} not found.", key);
                return default;
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
                return default;
            }
        }
    }

    /// <inheritdoc/>
    public T GetValue<T>(string key, T defaultValue)
    {
        var value = GetValue<T>(key);
        return value != null ? value : defaultValue;
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
    public bool ContainsKey(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        lock (_lock)
        {
            return _values.ContainsKey(key);
        }
    }

    /// <inheritdoc/>
    public bool RemoveKey(string key)
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
    public IEnumerable<string> GetAllKeys()
    {
        lock (_lock)
        {
            return _values.Keys.ToList();
        }
    }

    /// <inheritdoc/>
    public IServiceConfiguration? GetSection(string sectionName)
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
}
