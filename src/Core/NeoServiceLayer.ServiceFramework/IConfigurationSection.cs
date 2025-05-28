namespace NeoServiceLayer.ServiceFramework;

/// <summary>
/// Interface for configuration sections.
/// </summary>
public interface IConfigurationSection
{
    /// <summary>
    /// Gets the key of this section.
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Gets the path of this section.
    /// </summary>
    string Path { get; }

    /// <summary>
    /// Gets the value of this section.
    /// </summary>
    string? Value { get; }

    /// <summary>
    /// Gets a configuration value.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The configuration value.</returns>
    string? this[string key] { get; }

    /// <summary>
    /// Gets a child section.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The child section.</returns>
    IConfigurationSection GetSection(string key);

    /// <summary>
    /// Gets all child sections.
    /// </summary>
    /// <returns>The child sections.</returns>
    IEnumerable<IConfigurationSection> GetChildren();
}
