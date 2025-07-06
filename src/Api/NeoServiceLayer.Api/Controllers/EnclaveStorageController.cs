using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.EnclaveStorage;
using NeoServiceLayer.Services.EnclaveStorage.Models;

namespace NeoServiceLayer.Api.Controllers;

/// <summary>
/// Controller for secure enclave storage operations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/enclave-storage")]
[Authorize]
[Tags("Enclave Storage")]
public class EnclaveStorageController : BaseApiController
{
    private readonly IEnclaveStorageService _enclaveStorageService;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnclaveStorageController"/> class.
    /// </summary>
    /// <param name="enclaveStorageService">The enclave storage service.</param>
    /// <param name="logger">The logger.</param>
    public EnclaveStorageController(IEnclaveStorageService enclaveStorageService, ILogger<EnclaveStorageController> logger)
        : base(logger)
    {
        _enclaveStorageService = enclaveStorageService ?? throw new ArgumentNullException(nameof(enclaveStorageService));
    }

    /// <summary>
    /// Stores data securely in the enclave.
    /// </summary>
    /// <param name="request">The storage request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The storage result.</returns>
    /// <response code="200">Data stored successfully.</response>
    /// <response code="400">Invalid storage parameters.</response>
    /// <response code="413">Data too large for enclave storage.</response>
    /// <response code="507">Enclave storage quota exceeded.</response>
    [HttpPost("{blockchainType}/store")]
    [ProducesResponseType(typeof(ApiResponse<StorageResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 413)]
    [ProducesResponseType(typeof(ApiResponse<object>), 507)]
    public async Task<IActionResult> StoreData(
        [FromBody] StoreDataRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _enclaveStorageService.StoreDataAsync(request, blockchain);

            Logger.LogInformation("Stored data with key {Key} in enclave on {Blockchain} - Size: {DataSize} bytes",
                request.Key, blockchainType, request.Data.Length);

            return Ok(CreateResponse(result, "Data stored securely in enclave"));
        }
        catch (ArgumentException ex) when (ex.Message.Contains("too large"))
        {
            return StatusCode(413, CreateErrorResponse("Data size exceeds maximum allowed for enclave storage"));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("quota"))
        {
            return StatusCode(507, CreateErrorResponse("Enclave storage quota exceeded"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "storing data in enclave");
        }
    }

    /// <summary>
    /// Retrieves data from the enclave.
    /// </summary>
    /// <param name="key">The data key.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The retrieved data.</returns>
    /// <response code="200">Data retrieved successfully.</response>
    /// <response code="404">Data not found.</response>
    /// <response code="403">Access denied to data.</response>
    [HttpGet("{blockchainType}/retrieve/{key}")]
    [ProducesResponseType(typeof(ApiResponse<RetrievalResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public async Task<IActionResult> RetrieveData(
        [FromRoute] string key,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _enclaveStorageService.RetrieveDataAsync(key, blockchain);

            if (result == null)
            {
                return NotFound(CreateErrorResponse($"Data not found for key: {key}"));
            }

            Logger.LogInformation("Retrieved data with key {Key} from enclave on {Blockchain}",
                key, blockchainType);

            return Ok(CreateResponse(result, "Data retrieved successfully from enclave"));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, CreateErrorResponse("Access denied to the requested data"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving data from enclave");
        }
    }

    /// <summary>
    /// Updates existing data in the enclave.
    /// </summary>
    /// <param name="key">The data key.</param>
    /// <param name="request">The update request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The update result.</returns>
    /// <response code="200">Data updated successfully.</response>
    /// <response code="404">Data not found.</response>
    /// <response code="403">Access denied to data.</response>
    [HttpPut("{blockchainType}/update/{key}")]
    [ProducesResponseType(typeof(ApiResponse<UpdateResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public async Task<IActionResult> UpdateData(
        [FromRoute] string key,
        [FromBody] UpdateDataRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _enclaveStorageService.UpdateDataAsync(key, request, blockchain);

            if (result == null)
            {
                return NotFound(CreateErrorResponse($"Data not found for key: {key}"));
            }

            Logger.LogInformation("Updated data with key {Key} in enclave on {Blockchain}",
                key, blockchainType);

            return Ok(CreateResponse(result, "Data updated successfully in enclave"));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, CreateErrorResponse("Access denied to update the requested data"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "updating data in enclave");
        }
    }

    /// <summary>
    /// Deletes data from the enclave.
    /// </summary>
    /// <param name="key">The data key.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The deletion result.</returns>
    /// <response code="200">Data deleted successfully.</response>
    /// <response code="404">Data not found.</response>
    /// <response code="403">Access denied to data.</response>
    [HttpDelete("{blockchainType}/delete/{key}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public async Task<IActionResult> DeleteData(
        [FromRoute] string key,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _enclaveStorageService.DeleteDataAsync(key, blockchain);

            if (!result)
            {
                return NotFound(CreateErrorResponse($"Data not found for key: {key}"));
            }

            Logger.LogInformation("Deleted data with key {Key} from enclave on {Blockchain}",
                key, blockchainType);

            return Ok(CreateResponse(result, "Data deleted successfully from enclave"));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, CreateErrorResponse("Access denied to delete the requested data"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "deleting data from enclave");
        }
    }

    /// <summary>
    /// Lists stored data keys with metadata.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <param name="prefix">Filter keys by prefix.</param>
    /// <param name="limit">Maximum number of keys to return.</param>
    /// <returns>List of data keys and metadata.</returns>
    /// <response code="200">Keys retrieved successfully.</response>
    [HttpGet("{blockchainType}/keys")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<DataKeyInfo>>), 200)]
    public async Task<IActionResult> ListDataKeys(
        [FromRoute] string blockchainType,
        [FromQuery] string? prefix = null,
        [FromQuery] int limit = 100)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var filter = new DataKeyFilter
            {
                Prefix = prefix,
                Limit = Math.Min(limit, 500) // Cap at 500
            };

            var result = await _enclaveStorageService.ListDataKeysAsync(filter, blockchain);

            return Ok(CreateResponse(result, "Data keys retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "listing data keys");
        }
    }

    /// <summary>
    /// Gets storage metrics and usage statistics.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>Storage metrics.</returns>
    /// <response code="200">Metrics retrieved successfully.</response>
    [HttpGet("{blockchainType}/metrics")]
    [Authorize(Roles = "Admin,Monitor")]
    [ProducesResponseType(typeof(ApiResponse<StorageMetrics>), 200)]
    public async Task<IActionResult> GetStorageMetrics(
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _enclaveStorageService.GetStorageMetricsAsync(blockchain);

            return Ok(CreateResponse(result, "Storage metrics retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving storage metrics");
        }
    }

    /// <summary>
    /// Creates a secure backup of enclave data.
    /// </summary>
    /// <param name="request">The backup request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The backup result.</returns>
    /// <response code="200">Backup created successfully.</response>
    /// <response code="403">Insufficient permissions for backup operation.</response>
    [HttpPost("{blockchainType}/backup")]
    [Authorize(Roles = "Admin,Backup")]
    [ProducesResponseType(typeof(ApiResponse<BackupResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public async Task<IActionResult> CreateBackup(
        [FromBody] CreateBackupRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _enclaveStorageService.CreateBackupAsync(request, blockchain);

            Logger.LogInformation("Created enclave storage backup {BackupId} on {Blockchain}",
                result.BackupId, blockchainType);

            return Ok(CreateResponse(result, "Enclave storage backup created successfully"));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, CreateErrorResponse("Insufficient permissions for backup operation"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "creating storage backup");
        }
    }

    /// <summary>
    /// Restores enclave data from a backup.
    /// </summary>
    /// <param name="request">The restore request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The restore result.</returns>
    /// <response code="200">Data restored successfully.</response>
    /// <response code="404">Backup not found.</response>
    /// <response code="403">Insufficient permissions for restore operation.</response>
    [HttpPost("{blockchainType}/restore")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<RestoreResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public async Task<IActionResult> RestoreFromBackup(
        [FromBody] RestoreFromBackupRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _enclaveStorageService.RestoreFromBackupAsync(request, blockchain);

            if (result == null)
            {
                return NotFound(CreateErrorResponse($"Backup not found: {request.BackupId}"));
            }

            Logger.LogInformation("Restored enclave storage from backup {BackupId} on {Blockchain}",
                request.BackupId, blockchainType);

            return Ok(CreateResponse(result, "Enclave storage restored successfully"));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, CreateErrorResponse("Insufficient permissions for restore operation"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "restoring from backup");
        }
    }

    /// <summary>
    /// Securely purges all data from the enclave.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <param name="confirmationCode">Security confirmation code.</param>
    /// <returns>Purge result.</returns>
    /// <response code="200">Data purged successfully.</response>
    /// <response code="403">Invalid confirmation code or insufficient permissions.</response>
    [HttpPost("{blockchainType}/purge")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public async Task<IActionResult> PurgeEnclaveData(
        [FromRoute] string blockchainType,
        [FromQuery] string confirmationCode)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _enclaveStorageService.PurgeAllDataAsync(confirmationCode, blockchain);

            if (!result)
            {
                return StatusCode(403, CreateErrorResponse("Invalid confirmation code or insufficient permissions"));
            }

            Logger.LogWarning("Purged all enclave storage data on {Blockchain} - Confirmation: {Code}",
                blockchainType, confirmationCode[..4] + "****");

            return Ok(CreateResponse(result, "Enclave data purged successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "purging enclave data");
        }
    }

    /// <summary>
    /// Validates the integrity of stored data.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <param name="key">Optional specific key to validate.</param>
    /// <returns>Integrity validation result.</returns>
    /// <response code="200">Validation completed successfully.</response>
    [HttpPost("{blockchainType}/validate")]
    [Authorize(Roles = "Admin,Auditor")]
    [ProducesResponseType(typeof(ApiResponse<IntegrityValidationResult>), 200)]
    public async Task<IActionResult> ValidateDataIntegrity(
        [FromRoute] string blockchainType,
        [FromQuery] string? key = null)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _enclaveStorageService.ValidateDataIntegrityAsync(key, blockchain);

            Logger.LogInformation("Validated enclave data integrity on {Blockchain} - Valid: {IsValid}",
                blockchainType, result.IsValid);

            return Ok(CreateResponse(result, "Data integrity validation completed"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "validating data integrity");
        }
    }
}