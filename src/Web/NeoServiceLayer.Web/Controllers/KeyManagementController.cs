using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.KeyManagement;
using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Web.Controllers;

/// <summary>
/// API controller for key management operations.
/// </summary>
[Tags("Key Management")]
public class KeyManagementController : BaseApiController
{
    private readonly IKeyManagementService _keyManagementService;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyManagementController"/> class.
    /// </summary>
    /// <param name="keyManagementService">The key management service.</param>
    /// <param name="logger">The logger.</param>
    public KeyManagementController(
        IKeyManagementService keyManagementService,
        ILogger<KeyManagementController> logger) : base(logger)
    {
        _keyManagementService = keyManagementService;
    }

    /// <summary>
    /// Generates a new cryptographic key.
    /// </summary>
    /// <param name="request">The key generation request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The generated key metadata.</returns>
    /// <response code="200">Key generated successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("generate/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager")]
    [ProducesResponseType(typeof(ApiResponse<KeyMetadata>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> GenerateKey(
        [FromBody] GenerateKeyRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var keyMetadata = await _keyManagementService.GenerateKeyAsync(
                request.KeyId,
                request.KeyType,
                request.KeyUsage,
                request.Exportable,
                request.Description,
                blockchain);

            Logger.LogInformation("Generated key {KeyId} for user {UserId} on {BlockchainType}",
                request.KeyId, GetCurrentUserId(), blockchainType);

            return Ok(CreateResponse(keyMetadata, "Key generated successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GenerateKey");
        }
    }

    /// <summary>
    /// Lists keys with pagination.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <param name="page">The page number (default: 1).</param>
    /// <param name="pageSize">The page size (default: 20, max: 100).</param>
    /// <returns>The paginated list of keys.</returns>
    /// <response code="200">Keys retrieved successfully.</response>
    /// <response code="400">Invalid pagination parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpGet("list/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,KeyUser")]
    [ProducesResponseType(typeof(PaginatedResponse<KeyMetadata>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> ListKeys(
        [FromRoute] string blockchainType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            if (page < 1)
            {
                return BadRequest(CreateErrorResponse("Page number must be greater than 0"));
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(CreateErrorResponse("Page size must be between 1 and 100"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var skip = (page - 1) * pageSize;
            var keys = await _keyManagementService.ListKeysAsync(skip, pageSize, blockchain);

            var response = new PaginatedResponse<KeyMetadata>
            {
                Success = true,
                Data = keys,
                Message = "Keys retrieved successfully",
                Timestamp = DateTime.UtcNow,
                Page = page,
                PageSize = pageSize,
                TotalItems = keys.Count(),
                TotalPages = (int)Math.Ceiling((double)keys.Count() / pageSize)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "ListKeys");
        }
    }

    /// <summary>
    /// Signs data using a specified key.
    /// </summary>
    /// <param name="request">The signing request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The signature in hexadecimal format.</returns>
    /// <response code="200">Data signed successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Key not found.</response>
    [HttpPost("sign/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,KeyUser")]
    [ProducesResponseType(typeof(ApiResponse<SignDataResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> SignData(
        [FromBody] SignDataRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var signature = await _keyManagementService.SignDataAsync(
                request.KeyId,
                request.DataHex,
                request.SigningAlgorithm,
                blockchain);

            var response = new SignDataResponse
            {
                KeyId = request.KeyId,
                DataHex = request.DataHex,
                SignatureHex = signature,
                SigningAlgorithm = request.SigningAlgorithm,
                Timestamp = DateTime.UtcNow
            };

            Logger.LogInformation("Signed data with key {KeyId} for user {UserId} on {BlockchainType}",
                request.KeyId, GetCurrentUserId(), blockchainType);

            return Ok(CreateResponse(response, "Data signed successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "SignData");
        }
    }
}

#region Request/Response Models

/// <summary>
/// Request model for generating a new key.
/// </summary>
public class GenerateKeyRequest
{
    /// <summary>
    /// Gets or sets the key ID.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the key type (e.g., "Secp256k1", "Ed25519", "RSA").
    /// </summary>
    [Required]
    [StringLength(50)]
    public string KeyType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the key usage (e.g., "Sign,Verify", "Encrypt,Decrypt").
    /// </summary>
    [Required]
    [StringLength(100)]
    public string KeyUsage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the key is exportable.
    /// </summary>
    public bool Exportable { get; set; }

    /// <summary>
    /// Gets or sets the key description.
    /// </summary>
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Request model for signing data.
/// </summary>
public class SignDataRequest
{
    /// <summary>
    /// Gets or sets the key ID.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data to sign in hexadecimal format.
    /// </summary>
    [Required]
    public string DataHex { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the signing algorithm.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string SigningAlgorithm { get; set; } = string.Empty;
}

/// <summary>
/// Response model for signing data.
/// </summary>
public class SignDataResponse
{
    /// <summary>
    /// Gets or sets the key ID used for signing.
    /// </summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original data in hexadecimal format.
    /// </summary>
    public string DataHex { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the signature in hexadecimal format.
    /// </summary>
    public string SignatureHex { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the signing algorithm used.
    /// </summary>
    public string SigningAlgorithm { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the data was signed.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

#endregion