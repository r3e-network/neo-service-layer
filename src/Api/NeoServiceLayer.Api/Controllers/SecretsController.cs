using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.SecretsManagement;
using System.Security;
using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Api.Controllers;

/// <summary>
/// Controller for managing secrets through the Neo Service Layer API.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class SecretsController : ControllerBase
{
    private readonly ISecretsManagementService _secretsService;
    private readonly ILogger<SecretsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretsController"/> class.
    /// </summary>
    /// <param name="secretsService">The secrets management service.</param>
    /// <param name="logger">The logger.</param>
    public SecretsController(ISecretsManagementService secretsService, ILogger<SecretsController> logger)
    {
        _secretsService = secretsService;
        _logger = logger;
    }

    /// <summary>
    /// Stores a new secret securely.
    /// </summary>
    /// <param name="request">The store secret request.</param>
    /// <param name="blockchainType">The blockchain type context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The stored secret metadata.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SecretMetadata), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SecretMetadata>> StoreSecretAsync(
        [FromBody] StoreSecretRequest request,
        [FromQuery] BlockchainType blockchainType = BlockchainType.NeoN3,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Storing secret {SecretId} for blockchain {BlockchainType}", request.SecretId, blockchainType);

            var secureValue = StringToSecureString(request.Value);
            var options = new StoreSecretOptions
            {
                Description = request.Description,
                Tags = request.Tags ?? new Dictionary<string, string>(),
                ExpiresAt = request.ExpiresAt,
                ContentType = request.ContentType,
                Overwrite = request.Overwrite
            };

            var result = await _secretsService.StoreSecretAsync(
                request.SecretId,
                request.Name,
                secureValue,
                options,
                blockchainType,
                cancellationToken);

            _logger.LogInformation("Successfully stored secret {SecretId}", request.SecretId);
            return CreatedAtAction(nameof(GetSecretMetadataAsync), new { secretId = request.SecretId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing secret {SecretId}", request.SecretId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to store secret", details = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves a secret by its identifier.
    /// </summary>
    /// <param name="secretId">The unique identifier of the secret.</param>
    /// <param name="version">The version of the secret to retrieve (null for latest).</param>
    /// <param name="blockchainType">The blockchain type context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The secret if found.</returns>
    [HttpGet("{secretId}")]
    [ProducesResponseType(typeof(SecretResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SecretResponse>> GetSecretAsync(
        [FromRoute] string secretId,
        [FromQuery] int? version = null,
        [FromQuery] BlockchainType blockchainType = BlockchainType.NeoN3,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving secret {SecretId} version {Version} for blockchain {BlockchainType}", secretId, version, blockchainType);

            var secret = await _secretsService.GetSecretAsync(secretId, version, blockchainType, cancellationToken);
            
            if (secret == null)
            {
                _logger.LogWarning("Secret {SecretId} not found", secretId);
                return NotFound(new { error = "Secret not found", secretId });
            }

            var response = new SecretResponse
            {
                Metadata = secret.Metadata,
                Value = SecureStringToString(secret.Value)
            };

            _logger.LogInformation("Successfully retrieved secret {SecretId}", secretId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving secret {SecretId}", secretId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to retrieve secret", details = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves only the metadata of a secret without the value.
    /// </summary>
    /// <param name="secretId">The unique identifier of the secret.</param>
    /// <param name="version">The version of the secret (null for latest).</param>
    /// <param name="blockchainType">The blockchain type context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The secret metadata if found.</returns>
    [HttpGet("{secretId}/metadata")]
    [ProducesResponseType(typeof(SecretMetadata), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SecretMetadata>> GetSecretMetadataAsync(
        [FromRoute] string secretId,
        [FromQuery] int? version = null,
        [FromQuery] BlockchainType blockchainType = BlockchainType.NeoN3,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving secret metadata {SecretId} version {Version} for blockchain {BlockchainType}", secretId, version, blockchainType);

            var metadata = await _secretsService.GetSecretMetadataAsync(secretId, version, blockchainType, cancellationToken);
            
            if (metadata == null)
            {
                _logger.LogWarning("Secret metadata {SecretId} not found", secretId);
                return NotFound(new { error = "Secret not found", secretId });
            }

            _logger.LogInformation("Successfully retrieved secret metadata {SecretId}", secretId);
            return Ok(metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving secret metadata {SecretId}", secretId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to retrieve secret metadata", details = ex.Message });
        }
    }

    /// <summary>
    /// Lists all secret metadata based on the provided options.
    /// </summary>
    /// <param name="tags">Tags to filter by (format: key1=value1,key2=value2).</param>
    /// <param name="includeExpired">Whether to include expired secrets.</param>
    /// <param name="includeInactive">Whether to include inactive secrets.</param>
    /// <param name="limit">Maximum number of secrets to return.</param>
    /// <param name="skip">Number of secrets to skip.</param>
    /// <param name="blockchainType">The blockchain type context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of secret metadata.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SecretMetadata>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<SecretMetadata>>> ListSecretsAsync(
        [FromQuery] string? tags = null,
        [FromQuery] bool includeExpired = false,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int? limit = null,
        [FromQuery] int skip = 0,
        [FromQuery] BlockchainType blockchainType = BlockchainType.NeoN3,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Listing secrets for blockchain {BlockchainType}", blockchainType);

            var options = new GetSecretsOptions
            {
                Tags = ParseTags(tags),
                IncludeExpired = includeExpired,
                IncludeInactive = includeInactive,
                Limit = limit,
                Skip = skip
            };

            var secrets = await _secretsService.ListSecretsAsync(options, blockchainType, cancellationToken);

            _logger.LogInformation("Successfully listed {Count} secrets", secrets.Count());
            return Ok(secrets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing secrets");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to list secrets", details = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing secret with a new value.
    /// </summary>
    /// <param name="secretId">The unique identifier of the secret.</param>
    /// <param name="request">The update secret request.</param>
    /// <param name="blockchainType">The blockchain type context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated secret metadata.</returns>
    [HttpPut("{secretId}")]
    [ProducesResponseType(typeof(SecretMetadata), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SecretMetadata>> UpdateSecretAsync(
        [FromRoute] string secretId,
        [FromBody] UpdateSecretRequest request,
        [FromQuery] BlockchainType blockchainType = BlockchainType.NeoN3,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating secret {SecretId} for blockchain {BlockchainType}", secretId, blockchainType);

            var secureValue = StringToSecureString(request.Value);
            var result = await _secretsService.UpdateSecretAsync(
                secretId,
                secureValue,
                request.Description,
                blockchainType,
                cancellationToken);

            _logger.LogInformation("Successfully updated secret {SecretId}", secretId);
            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning("Secret {SecretId} not found for update", secretId);
            return NotFound(new { error = "Secret not found", secretId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating secret {SecretId}", secretId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to update secret", details = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a secret permanently.
    /// </summary>
    /// <param name="secretId">The unique identifier of the secret.</param>
    /// <param name="blockchainType">The blockchain type context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the secret was deleted, false if not found.</returns>
    [HttpDelete("{secretId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteSecretAsync(
        [FromRoute] string secretId,
        [FromQuery] BlockchainType blockchainType = BlockchainType.NeoN3,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting secret {SecretId} for blockchain {BlockchainType}", secretId, blockchainType);

            var deleted = await _secretsService.DeleteSecretAsync(secretId, blockchainType, cancellationToken);
            
            if (!deleted)
            {
                _logger.LogWarning("Secret {SecretId} not found for deletion", secretId);
                return NotFound(new { error = "Secret not found", secretId });
            }

            _logger.LogInformation("Successfully deleted secret {SecretId}", secretId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting secret {SecretId}", secretId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to delete secret", details = ex.Message });
        }
    }

    /// <summary>
    /// Rotates a secret by creating a new version.
    /// </summary>
    /// <param name="secretId">The unique identifier of the secret.</param>
    /// <param name="request">The rotate secret request.</param>
    /// <param name="blockchainType">The blockchain type context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The metadata of the new secret version.</returns>
    [HttpPost("{secretId}/rotate")]
    [ProducesResponseType(typeof(SecretMetadata), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SecretMetadata>> RotateSecretAsync(
        [FromRoute] string secretId,
        [FromBody] RotateSecretRequest request,
        [FromQuery] BlockchainType blockchainType = BlockchainType.NeoN3,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Rotating secret {SecretId} for blockchain {BlockchainType}", secretId, blockchainType);

            var secureValue = StringToSecureString(request.NewValue);
            var result = await _secretsService.RotateSecretAsync(
                secretId,
                secureValue,
                request.DisableOldVersion,
                blockchainType,
                cancellationToken);

            _logger.LogInformation("Successfully rotated secret {SecretId} to version {Version}", secretId, result.Version);
            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning("Secret {SecretId} not found for rotation", secretId);
            return NotFound(new { error = "Secret not found", secretId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rotating secret {SecretId}", secretId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to rotate secret", details = ex.Message });
        }
    }

    /// <summary>
    /// Configures integration with external secret providers.
    /// </summary>
    /// <param name="request">The external provider configuration request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if configured successfully.</returns>
    [HttpPost("external-providers/configure")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<bool>> ConfigureExternalProviderAsync(
        [FromBody] ConfigureExternalProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Configuring external provider {ProviderType}", request.ProviderType);

            var result = await _secretsService.ConfigureExternalProviderAsync(
                request.ProviderType,
                request.Configuration,
                cancellationToken);

            _logger.LogInformation("Successfully configured external provider {ProviderType}", request.ProviderType);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring external provider {ProviderType}", request.ProviderType);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to configure external provider", details = ex.Message });
        }
    }

    /// <summary>
    /// Synchronizes secrets with an external provider.
    /// </summary>
    /// <param name="request">The synchronization request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of secrets synchronized.</returns>
    [HttpPost("external-providers/sync")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<int>> SynchronizeWithExternalProviderAsync(
        [FromBody] SyncExternalProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Synchronizing with external provider {ProviderType}, direction: {Direction}", request.ProviderType, request.Direction);

            var result = await _secretsService.SynchronizeWithExternalProviderAsync(
                request.ProviderType,
                request.SecretIds,
                request.Direction,
                cancellationToken);

            _logger.LogInformation("Successfully synchronized {Count} secrets with external provider {ProviderType}", result, request.ProviderType);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synchronizing with external provider {ProviderType}", request.ProviderType);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to synchronize with external provider", details = ex.Message });
        }
    }

    private static Dictionary<string, string>? ParseTags(string? tagsString)
    {
        if (string.IsNullOrWhiteSpace(tagsString))
        {
            return null;
        }

        var tags = new Dictionary<string, string>();
        var pairs = tagsString.Split(',', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var pair in pairs)
        {
            var keyValue = pair.Split('=', 2);
            if (keyValue.Length == 2)
            {
                tags[keyValue[0].Trim()] = keyValue[1].Trim();
            }
        }

        return tags.Count > 0 ? tags : null;
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

#region Request/Response Models

/// <summary>
/// Request model for storing a secret.
/// </summary>
public class StoreSecretRequest
{
    /// <summary>
    /// Gets or sets the unique identifier for the secret.
    /// </summary>
    [Required]
    public required string SecretId { get; set; }

    /// <summary>
    /// Gets or sets the human-readable name of the secret.
    /// </summary>
    [Required]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the secret value to store.
    /// </summary>
    [Required]
    public required string Value { get; set; }

    /// <summary>
    /// Gets or sets the description of the secret.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the tags for the secret.
    /// </summary>
    public Dictionary<string, string>? Tags { get; set; }

    /// <summary>
    /// Gets or sets the expiration date for the secret.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the content type of the secret.
    /// </summary>
    public SecretContentType ContentType { get; set; } = SecretContentType.Text;

    /// <summary>
    /// Gets or sets whether to overwrite an existing secret.
    /// </summary>
    public bool Overwrite { get; set; } = false;
}

/// <summary>
/// Request model for updating a secret.
/// </summary>
public class UpdateSecretRequest
{
    /// <summary>
    /// Gets or sets the new secret value.
    /// </summary>
    [Required]
    public required string Value { get; set; }

    /// <summary>
    /// Gets or sets the optional description for the update.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Request model for rotating a secret.
/// </summary>
public class RotateSecretRequest
{
    /// <summary>
    /// Gets or sets the new secret value.
    /// </summary>
    [Required]
    public required string NewValue { get; set; }

    /// <summary>
    /// Gets or sets whether to disable the old version.
    /// </summary>
    public bool DisableOldVersion { get; set; } = true;
}

/// <summary>
/// Request model for configuring external providers.
/// </summary>
public class ConfigureExternalProviderRequest
{
    /// <summary>
    /// Gets or sets the type of external provider.
    /// </summary>
    [Required]
    public ExternalSecretProviderType ProviderType { get; set; }

    /// <summary>
    /// Gets or sets the provider configuration.
    /// </summary>
    [Required]
    public required Dictionary<string, string> Configuration { get; set; }
}

/// <summary>
/// Request model for synchronizing with external providers.
/// </summary>
public class SyncExternalProviderRequest
{
    /// <summary>
    /// Gets or sets the type of external provider.
    /// </summary>
    [Required]
    public ExternalSecretProviderType ProviderType { get; set; }

    /// <summary>
    /// Gets or sets the specific secret IDs to sync (null for all).
    /// </summary>
    public IEnumerable<string>? SecretIds { get; set; }

    /// <summary>
    /// Gets or sets the direction of synchronization.
    /// </summary>
    public SyncDirection Direction { get; set; } = SyncDirection.Pull;
}

/// <summary>
/// Response model for retrieving a secret.
/// </summary>
public class SecretResponse
{
    /// <summary>
    /// Gets or sets the secret metadata.
    /// </summary>
    public required SecretMetadata Metadata { get; set; }

    /// <summary>
    /// Gets or sets the secret value.
    /// </summary>
    public required string Value { get; set; }
}

#endregion