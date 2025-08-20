using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.EnclaveStorage;
using NeoServiceLayer.Services.EnclaveStorage.Models;
using NeoServiceLayer.Web.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;


namespace NeoServiceLayer.Web.Controllers;

/// <summary>
/// Controller for enclave storage service operations.
/// </summary>
[Authorize]
[Tags("Enclave Storage")]
public class EnclaveStorageController : BaseApiController
{
    private readonly IEnclaveStorageService _enclaveStorageService;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnclaveStorageController"/> class.
    /// </summary>
    public EnclaveStorageController(
        IEnclaveStorageService enclaveStorageService,
        ILogger<EnclaveStorageController> logger)
        : base(logger)
    {
        _enclaveStorageService = enclaveStorageService;
    }

    /// <summary>
    /// Seals and stores data within the enclave.
    /// </summary>
    /// <param name="request">The seal data request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The seal result.</returns>
    [HttpPost("seal/{blockchainType}")]
    [ProducesResponseType(typeof(ApiResponse<SealDataResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> SealData(
        [FromBody] SealDataRequest request,
        [FromRoute] BlockchainType blockchainType)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(CreateErrorResponse("Invalid request", GetModelStateErrors()));
            }

            var result = await _enclaveStorageService.SealDataAsync(request, blockchainType);

            return Ok(CreateSuccessResponse(result, "Data sealed successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(CreateErrorResponse("Invalid request", ex.Message));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("quota"))
        {
            return StatusCode(507, CreateErrorResponse("Insufficient storage", ex.Message));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to seal data");
            return StatusCode(500, CreateErrorResponse("Failed to seal data", ex.Message));
        }
    }

    /// <summary>
    /// Unseals previously stored data.
    /// </summary>
    /// <param name="key">The data key.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The unsealed data.</returns>
    [HttpGet("unseal/{key}/{blockchainType}")]
    [ProducesResponseType(typeof(ApiResponse<UnsealDataResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> UnsealData(
        [FromRoute] string key,
        [FromRoute] BlockchainType blockchainType)
    {
        try
        {
            var result = await _enclaveStorageService.UnsealDataAsync(key, blockchainType);

            return Ok(CreateSuccessResponse(result, "Data unsealed successfully"));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(CreateErrorResponse("Data not found", ex.Message));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("expired"))
        {
            return StatusCode(410, CreateErrorResponse("Data expired", ex.Message));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to unseal data");
            return StatusCode(500, CreateErrorResponse("Failed to unseal data", ex.Message));
        }
    }

    /// <summary>
    /// Lists all sealed items for a service.
    /// </summary>
    /// <param name="service">The service filter.</param>
    /// <param name="prefix">The key prefix filter.</param>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The list of sealed items.</returns>
    [HttpGet("list/{blockchainType}")]
    [ProducesResponseType(typeof(ApiResponse<SealedItemsList>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> ListSealedItems(
        [FromQuery] string? service,
        [FromQuery] string? prefix,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        [FromRoute] BlockchainType blockchainType = BlockchainType.NeoN3)
    {
        try
        {
            var request = new ListSealedItemsRequest
            {
                Service = service,
                Prefix = prefix,
                Page = page,
                PageSize = pageSize
            };

            var result = await _enclaveStorageService.ListSealedItemsAsync(request, blockchainType);

            return Ok(CreateSuccessResponse(result, "Sealed items retrieved"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to list sealed items");
            return StatusCode(500, CreateErrorResponse("Failed to list sealed items", ex.Message));
        }
    }

    /// <summary>
    /// Deletes sealed data.
    /// </summary>
    /// <param name="key">The data key to delete.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The deletion result.</returns>
    [HttpDelete("{key}/{blockchainType}")]
    [ProducesResponseType(typeof(ApiResponse<DeleteSealedDataResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> DeleteSealedData(
        [FromRoute] string key,
        [FromRoute] BlockchainType blockchainType)
    {
        try
        {
            var result = await _enclaveStorageService.DeleteSealedDataAsync(key, blockchainType);

            if (!result.Deleted)
            {
                return NotFound(CreateErrorResponse("Data not found", $"No sealed data found for key: {key}"));
            }

            return Ok(CreateSuccessResponse(result, "Data deleted successfully"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete sealed data");
            return StatusCode(500, CreateErrorResponse("Failed to delete sealed data", ex.Message));
        }
    }

    /// <summary>
    /// Gets storage statistics.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The storage statistics.</returns>
    [HttpGet("statistics/{blockchainType}")]
    [ProducesResponseType(typeof(ApiResponse<EnclaveStorageStatistics>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> GetStorageStatistics([FromRoute] BlockchainType blockchainType)
    {
        try
        {
            var result = await _enclaveStorageService.GetStorageStatisticsAsync(blockchainType);

            return Ok(CreateSuccessResponse(result, "Storage statistics retrieved"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get storage statistics");
            return StatusCode(500, CreateErrorResponse("Failed to get storage statistics", ex.Message));
        }
    }

    /// <summary>
    /// Backs up sealed data with re-sealing.
    /// </summary>
    /// <param name="request">The backup request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The backup result.</returns>
    [HttpPost("backup/{blockchainType}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<BackupResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> BackupSealedData(
        [FromBody] BackupRequest request,
        [FromRoute] BlockchainType blockchainType)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(CreateErrorResponse("Invalid request", GetModelStateErrors()));
            }

            var result = await _enclaveStorageService.BackupSealedDataAsync(request, blockchainType);

            return Ok(CreateSuccessResponse(result, "Backup completed successfully"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to backup sealed data");
            return StatusCode(500, CreateErrorResponse("Failed to backup sealed data", ex.Message));
        }
    }
}
