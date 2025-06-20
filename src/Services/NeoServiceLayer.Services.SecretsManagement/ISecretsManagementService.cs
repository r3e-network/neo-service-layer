using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using System.Security;

namespace NeoServiceLayer.Services.SecretsManagement;

/// <summary>
/// Interface for the Secrets Management service.
/// </summary>
public interface ISecretsManagementService : IService
{
    /// <summary>
    /// Stores a secret securely.
    /// </summary>
    /// <param name="secretId">The unique identifier for the secret.</param>
    /// <param name="name">The human-readable name of the secret.</param>
    /// <param name="value">The secret value to store.</param>
    /// <param name="options">Options for storing the secret.</param>
    /// <param name="blockchainType">The blockchain type context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The metadata of the stored secret.</returns>
    Task<SecretMetadata> StoreSecretAsync(string secretId, string name, SecureString value, StoreSecretOptions? options = null, BlockchainType blockchainType = BlockchainType.NeoN3, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a secret by its identifier.
    /// </summary>
    /// <param name="secretId">The unique identifier of the secret.</param>
    /// <param name="version">The version of the secret to retrieve (null for latest).</param>
    /// <param name="blockchainType">The blockchain type context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The secret if found, null otherwise.</returns>
    Task<Secret?> GetSecretAsync(string secretId, int? version = null, BlockchainType blockchainType = BlockchainType.NeoN3, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves only the metadata of a secret without the value.
    /// </summary>
    /// <param name="secretId">The unique identifier of the secret.</param>
    /// <param name="version">The version of the secret (null for latest).</param>
    /// <param name="blockchainType">The blockchain type context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The secret metadata if found, null otherwise.</returns>
    Task<SecretMetadata?> GetSecretMetadataAsync(string secretId, int? version = null, BlockchainType blockchainType = BlockchainType.NeoN3, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all secret metadata based on the provided options.
    /// </summary>
    /// <param name="options">Options for filtering and pagination.</param>
    /// <param name="blockchainType">The blockchain type context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of secret metadata.</returns>
    Task<IEnumerable<SecretMetadata>> ListSecretsAsync(GetSecretsOptions? options = null, BlockchainType blockchainType = BlockchainType.NeoN3, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing secret with a new value.
    /// </summary>
    /// <param name="secretId">The unique identifier of the secret.</param>
    /// <param name="value">The new secret value.</param>
    /// <param name="description">Optional description for the update.</param>
    /// <param name="blockchainType">The blockchain type context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated secret metadata.</returns>
    Task<SecretMetadata> UpdateSecretAsync(string secretId, SecureString value, string? description = null, BlockchainType blockchainType = BlockchainType.NeoN3, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a secret permanently.
    /// </summary>
    /// <param name="secretId">The unique identifier of the secret.</param>
    /// <param name="blockchainType">The blockchain type context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the secret was deleted, false if not found.</returns>
    Task<bool> DeleteSecretAsync(string secretId, BlockchainType blockchainType = BlockchainType.NeoN3, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotates a secret by creating a new version.
    /// </summary>
    /// <param name="secretId">The unique identifier of the secret.</param>
    /// <param name="newValue">The new secret value.</param>
    /// <param name="disableOldVersion">Whether to disable the old version.</param>
    /// <param name="blockchainType">The blockchain type context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The metadata of the new secret version.</returns>
    Task<SecretMetadata> RotateSecretAsync(string secretId, SecureString newValue, bool disableOldVersion = true, BlockchainType blockchainType = BlockchainType.NeoN3, CancellationToken cancellationToken = default);

    /// <summary>
    /// Configures integration with external secret providers.
    /// </summary>
    /// <param name="providerType">The type of external provider.</param>
    /// <param name="configuration">The provider configuration.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if configured successfully.</returns>
    Task<bool> ConfigureExternalProviderAsync(ExternalSecretProviderType providerType, Dictionary<string, string> configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronizes secrets with an external provider.
    /// </summary>
    /// <param name="providerType">The type of external provider.</param>
    /// <param name="secretIds">The specific secret IDs to sync (null for all).</param>
    /// <param name="direction">The direction of synchronization.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of secrets synchronized.</returns>
    Task<int> SynchronizeWithExternalProviderAsync(ExternalSecretProviderType providerType, IEnumerable<string>? secretIds = null, SyncDirection direction = SyncDirection.Pull, CancellationToken cancellationToken = default);
}

/// <summary>
/// Types of external secret providers.
/// </summary>
public enum ExternalSecretProviderType
{
    /// <summary>
    /// Azure Key Vault.
    /// </summary>
    AzureKeyVault,

    /// <summary>
    /// AWS Secrets Manager.
    /// </summary>
    AwsSecretsManager,

    /// <summary>
    /// HashiCorp Vault.
    /// </summary>
    HashiCorpVault,

    /// <summary>
    /// Google Secret Manager.
    /// </summary>
    GoogleSecretManager,

    /// <summary>
    /// Kubernetes Secrets.
    /// </summary>
    KubernetesSecrets
}

/// <summary>
/// Direction of synchronization with external providers.
/// </summary>
public enum SyncDirection
{
    /// <summary>
    /// Pull secrets from external provider to local storage.
    /// </summary>
    Pull,

    /// <summary>
    /// Push secrets from local storage to external provider.
    /// </summary>
    Push,

    /// <summary>
    /// Bidirectional synchronization.
    /// </summary>
    Bidirectional
}