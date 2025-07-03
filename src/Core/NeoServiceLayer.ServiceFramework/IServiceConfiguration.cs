namespace NeoServiceLayer.ServiceFramework;

/// <summary>
/// Interface for service configuration.
/// </summary>
public interface IServiceConfiguration
{
    /// <summary>
    /// Gets a configuration value.
    /// </summary>
    /// <typeparam name="T">The type of the configuration value.</typeparam>
    /// <param name="key">The configuration key.</param>
    /// <returns>The configuration value, or default(T) if the key was not found.</returns>
    T? GetValue<T>(string key);

    /// <summary>
    /// Gets a configuration value.
    /// </summary>
    /// <typeparam name="T">The type of the configuration value.</typeparam>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">The default value to return if the key was not found.</param>
    /// <returns>The configuration value, or defaultValue if the key was not found.</returns>
    T GetValue<T>(string key, T defaultValue);

    /// <summary>
    /// Sets a configuration value.
    /// </summary>
    /// <typeparam name="T">The type of the configuration value.</typeparam>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The configuration value.</param>
    void SetValue<T>(string key, T value);

    /// <summary>
    /// Checks if a configuration key exists.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <returns>True if the key exists, false otherwise.</returns>
    bool ContainsKey(string key);

    /// <summary>
    /// Removes a configuration key.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <returns>True if the key was removed, false if it was not found.</returns>
    bool RemoveKey(string key);

    /// <summary>
    /// Gets all configuration keys.
    /// </summary>
    /// <returns>All configuration keys.</returns>
    IEnumerable<string> GetAllKeys();

    /// <summary>
    /// Gets a configuration section.
    /// </summary>
    /// <param name="sectionName">The section name.</param>
    /// <returns>The configuration section, or null if it was not found.</returns>
    IServiceConfiguration? GetSection(string sectionName);

    /// <summary>
    /// Gets a connection string.
    /// </summary>
    /// <param name="name">The connection string name.</param>
    /// <returns>The connection string, or empty string if not found.</returns>
    string GetConnectionString(string name);
}
