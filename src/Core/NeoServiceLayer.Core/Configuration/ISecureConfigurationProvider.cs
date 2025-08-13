using System.Threading.Tasks;

namespace NeoServiceLayer.Core.Configuration
{
    /// <summary>
    /// Provides secure configuration values from environment variables, key vaults, or other secure sources
    /// </summary>
    public interface ISecureConfigurationProvider
    {
        /// <summary>
        /// Gets a configuration value securely
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <returns>The configuration value</returns>
        Task<string?> GetSecureValueAsync(string key, string? defaultValue = null);

        /// <summary>
        /// Gets a connection string securely
        /// </summary>
        /// <param name="name">The connection string name</param>
        /// <returns>The connection string</returns>
        Task<string?> GetConnectionStringAsync(string name);

        /// <summary>
        /// Gets a secret from secure storage
        /// </summary>
        /// <param name="secretName">The secret name</param>
        /// <returns>The secret value</returns>
        Task<string?> GetSecretAsync(string secretName);

        /// <summary>
        /// Checks if a configuration key exists
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <returns>True if the key exists</returns>
        Task<bool> ExistsAsync(string key);
    }
}
