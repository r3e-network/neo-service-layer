using System.Threading.Tasks;

namespace NeoServiceLayer.Infrastructure.Secrets;

/// <summary>
/// Interface for secret management providers.
/// </summary>
public interface ISecretProvider
{
    /// <summary>
    /// Gets a secret value by key.
    /// </summary>
    /// <param name="key">The secret key.</param>
    /// <returns>The secret value.</returns>
    Task<string?> GetSecretAsync(string key);

    /// <summary>
    /// Sets a secret value.
    /// </summary>
    /// <param name="key">The secret key.</param>
    /// <param name="value">The secret value.</param>
    /// <returns>True if successful.</returns>
    Task<bool> SetSecretAsync(string key, string value);

    /// <summary>
    /// Deletes a secret.
    /// </summary>
    /// <param name="key">The secret key.</param>
    /// <returns>True if successful.</returns>
    Task<bool> DeleteSecretAsync(string key);

    /// <summary>
    /// Checks if a secret exists.
    /// </summary>
    /// <param name="key">The secret key.</param>
    /// <returns>True if the secret exists.</returns>
    Task<bool> ExistsAsync(string key);
}