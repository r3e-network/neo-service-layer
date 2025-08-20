using NeoServiceLayer.Core;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Security;


namespace NeoServiceLayer.Services.SecretsManagement;

/// <summary>
/// Interface for external secret providers.
/// </summary>
public interface IExternalSecretProvider
{
    /// <summary>
    /// Gets the type of the external provider.
    /// </summary>
    ExternalSecretProviderType ProviderType { get; }

    /// <summary>
    /// Gets a value indicating whether the provider is configured and ready to use.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Initializes the external provider.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Configures the external provider with the specified settings.
    /// </summary>
    /// <param name="configuration">The configuration settings.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ConfigureAsync(Dictionary<string, string> configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a secret in the external provider.
    /// </summary>
    /// <param name="secretId">The unique identifier for the secret.</param>
    /// <param name="name">The human-readable name of the secret.</param>
    /// <param name="value">The secret value to store.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StoreSecretAsync(string secretId, string name, SecureString value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a secret from the external provider.
    /// </summary>
    /// <param name="secretId">The unique identifier of the secret.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The secret value if found, null otherwise.</returns>
    Task<SecureString?> GetSecretAsync(string secretId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists secrets available in the external provider.
    /// </summary>
    /// <param name="secretIds">The specific secret IDs to list (null for all).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of secret metadata.</returns>
    Task<IEnumerable<SecretMetadata>> ListSecretsAsync(IEnumerable<string>? secretIds = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a secret from the external provider.
    /// </summary>
    /// <param name="secretId">The unique identifier of the secret.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the secret was deleted, false if not found.</returns>
    Task<bool> DeleteSecretAsync(string secretId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the connection to the external provider.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the connection is successful, false otherwise.</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}
