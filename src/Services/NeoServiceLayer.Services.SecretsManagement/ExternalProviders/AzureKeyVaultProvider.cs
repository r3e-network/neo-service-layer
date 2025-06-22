using System.Security;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.SecretsManagement;

/// <summary>
/// Azure Key Vault implementation of external secret provider.
/// </summary>
public class AzureKeyVaultProvider : IExternalSecretProvider
{
    private readonly ILogger<AzureKeyVaultProvider> _logger;
    private SecretClient? _secretClient;
    private string? _keyVaultUrl;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureKeyVaultProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public AzureKeyVaultProvider(ILogger<AzureKeyVaultProvider> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public ExternalSecretProviderType ProviderType => ExternalSecretProviderType.AzureKeyVault;

    /// <inheritdoc/>
    public bool IsConfigured => _secretClient != null;

    /// <inheritdoc/>
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing Azure Key Vault provider");
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ConfigureAsync(Dictionary<string, string> configuration, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Configuring Azure Key Vault provider");

            if (!configuration.TryGetValue("KeyVaultUrl", out _keyVaultUrl) || string.IsNullOrEmpty(_keyVaultUrl))
            {
                throw new ArgumentException("KeyVaultUrl is required in configuration");
            }

            // Create credential based on configuration
            DefaultAzureCredential credential;

            if (configuration.TryGetValue("TenantId", out var tenantId) &&
                configuration.TryGetValue("ClientId", out var clientId) &&
                configuration.TryGetValue("ClientSecret", out var clientSecret))
            {
                // Use service principal authentication
                credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    TenantId = tenantId
                });
            }
            else
            {
                // Use managed identity or default credential chain
                credential = new DefaultAzureCredential();
            }

            _secretClient = new SecretClient(new Uri(_keyVaultUrl), credential);

            _logger.LogInformation("Azure Key Vault provider configured successfully");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring Azure Key Vault provider");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task StoreSecretAsync(string secretId, string name, SecureString value, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        try
        {
            _logger.LogDebug("Storing secret {SecretId} in Azure Key Vault", secretId);

            var secretValue = SecureStringToString(value);
            var secret = new KeyVaultSecret(SanitizeSecretName(secretId), secretValue);

            // Add metadata
            secret.Properties.Tags["Name"] = name;
            secret.Properties.Tags["Source"] = "NeoServiceLayer";
            secret.Properties.Tags["CreatedAt"] = DateTime.UtcNow.ToString("O");

            await _secretClient!.SetSecretAsync(secret, cancellationToken);

            _logger.LogDebug("Successfully stored secret {SecretId} in Azure Key Vault", secretId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing secret {SecretId} in Azure Key Vault", secretId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<SecureString?> GetSecretAsync(string secretId, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        try
        {
            _logger.LogDebug("Retrieving secret {SecretId} from Azure Key Vault", secretId);

            var response = await _secretClient!.GetSecretAsync(SanitizeSecretName(secretId), cancellationToken: cancellationToken);

            if (response?.Value?.Value != null)
            {
                return StringToSecureString(response.Value.Value);
            }

            return null;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogDebug("Secret {SecretId} not found in Azure Key Vault", secretId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving secret {SecretId} from Azure Key Vault", secretId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<SecretMetadata>> ListSecretsAsync(IEnumerable<string>? secretIds = null, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        try
        {
            _logger.LogDebug("Listing secrets from Azure Key Vault");

            var secrets = new List<SecretMetadata>();
            var secretProperties = _secretClient!.GetPropertiesOfSecretsAsync(cancellationToken);

            await foreach (var secretProperty in secretProperties)
            {
                if (secretIds != null && !secretIds.Contains(secretProperty.Name))
                {
                    continue;
                }

                var metadata = new SecretMetadata
                {
                    SecretId = secretProperty.Name,
                    Name = secretProperty.Tags.TryGetValue("Name", out var name) ? name : secretProperty.Name,
                    Description = "Synced from Azure Key Vault",
                    CreatedAt = secretProperty.CreatedOn?.DateTime ?? DateTime.UtcNow,
                    UpdatedAt = secretProperty.UpdatedOn?.DateTime ?? DateTime.UtcNow,
                    ExpiresAt = secretProperty.ExpiresOn?.DateTime,
                    IsActive = secretProperty.Enabled ?? true,
                    Tags = secretProperty.Tags.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                };

                secrets.Add(metadata);
            }

            _logger.LogDebug("Listed {Count} secrets from Azure Key Vault", secrets.Count);
            return secrets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing secrets from Azure Key Vault");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteSecretAsync(string secretId, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        try
        {
            _logger.LogDebug("Deleting secret {SecretId} from Azure Key Vault", secretId);

            var operation = await _secretClient!.StartDeleteSecretAsync(SanitizeSecretName(secretId), cancellationToken);
            await operation.WaitForCompletionAsync(cancellationToken);

            _logger.LogDebug("Successfully deleted secret {SecretId} from Azure Key Vault", secretId);
            return true;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogDebug("Secret {SecretId} not found in Azure Key Vault for deletion", secretId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting secret {SecretId} from Azure Key Vault", secretId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        try
        {
            _logger.LogDebug("Testing connection to Azure Key Vault");

            // Try to list secrets to test connectivity
            var secrets = _secretClient!.GetPropertiesOfSecretsAsync(cancellationToken);
            var enumerator = secrets.GetAsyncEnumerator(cancellationToken);
            try
            {
                if (await enumerator.MoveNextAsync())
                {
                    // Successfully connected - we can list at least one secret
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }

            _logger.LogDebug("Azure Key Vault connection test successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure Key Vault connection test failed");
            return false;
        }
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Azure Key Vault provider is not configured");
        }
    }

    private static string SanitizeSecretName(string secretId)
    {
        // Azure Key Vault secret names can only contain alphanumeric characters and hyphens
        return secretId.Replace("_", "-").Replace(".", "-").Replace(":", "-");
    }

    private static string SecureStringToString(SecureString secureString)
    {
        IntPtr ptr = IntPtr.Zero;
        try
        {
            ptr = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(secureString);
            return System.Runtime.InteropServices.Marshal.PtrToStringUni(ptr) ?? string.Empty;
        }
        finally
        {
            if (ptr != IntPtr.Zero)
            {
                System.Runtime.InteropServices.Marshal.ZeroFreeGlobalAllocUnicode(ptr);
            }
        }
    }

    private static SecureString StringToSecureString(string str)
    {
        var secureString = new SecureString();
        foreach (char c in str)
        {
            secureString.AppendChar(c);
        }
        secureString.MakeReadOnly();
        return secureString;
    }
}
