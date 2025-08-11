using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Storage;
using NeoServiceLayer.Services.Storage.Models;

namespace NeoServiceLayer.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StorageController : ControllerBase
{
    private readonly IStorageService _storageService;
    private readonly ILogger<StorageController> _logger;

    public StorageController(IStorageService storageService, ILogger<StorageController> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the storage service health status.
    /// </summary>
    /// <returns>Service health status.</returns>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatusAsync()
    {
        try
        {
            var health = await _storageService.GetHealthAsync();
            return Ok(new { status = health.ToString(), timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting storage service status");
            return StatusCode(500, new { error = "Failed to get service status" });
        }
    }

    /// <summary>
    /// Stores data with the specified key.
    /// </summary>
    /// <param name="request">The store data request.</param>
    /// <returns>Storage metadata for the stored data.</returns>
    [HttpPost("store")]
    public async Task<IActionResult> StoreDataAsync([FromBody] StoreDataRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Key) || request.Data == null)
            {
                return BadRequest(new { error = "Key and data are required" });
            }

            var options = new StorageOptions
            {
                Encrypt = request.Encrypt,
                Compress = request.Compress,
                ExpiresAt = request.ExpiresAt,
                EncryptionKeyId = request.EncryptionKeyId,
                CustomMetadata = request.CustomMetadata ?? new Dictionary<string, string>()
            };

            var result = await _storageService.StoreDataAsync(
                request.Key, 
                request.Data, 
                options, 
                request.BlockchainType ?? BlockchainType.NeoN3);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing data with key {Key}", request.Key);
            return StatusCode(500, new { error = "Failed to store data", details = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves data by key.
    /// </summary>
    /// <param name="key">The data key.</param>
    /// <param name="blockchainType">The blockchain type (optional, defaults to NeoN3).</param>
    /// <returns>The stored data.</returns>
    [HttpGet("retrieve/{key}")]
    public async Task<IActionResult> RetrieveDataAsync(string key, [FromQuery] BlockchainType? blockchainType = null)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
            {
                return BadRequest(new { error = "Key is required" });
            }

            var data = await _storageService.GetDataAsync(key, blockchainType ?? BlockchainType.NeoN3);
            
            if (data == null)
            {
                return NotFound(new { error = "Data not found" });
            }

            return File(data, "application/octet-stream", $"{key}.data");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving data with key {Key}", key);
            return StatusCode(500, new { error = "Failed to retrieve data", details = ex.Message });
        }
    }

    /// <summary>
    /// Deletes data by key.
    /// </summary>
    /// <param name="key">The data key.</param>
    /// <param name="blockchainType">The blockchain type (optional, defaults to NeoN3).</param>
    /// <returns>Success or failure result.</returns>
    [HttpDelete("delete/{key}")]
    public async Task<IActionResult> DeleteDataAsync(string key, [FromQuery] BlockchainType? blockchainType = null)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
            {
                return BadRequest(new { error = "Key is required" });
            }

            var success = await _storageService.DeleteDataAsync(key, blockchainType ?? BlockchainType.NeoN3);
            
            if (!success)
            {
                return NotFound(new { error = "Data not found or could not be deleted" });
            }

            return Ok(new { message = "Data deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting data with key {Key}", key);
            return StatusCode(500, new { error = "Failed to delete data", details = ex.Message });
        }
    }

    /// <summary>
    /// Gets metadata for stored data.
    /// </summary>
    /// <param name="key">The data key.</param>
    /// <param name="blockchainType">The blockchain type (optional, defaults to NeoN3).</param>
    /// <returns>Storage metadata.</returns>
    [HttpGet("metadata/{key}")]
    public async Task<IActionResult> GetMetadataAsync(string key, [FromQuery] BlockchainType? blockchainType = null)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
            {
                return BadRequest(new { error = "Key is required" });
            }

            var metadata = await _storageService.GetStorageMetadataAsync(key, blockchainType ?? BlockchainType.NeoN3);
            
            if (metadata == null)
            {
                return NotFound(new { error = "Metadata not found" });
            }

            return Ok(metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metadata for key {Key}", key);
            return StatusCode(500, new { error = "Failed to get metadata", details = ex.Message });
        }
    }

    /// <summary>
    /// Lists storage keys with optional prefix filtering.
    /// </summary>
    /// <param name="prefix">Optional key prefix filter.</param>
    /// <param name="skip">Number of keys to skip (for pagination).</param>
    /// <param name="take">Number of keys to return (for pagination).</param>
    /// <param name="blockchainType">The blockchain type (optional, defaults to NeoN3).</param>
    /// <returns>List of storage metadata.</returns>
    [HttpGet("list")]
    public async Task<IActionResult> ListKeysAsync(
        [FromQuery] string? prefix = null, 
        [FromQuery] int skip = 0, 
        [FromQuery] int take = 100,
        [FromQuery] BlockchainType? blockchainType = null)
    {
        try
        {
            if (take > 1000)
            {
                return BadRequest(new { error = "Take parameter cannot exceed 1000" });
            }

            var keys = await _storageService.ListKeysAsync(
                prefix ?? string.Empty, 
                skip, 
                take, 
                blockchainType ?? BlockchainType.NeoN3);

            return Ok(new { keys, count = keys.Count(), skip, take });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing keys with prefix {Prefix}", prefix);
            return StatusCode(500, new { error = "Failed to list keys", details = ex.Message });
        }
    }

    /// <summary>
    /// Updates metadata for stored data.
    /// </summary>
    /// <param name="key">The data key.</param>
    /// <param name="metadata">The new metadata.</param>
    /// <param name="blockchainType">The blockchain type (optional, defaults to NeoN3).</param>
    /// <returns>Success or failure result.</returns>
    [HttpPut("metadata/{key}")]
    public async Task<IActionResult> UpdateMetadataAsync(
        string key, 
        [FromBody] StorageMetadata metadata,
        [FromQuery] BlockchainType? blockchainType = null)
    {
        try
        {
            if (string.IsNullOrEmpty(key) || metadata == null)
            {
                return BadRequest(new { error = "Key and metadata are required" });
            }

            var success = await _storageService.UpdateMetadataAsync(key, metadata, blockchainType ?? BlockchainType.NeoN3);
            
            if (!success)
            {
                return NotFound(new { error = "Data not found or metadata could not be updated" });
            }

            return Ok(new { message = "Metadata updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating metadata for key {Key}", key);
            return StatusCode(500, new { error = "Failed to update metadata", details = ex.Message });
        }
    }
}

/// <summary>
/// Request model for storing data.
/// </summary>
public class StoreDataRequest
{
    public string Key { get; set; } = string.Empty;
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public bool Encrypt { get; set; } = true;
    public bool Compress { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
    public string? EncryptionKeyId { get; set; }
    public Dictionary<string, string>? CustomMetadata { get; set; }
    public BlockchainType? BlockchainType { get; set; }
}
