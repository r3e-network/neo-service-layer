using System.Security;

namespace NeoServiceLayer.Core;

/// <summary>
/// Represents metadata for a stored secret.
/// </summary>
public class SecretMetadata
{
    /// <summary>
    /// Gets or sets the unique identifier for the secret.
    /// </summary>
    public required string SecretId { get; set; }

    /// <summary>
    /// Gets or sets the human-readable name of the secret.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the secret.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the version of the secret.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets the tags associated with the secret.
    /// </summary>
    public Dictionary<string, string> Tags { get; set; } = new();

    /// <summary>
    /// Gets or sets when the secret was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the secret was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the secret was last accessed.
    /// </summary>
    public DateTime? LastAccessedAt { get; set; }

    /// <summary>
    /// Gets or sets the expiration date of the secret.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets whether the secret is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the content type of the secret.
    /// </summary>
    public SecretContentType ContentType { get; set; } = SecretContentType.Text;

    /// <summary>
    /// Gets or sets the encryption algorithm used to protect the secret.
    /// </summary>
    public string EncryptionAlgorithm { get; set; } = "AES-256-GCM";

    /// <summary>
    /// Gets or sets the access control information.
    /// </summary>
    public SecretAccessControl AccessControl { get; set; } = new();
}

/// <summary>
/// Represents the type of content stored in a secret.
/// </summary>
public enum SecretContentType
{
    /// <summary>
    /// Plain text content.
    /// </summary>
    Text,

    /// <summary>
    /// JSON content.
    /// </summary>
    Json,

    /// <summary>
    /// Binary content.
    /// </summary>
    Binary,

    /// <summary>
    /// Database connection string.
    /// </summary>
    ConnectionString,

    /// <summary>
    /// API key or token.
    /// </summary>
    ApiKey,

    /// <summary>
    /// Certificate content.
    /// </summary>
    Certificate,

    /// <summary>
    /// Private key content.
    /// </summary>
    PrivateKey
}

/// <summary>
/// Represents access control settings for a secret.
/// </summary>
public class SecretAccessControl
{
    /// <summary>
    /// Gets or sets the list of roles that can read the secret.
    /// </summary>
    public List<string> ReadRoles { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of roles that can write the secret.
    /// </summary>
    public List<string> WriteRoles { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of roles that can delete the secret.
    /// </summary>
    public List<string> DeleteRoles { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of services that can access the secret.
    /// </summary>
    public List<string> AllowedServices { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of IP addresses/ranges that can access the secret.
    /// </summary>
    public List<string> AllowedIpRanges { get; set; } = new();

    /// <summary>
    /// Gets or sets whether audit logging is required for this secret.
    /// </summary>
    public bool RequireAuditLogging { get; set; } = true;
}

/// <summary>
/// Represents a secret value with its metadata.
/// </summary>
public class Secret
{
    /// <summary>
    /// Gets or sets the metadata for the secret.
    /// </summary>
    public required SecretMetadata Metadata { get; set; }

    /// <summary>
    /// Gets or sets the secret value.
    /// </summary>
    public required SecureString Value { get; set; }
}

/// <summary>
/// Represents options for storing a secret.
/// </summary>
public class StoreSecretOptions
{
    /// <summary>
    /// Gets or sets the description of the secret.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the tags for the secret.
    /// </summary>
    public Dictionary<string, string> Tags { get; set; } = new();

    /// <summary>
    /// Gets or sets the expiration date for the secret.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the content type of the secret.
    /// </summary>
    public SecretContentType ContentType { get; set; } = SecretContentType.Text;

    /// <summary>
    /// Gets or sets the access control for the secret.
    /// </summary>
    public SecretAccessControl? AccessControl { get; set; }

    /// <summary>
    /// Gets or sets whether to overwrite an existing secret.
    /// </summary>
    public bool Overwrite { get; set; } = false;
}

/// <summary>
/// Represents options for retrieving secrets.
/// </summary>
public class GetSecretsOptions
{
    /// <summary>
    /// Gets or sets the tags to filter by.
    /// </summary>
    public Dictionary<string, string>? Tags { get; set; }

    /// <summary>
    /// Gets or sets whether to include expired secrets.
    /// </summary>
    public bool IncludeExpired { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to include inactive secrets.
    /// </summary>
    public bool IncludeInactive { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum number of secrets to return.
    /// </summary>
    public int? Limit { get; set; }

    /// <summary>
    /// Gets or sets the number of secrets to skip.
    /// </summary>
    public int Skip { get; set; } = 0;
}

/// <summary>
/// Interface for managing secrets securely within the trusted execution environment.
/// </summary>
public interface ISecretsManager
{
    /// <summary>
    /// Stores a secret securely in the encrypted storage.
    /// </summary>
    /// <param name="secretId">The unique identifier for the secret.</param>
    /// <param name="name">The human-readable name of the secret.</param>
    /// <param name="value">The secret value to store.</param>
    /// <param name="options">Options for storing the secret.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The metadata of the stored secret.</returns>
    Task<SecretMetadata> StoreSecretAsync(string secretId, string name, SecureString value, StoreSecretOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a secret by its identifier.
    /// </summary>
    /// <param name="secretId">The unique identifier of the secret.</param>
    /// <param name="version">The version of the secret to retrieve (null for latest).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The secret if found, null otherwise.</returns>
    Task<Secret?> GetSecretAsync(string secretId, int? version = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves only the metadata of a secret without the value.
    /// </summary>
    /// <param name="secretId">The unique identifier of the secret.</param>
    /// <param name="version">The version of the secret (null for latest).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The secret metadata if found, null otherwise.</returns>
    Task<SecretMetadata?> GetSecretMetadataAsync(string secretId, int? version = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all secret metadata based on the provided options.
    /// </summary>
    /// <param name="options">Options for filtering and pagination.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of secret metadata.</returns>
    Task<IEnumerable<SecretMetadata>> ListSecretsAsync(GetSecretsOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing secret with a new value.
    /// </summary>
    /// <param name="secretId">The unique identifier of the secret.</param>
    /// <param name="value">The new secret value.</param>
    /// <param name="description">Optional description for the update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated secret metadata.</returns>
    Task<SecretMetadata> UpdateSecretAsync(string secretId, SecureString value, string? description = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a secret permanently.
    /// </summary>
    /// <param name="secretId">The unique identifier of the secret.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the secret was deleted, false if not found.</returns>
    Task<bool> DeleteSecretAsync(string secretId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new version of an existing secret.
    /// </summary>
    /// <param name="secretId">The unique identifier of the secret.</param>
    /// <param name="value">The new secret value.</param>
    /// <param name="description">Optional description for the new version.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The metadata of the new secret version.</returns>
    Task<SecretMetadata> CreateSecretVersionAsync(string secretId, SecureString value, string? description = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all versions of a secret.
    /// </summary>
    /// <param name="secretId">The unique identifier of the secret.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of secret metadata for all versions.</returns>
    Task<IEnumerable<SecretMetadata>> ListSecretVersionsAsync(string secretId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotates a secret by creating a new version and optionally disabling the old one.
    /// </summary>
    /// <param name="secretId">The unique identifier of the secret.</param>
    /// <param name="newValue">The new secret value.</param>
    /// <param name="disableOldVersion">Whether to disable the old version.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The metadata of the new secret version.</returns>
    Task<SecretMetadata> RotateSecretAsync(string secretId, SecureString newValue, bool disableOldVersion = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports secrets to a secure backup format.
    /// </summary>
    /// <param name="secretIds">The identifiers of secrets to export (null for all).</param>
    /// <param name="encryptionKey">The key to encrypt the backup with.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The encrypted backup data.</returns>
    Task<byte[]> ExportSecretsAsync(IEnumerable<string>? secretIds = null, SecureString? encryptionKey = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports secrets from a secure backup format.
    /// </summary>
    /// <param name="backupData">The encrypted backup data.</param>
    /// <param name="decryptionKey">The key to decrypt the backup with.</param>
    /// <param name="overwriteExisting">Whether to overwrite existing secrets.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of secrets imported.</returns>
    Task<int> ImportSecretsAsync(byte[] backupData, SecureString decryptionKey, bool overwriteExisting = false, CancellationToken cancellationToken = default);
}