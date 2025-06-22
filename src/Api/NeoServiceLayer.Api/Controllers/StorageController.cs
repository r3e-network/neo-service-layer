using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Storage;
using NeoServiceLayer.Services.Storage.Models;

namespace NeoServiceLayer.Api.Controllers;

/// <summary>
/// Controller for privacy-preserving data storage operations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/storage")]
[Authorize]
[Tags("Storage")]
public class StorageController : BaseApiController
{
    private readonly IStorageService _storageService;
    private readonly ILogger<StorageController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageController"/> class.
    /// </summary>
    /// <param name="storageService">The storage service.</param>
    /// <param name="logger">The logger.</param>
    public StorageController(IStorageService storageService, ILogger<StorageController> logger)
        : base(logger)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Stores data in the privacy-preserving storage.
    /// </summary>
    /// <param name="request">The storage request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The storage metadata.</returns>
    /// <response code="200">Data stored successfully.</response>
    /// <response code="400">Invalid storage request.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="413">Payload too large.</response>
    [HttpPost("{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<StorageMetadata>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 413)]
    public async Task<IActionResult> StoreData(
        [FromBody] object request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);

            // Parse the request object to extract key and data
            var requestDict = request as Dictionary<string, object> ??
                System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                    System.Text.Json.JsonSerializer.Serialize(request));

            if (requestDict == null || !requestDict.ContainsKey("key") || !requestDict.ContainsKey("data"))
            {
                return BadRequest(CreateErrorResponse("Request must contain 'key' and 'data' properties"));
            }

            var key = requestDict["key"].ToString();
            var dataStr = requestDict["data"].ToString();
            var dataBytes = System.Text.Encoding.UTF8.GetBytes(dataStr ?? string.Empty);

            // Extract storage options from request if provided
            var options = new StorageOptions();
            if (requestDict.ContainsKey("options"))
            {
                var optionsJson = System.Text.Json.JsonSerializer.Serialize(requestDict["options"]);
                var requestOptions = System.Text.Json.JsonSerializer.Deserialize<StorageOptions>(optionsJson);
                if (requestOptions != null)
                {
                    options = requestOptions;
                }
            }

            var metadata = await _storageService.StoreDataAsync(key, dataBytes, options, blockchain);

            _logger.LogInformation("Data stored successfully with key: {Key} on {Blockchain}",
                key, blockchainType);

            return Ok(CreateResponse(metadata, "Data stored successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid storage request");
            return BadRequest(CreateErrorResponse($"Invalid storage request: {ex.Message}"));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("size"))
        {
            _logger.LogWarning(ex, "Storage size limit exceeded");
            return StatusCode(413, CreateErrorResponse("Storage size limit exceeded"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing data");
            return StatusCode(500, CreateErrorResponse($"Failed to store data: {ex.Message}"));
        }
    }

    /// <summary>
    /// Retrieves data from the privacy-preserving storage.
    /// </summary>
    /// <param name="storageId">The storage item ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The storage response containing the data.</returns>
    /// <response code="200">Data retrieved successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Storage item not found.</response>
    [HttpGet("{storageId}/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> RetrieveData(
        [FromRoute] string storageId,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var response = await _storageService.RetrieveDataAsync(storageId, blockchain);

            if (response == null)
            {
                return NotFound(CreateErrorResponse($"Storage item not found: {storageId}"));
            }

            _logger.LogInformation("Data retrieved successfully for ID: {StorageId} from {Blockchain}",
                storageId, blockchainType);

            return Ok(CreateResponse(response, "Data retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving data for ID: {StorageId}", storageId);
            return StatusCode(500, CreateErrorResponse($"Failed to retrieve data: {ex.Message}"));
        }
    }

    /// <summary>
    /// Updates existing data in the privacy-preserving storage.
    /// </summary>
    /// <param name="storageId">The storage item ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>Success indicator.</returns>
    /// <response code="200">Data updated successfully.</response>
    /// <response code="400">Invalid update request.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Storage item not found.</response>
    [HttpPut("{storageId}/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> UpdateData(
        [FromRoute] string storageId,
        [FromBody] object request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);

            // Parse the request object to extract data
            var requestDict = request as Dictionary<string, object> ??
                System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                    System.Text.Json.JsonSerializer.Serialize(request));

            if (requestDict == null || !requestDict.ContainsKey("data"))
            {
                return BadRequest(CreateErrorResponse("Request must contain 'data' property"));
            }

            var dataStr = requestDict["data"].ToString();
            var dataBytes = System.Text.Encoding.UTF8.GetBytes(dataStr ?? string.Empty);

            // Extract storage options from request if provided
            var options = new StorageOptions();
            if (requestDict.ContainsKey("options"))
            {
                var optionsJson = System.Text.Json.JsonSerializer.Serialize(requestDict["options"]);
                var requestOptions = System.Text.Json.JsonSerializer.Deserialize<StorageOptions>(optionsJson);
                if (requestOptions != null)
                {
                    options = requestOptions;
                }
            }

            // Delete existing data first
            var deleteSuccess = await _storageService.DeleteDataAsync(storageId, blockchain);
            if (!deleteSuccess)
            {
                return NotFound(CreateErrorResponse($"Storage item not found: {storageId}"));
            }

            // Store new data with the same key
            var metadata = await _storageService.StoreDataAsync(storageId, dataBytes, options, blockchain);

            _logger.LogInformation("Data updated successfully for key: {Key} on {Blockchain}",
                storageId, blockchainType);

            return Ok(CreateResponse(true, "Data updated successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid update request for ID: {StorageId}", storageId);
            return BadRequest(CreateErrorResponse($"Invalid update request: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating data for ID: {StorageId}", storageId);
            return StatusCode(500, CreateErrorResponse($"Failed to update data: {ex.Message}"));
        }
    }

    /// <summary>
    /// Deletes data from the privacy-preserving storage.
    /// </summary>
    /// <param name="storageId">The storage item ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>Success indicator.</returns>
    /// <response code="200">Data deleted successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Storage item not found.</response>
    [HttpDelete("{storageId}/{blockchainType}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> DeleteData(
        [FromRoute] string storageId,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var success = await _storageService.DeleteDataAsync(storageId, blockchain);

            if (!success)
            {
                return NotFound(CreateErrorResponse($"Storage item not found: {storageId}"));
            }

            _logger.LogInformation("Data deleted successfully for ID: {StorageId} from {Blockchain}",
                storageId, blockchainType);

            return Ok(CreateResponse(success, "Data deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting data for ID: {StorageId}", storageId);
            return StatusCode(500, CreateErrorResponse($"Failed to delete data: {ex.Message}"));
        }
    }

    /// <summary>
    /// Lists storage items for the authenticated user.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <param name="skip">Number of items to skip (for pagination).</param>
    /// <param name="take">Number of items to take (for pagination).</param>
    /// <returns>List of storage metadata.</returns>
    /// <response code="200">Storage items retrieved successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpGet("list/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<StorageMetadata>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> ListStorageItems(
        [FromRoute] string blockchainType,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            if (skip < 0 || take <= 0 || take > 100)
            {
                return BadRequest(CreateErrorResponse("Invalid pagination parameters"));
            }

            var blockchain = ParseBlockchainType(blockchainType);

            // Get the user's key prefix from claims or use empty for all items
            var userPrefix = User.Identity?.Name ?? string.Empty;

            var items = await _storageService.ListKeysAsync(userPrefix, skip, take, blockchain);
            var itemsList = items.ToList();

            // Calculate pagination values
            var totalItems = itemsList.Count; // This is approximate
            var currentPage = (skip / take) + 1;
            var totalPages = (int)Math.Ceiling((double)totalItems / take);

            var paginatedResponse = new PaginatedResponse<StorageMetadata>
            {
                Success = true,
                Data = itemsList,
                Message = "Storage items retrieved successfully",
                Timestamp = DateTime.UtcNow,
                Page = currentPage,
                PageSize = take,
                TotalItems = totalItems,
                TotalPages = totalPages
            };

            _logger.LogInformation("Listed {Count} storage items for user {User} on {Blockchain}",
                itemsList.Count, userPrefix, blockchainType);

            return Ok(paginatedResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing storage items");
            return StatusCode(500, CreateErrorResponse($"Failed to list storage items: {ex.Message}"));
        }
    }

    /// <summary>
    /// Gets metadata for a specific storage item.
    /// </summary>
    /// <param name="storageId">The storage item ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The storage metadata.</returns>
    /// <response code="200">Metadata retrieved successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Storage item not found.</response>
    [HttpGet("{storageId}/metadata/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<StorageMetadata>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetMetadata(
        [FromRoute] string storageId,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var metadata = await _storageService.GetMetadataAsync(storageId, blockchain);

            if (metadata == null)
            {
                return NotFound(CreateErrorResponse($"Storage item not found: {storageId}"));
            }

            return Ok(CreateResponse(metadata, "Metadata retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving metadata for ID: {StorageId}", storageId);
            return StatusCode(500, CreateErrorResponse($"Failed to retrieve metadata: {ex.Message}"));
        }
    }

    /// <summary>
    /// Shares a storage item with another user.
    /// </summary>
    /// <param name="storageId">The storage item ID.</param>
    /// <param name="request">The share request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The share token.</returns>
    /// <response code="200">Storage item shared successfully.</response>
    /// <response code="400">Invalid share request.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Storage item not found.</response>
    [HttpPost("{storageId}/share/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> ShareData(
        [FromRoute] string storageId,
        [FromBody] object request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);

            // Parse the share request
            var requestDict = request as Dictionary<string, object> ??
                System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                    System.Text.Json.JsonSerializer.Serialize(request));

            if (requestDict == null || !requestDict.ContainsKey("recipientId"))
            {
                return BadRequest(CreateErrorResponse("Request must contain 'recipientId' property"));
            }

            var recipientId = requestDict["recipientId"].ToString();

            // Get the existing metadata
            var metadata = await _storageService.GetMetadataAsync(storageId, blockchain);
            if (metadata == null)
            {
                return NotFound(CreateErrorResponse($"Storage item not found: {storageId}"));
            }

            // Add recipient to access control list
            if (!metadata.AccessControlList.Contains(recipientId!))
            {
                metadata.AccessControlList.Add(recipientId!);
            }

            // Update the metadata with new access control
            var updateSuccess = await _storageService.UpdateMetadataAsync(storageId, metadata, blockchain);
            if (!updateSuccess)
            {
                return StatusCode(500, CreateErrorResponse("Failed to update access control"));
            }

            // Generate a share token (simplified - in production this would be a secure token)
            var shareToken = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes($"{storageId}:{recipientId}:{DateTime.UtcNow.Ticks}"));

            _logger.LogInformation("Shared storage item {StorageId} with user {RecipientId} on {Blockchain}",
                storageId, recipientId, blockchainType);

            return Ok(CreateResponse(shareToken, "Storage item shared successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid share request for ID: {StorageId}", storageId);
            return BadRequest(CreateErrorResponse($"Invalid share request: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sharing storage item: {StorageId}", storageId);
            return StatusCode(500, CreateErrorResponse($"Failed to share storage item: {ex.Message}"));
        }
    }
}
