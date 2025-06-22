using System.Security;
using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.SecretsManagement;

/// <summary>
/// AWS Secrets Manager implementation of external secret provider.
/// </summary>
public class AwsSecretsManagerProvider : IExternalSecretProvider
{
    private readonly ILogger<AwsSecretsManagerProvider> _logger;
    private AmazonSecretsManagerClient? _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="AwsSecretsManagerProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public AwsSecretsManagerProvider(ILogger<AwsSecretsManagerProvider> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public ExternalSecretProviderType ProviderType => ExternalSecretProviderType.AwsSecretsManager;

    /// <inheritdoc/>
    public bool IsConfigured => _client != null;

    /// <inheritdoc/>
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing AWS Secrets Manager provider");
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ConfigureAsync(Dictionary<string, string> configuration, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Configuring AWS Secrets Manager provider");

            var config = new AmazonSecretsManagerConfig();

            if (configuration.TryGetValue("Region", out var region) && !string.IsNullOrEmpty(region))
            {
                config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region);
            }

            if (configuration.TryGetValue("ServiceUrl", out var serviceUrl) && !string.IsNullOrEmpty(serviceUrl))
            {
                config.ServiceURL = serviceUrl;
            }

            // Create client - credentials will be resolved from environment/role/profile
            _client = new AmazonSecretsManagerClient(config);

            _logger.LogInformation("AWS Secrets Manager provider configured successfully");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring AWS Secrets Manager provider");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task StoreSecretAsync(string secretId, string name, SecureString value, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        try
        {
            _logger.LogDebug("Storing secret {SecretId} in AWS Secrets Manager", secretId);

            var secretValue = SecureStringToString(value);

            // Check if secret exists
            bool secretExists = await SecretExistsAsync(secretId, cancellationToken);

            if (secretExists)
            {
                // Update existing secret
                var updateRequest = new UpdateSecretRequest
                {
                    SecretId = secretId,
                    SecretString = secretValue,
                    Description = $"Updated by NeoServiceLayer - {name}"
                };

                await _client!.UpdateSecretAsync(updateRequest, cancellationToken);
            }
            else
            {
                // Create new secret
                var createRequest = new CreateSecretRequest
                {
                    Name = secretId,
                    SecretString = secretValue,
                    Description = $"Created by NeoServiceLayer - {name}",
                    Tags = new List<Tag>
                    {
                        new Tag { Key = "Name", Value = name },
                        new Tag { Key = "Source", Value = "NeoServiceLayer" },
                        new Tag { Key = "CreatedAt", Value = DateTime.UtcNow.ToString("O") }
                    }
                };

                await _client!.CreateSecretAsync(createRequest, cancellationToken);
            }

            _logger.LogDebug("Successfully stored secret {SecretId} in AWS Secrets Manager", secretId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing secret {SecretId} in AWS Secrets Manager", secretId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<SecureString?> GetSecretAsync(string secretId, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        try
        {
            _logger.LogDebug("Retrieving secret {SecretId} from AWS Secrets Manager", secretId);

            var request = new GetSecretValueRequest
            {
                SecretId = secretId
            };

            var response = await _client!.GetSecretValueAsync(request, cancellationToken);

            if (!string.IsNullOrEmpty(response.SecretString))
            {
                return StringToSecureString(response.SecretString);
            }

            return null;
        }
        catch (ResourceNotFoundException)
        {
            _logger.LogDebug("Secret {SecretId} not found in AWS Secrets Manager", secretId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving secret {SecretId} from AWS Secrets Manager", secretId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<SecretMetadata>> ListSecretsAsync(IEnumerable<string>? secretIds = null, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        try
        {
            _logger.LogDebug("Listing secrets from AWS Secrets Manager");

            var secrets = new List<SecretMetadata>();
            var request = new ListSecretsRequest
            {
                MaxResults = 100
            };

            ListSecretsResponse response;
            do
            {
                response = await _client!.ListSecretsAsync(request, cancellationToken);

                foreach (var secret in response.SecretList)
                {
                    if (secretIds != null && !secretIds.Contains(secret.Name))
                    {
                        continue;
                    }

                    var nameTag = secret.Tags?.FirstOrDefault(t => t.Key == "Name")?.Value;

                    var metadata = new SecretMetadata
                    {
                        SecretId = secret.Name,
                        Name = nameTag ?? secret.Name,
                        Description = secret.Description ?? "Synced from AWS Secrets Manager",
                        CreatedAt = secret.CreatedDate,
                        UpdatedAt = secret.LastChangedDate,
                        Tags = secret.Tags?.ToDictionary(t => t.Key, t => t.Value) ?? new Dictionary<string, string>()
                    };

                    secrets.Add(metadata);
                }

                request.NextToken = response.NextToken;
            }
            while (!string.IsNullOrEmpty(response.NextToken));

            _logger.LogDebug("Listed {Count} secrets from AWS Secrets Manager", secrets.Count);
            return secrets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing secrets from AWS Secrets Manager");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteSecretAsync(string secretId, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        try
        {
            _logger.LogDebug("Deleting secret {SecretId} from AWS Secrets Manager", secretId);

            var request = new DeleteSecretRequest
            {
                SecretId = secretId,
                ForceDeleteWithoutRecovery = true
            };

            await _client!.DeleteSecretAsync(request, cancellationToken);

            _logger.LogDebug("Successfully deleted secret {SecretId} from AWS Secrets Manager", secretId);
            return true;
        }
        catch (ResourceNotFoundException)
        {
            _logger.LogDebug("Secret {SecretId} not found in AWS Secrets Manager for deletion", secretId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting secret {SecretId} from AWS Secrets Manager", secretId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        try
        {
            _logger.LogDebug("Testing connection to AWS Secrets Manager");

            // Try to list secrets to test connectivity
            var request = new ListSecretsRequest { MaxResults = 1 };
            await _client!.ListSecretsAsync(request, cancellationToken);

            _logger.LogDebug("AWS Secrets Manager connection test successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AWS Secrets Manager connection test failed");
            return false;
        }
    }

    private async Task<bool> SecretExistsAsync(string secretId, CancellationToken cancellationToken)
    {
        try
        {
            var request = new DescribeSecretRequest { SecretId = secretId };
            await _client!.DescribeSecretAsync(request, cancellationToken);
            return true;
        }
        catch (ResourceNotFoundException)
        {
            return false;
        }
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("AWS Secrets Manager provider is not configured");
        }
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
