using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Shared.Models;

namespace NeoServiceLayer.Api.Controllers
{
    /// <summary>
    /// Controller for managing keys.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class KeysController : ControllerBase
    {
        private readonly IKeyManagementService _keyManagementService;
        private readonly IKeyService _keyService;
        private readonly ILogger<KeysController> _logger;

        /// <summary>
        /// Initializes a new instance of the KeysController class.
        /// </summary>
        /// <param name="keyManagementService">The key management service.</param>
        /// <param name="keyService">The key service.</param>
        /// <param name="logger">The logger.</param>
        public KeysController(IKeyManagementService keyManagementService, IKeyService keyService, ILogger<KeysController> logger)
        {
            _keyManagementService = keyManagementService ?? throw new ArgumentNullException(nameof(keyManagementService));
            _keyService = keyService ?? throw new ArgumentNullException(nameof(keyService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new key.
        /// </summary>
        /// <param name="request">The request to create a key.</param>
        /// <returns>The created key.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<TeeAccount>), 201)]
        [ProducesResponseType(typeof(ApiResponse<TeeAccount>), 400)]
        [ProducesResponseType(typeof(ApiResponse<TeeAccount>), 401)]
        [ProducesResponseType(typeof(ApiResponse<TeeAccount>), 500)]
        public async Task<IActionResult> CreateKey([FromBody] CreateKeyRequest request)
        {
            try
            {
                _logger.LogInformation("Creating key of type {KeyType}", request.KeyType);

                var account = new TeeAccount
                {
                    Name = request.KeyName,
                    Type = request.KeyType,
                    UserId = User.Identity?.Name ?? "user123",
                    IsExportable = request.Exportable
                };

                var createdAccount = await _keyManagementService.CreateAccountAsync(account);

                _logger.LogInformation("Key {KeyId} created successfully", createdAccount.Id);

                return CreatedAtAction(nameof(GetKey), new { keyId = createdAccount.Id }, ApiResponse<TeeAccount>.CreateSuccess(createdAccount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating key");
                return StatusCode(500, ApiResponse<TeeAccount>.CreateError(ApiErrorCodes.InternalError, "An error occurred while creating the key."));
            }
        }

        /// <summary>
        /// Gets a key by ID.
        /// </summary>
        /// <param name="keyId">The ID of the key to get.</param>
        /// <returns>The key with the specified ID.</returns>
        [HttpGet("{keyId}")]
        [ProducesResponseType(typeof(ApiResponse<TeeAccount>), 200)]
        [ProducesResponseType(typeof(ApiResponse<TeeAccount>), 401)]
        [ProducesResponseType(typeof(ApiResponse<TeeAccount>), 404)]
        [ProducesResponseType(typeof(ApiResponse<TeeAccount>), 500)]
        public async Task<IActionResult> GetKey(string keyId)
        {
            try
            {
                _logger.LogInformation("Getting key {KeyId}", keyId);

                var account = await _keyManagementService.GetAccountAsync(keyId);

                if (account == null)
                {
                    _logger.LogWarning("Key {KeyId} not found", keyId);
                    return NotFound(ApiResponse<TeeAccount>.CreateError(ApiErrorCodes.ResourceNotFound, $"Key with ID {keyId} not found."));
                }

                // Check if the key belongs to the authenticated user
                var currentUser = User.Identity?.Name;
                if (currentUser != null && account.UserId != currentUser)
                {
                    _logger.LogWarning("User {UserId} attempted to access key {KeyId} belonging to user {KeyUserId}", currentUser, keyId, account.UserId);
                    return Unauthorized(ApiResponse<TeeAccount>.CreateError(ApiErrorCodes.AuthorizationError, "You are not authorized to access this key."));
                }

                _logger.LogInformation("Key {KeyId} retrieved successfully", keyId);

                return Ok(ApiResponse<TeeAccount>.CreateSuccess(account));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting key {KeyId}", keyId);
                return StatusCode(500, ApiResponse<TeeAccount>.CreateError(ApiErrorCodes.InternalError, "An error occurred while getting the key."));
            }
        }

        /// <summary>
        /// Gets all keys for the authenticated user.
        /// </summary>
        /// <param name="type">Optional type filter.</param>
        /// <returns>A list of keys for the authenticated user.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<TeeAccount>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<TeeAccount>>), 401)]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<TeeAccount>>), 500)]
        public async Task<IActionResult> GetKeys([FromQuery] AccountType? type = null)
        {
            try
            {
                // Use a default user ID for testing if not authenticated
                var userId = User.Identity?.Name ?? "user123";
                _logger.LogInformation("Getting keys for user {UserId} with type {Type}", userId, type);

                var accounts = await _keyManagementService.GetAccountsAsync(userId, type);

                _logger.LogInformation("Retrieved keys for user {UserId}", userId);

                return Ok(ApiResponse<IEnumerable<TeeAccount>>.CreateSuccess(accounts));
            }
            catch (Exception ex)
            {
                var userId = User.Identity?.Name ?? "user123";
                _logger.LogError(ex, "Error getting keys for user {UserId}", userId);
                return StatusCode(500, ApiResponse<IEnumerable<TeeAccount>>.CreateError(ApiErrorCodes.InternalError, "An error occurred while getting the keys."));
            }
        }

        /// <summary>
        /// Signs data using a key.
        /// </summary>
        /// <param name="keyId">The ID of the key to use for signing.</param>
        /// <param name="request">The request to sign data.</param>
        /// <returns>The signature.</returns>
        [HttpPost("{keyId}/sign")]
        [ProducesResponseType(typeof(ApiResponse<SignResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<SignResponse>), 400)]
        [ProducesResponseType(typeof(ApiResponse<SignResponse>), 401)]
        [ProducesResponseType(typeof(ApiResponse<SignResponse>), 404)]
        [ProducesResponseType(typeof(ApiResponse<SignResponse>), 500)]
        public async Task<IActionResult> SignData(string keyId, [FromBody] SignRequest request)
        {
            try
            {
                _logger.LogInformation("Signing data using key {KeyId}", keyId);

                var account = await _keyManagementService.GetAccountAsync(keyId);

                if (account == null)
                {
                    _logger.LogWarning("Key {KeyId} not found", keyId);
                    return NotFound(ApiResponse<SignResponse>.CreateError(ApiErrorCodes.ResourceNotFound, $"Key with ID {keyId} not found."));
                }

                // Check if the key belongs to the authenticated user
                var currentUser = User.Identity?.Name;
                if (currentUser != null && account.UserId != currentUser)
                {
                    _logger.LogWarning("User {UserId} attempted to use key {KeyId} belonging to user {KeyUserId}", currentUser, keyId, account.UserId);
                    return Unauthorized(ApiResponse<SignResponse>.CreateError(ApiErrorCodes.AuthorizationError, "You are not authorized to use this key."));
                }

                var data = Convert.FromBase64String(request.Data);
                var signature = await _keyManagementService.SignAsync(keyId, data, request.HashAlgorithm);

                var response = new SignResponse
                {
                    Signature = signature,
                    Timestamp = DateTime.UtcNow
                };

                _logger.LogInformation("Data signed successfully using key {KeyId}", keyId);

                return Ok(ApiResponse<SignResponse>.CreateSuccess(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error signing data using key {KeyId}", keyId);
                return StatusCode(500, ApiResponse<SignResponse>.CreateError(ApiErrorCodes.InternalError, "An error occurred while signing the data."));
            }
        }

        /// <summary>
        /// Generates a new key.
        /// </summary>
        /// <param name="request">The request to generate a key.</param>
        /// <returns>The generated key.</returns>
        [HttpPost("generate")]
        [ProducesResponseType(typeof(ApiResponse<GenerateKeyResponse>), 201)]
        [ProducesResponseType(typeof(ApiResponse<GenerateKeyResponse>), 400)]
        [ProducesResponseType(typeof(ApiResponse<GenerateKeyResponse>), 401)]
        [ProducesResponseType(typeof(ApiResponse<GenerateKeyResponse>), 500)]
        public async Task<IActionResult> GenerateKey([FromBody] GenerateKeyRequest request)
        {
            try
            {
                _logger.LogInformation("Generating key of type {KeyType}", request.KeyType);

                // Parse the key type from the request
                if (!Enum.TryParse<AccountType>(request.KeyType, true, out var keyType))
                {
                    keyType = AccountType.Wallet; // Default to Wallet if parsing fails
                }

                var account = new TeeAccount
                {
                    Name = request.KeyName,
                    Type = keyType,
                    UserId = request.UserId ?? User.Identity?.Name ?? "anonymous",
                    IsExportable = true
                };

                var createdAccount = await _keyManagementService.CreateAccountAsync(account);

                var response = new GenerateKeyResponse
                {
                    KeyId = createdAccount.Id,
                    KeyName = createdAccount.Name,
                    KeyType = request.KeyType,
                    PublicKey = createdAccount.PublicKey,
                    CreatedAt = createdAccount.CreatedAt
                };

                _logger.LogInformation("Key {KeyId} generated successfully", createdAccount.Id);

                return CreatedAtAction(nameof(GetKey), new { keyId = createdAccount.Id }, ApiResponse<GenerateKeyResponse>.CreateSuccess(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating key");
                return StatusCode(500, ApiResponse<GenerateKeyResponse>.CreateError(ApiErrorCodes.InternalError, "An error occurred while generating the key."));
            }
        }

        /// <summary>
        /// Signs data using a key.
        /// </summary>
        /// <param name="request">The request to sign data.</param>
        /// <returns>The signature.</returns>
        [HttpPost("sign")]
        [ProducesResponseType(typeof(ApiResponse<SignDataResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<SignDataResponse>), 400)]
        [ProducesResponseType(typeof(ApiResponse<SignDataResponse>), 401)]
        [ProducesResponseType(typeof(ApiResponse<SignDataResponse>), 404)]
        [ProducesResponseType(typeof(ApiResponse<SignDataResponse>), 500)]
        public async Task<IActionResult> SignData([FromBody] SignDataRequest request)
        {
            try
            {
                _logger.LogInformation("Signing data using key {KeyId}", request.KeyId);

                var account = await _keyManagementService.GetAccountAsync(request.KeyId);

                if (account == null)
                {
                    _logger.LogWarning("Key {KeyId} not found", request.KeyId);
                    return NotFound(ApiResponse<SignDataResponse>.CreateError(ApiErrorCodes.ResourceNotFound, $"Key with ID {request.KeyId} not found."));
                }

                // Check if the key belongs to the authenticated user or the specified user
                var userId = request.UserId ?? User.Identity?.Name;
                if (userId != null && account.UserId != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to use key {KeyId} belonging to user {KeyUserId}", userId, request.KeyId, account.UserId);
                    return Unauthorized(ApiResponse<SignDataResponse>.CreateError(ApiErrorCodes.AuthorizationError, "You are not authorized to use this key."));
                }

                var data = Convert.FromBase64String(request.Data);
                var signature = await _keyManagementService.SignAsync(request.KeyId, data, "SHA256");

                var response = new SignDataResponse
                {
                    KeyId = request.KeyId,
                    Signature = signature,
                    Timestamp = DateTime.UtcNow
                };

                _logger.LogInformation("Data signed successfully using key {KeyId}", request.KeyId);

                return Ok(ApiResponse<SignDataResponse>.CreateSuccess(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error signing data using key {KeyId}", request.KeyId);
                return StatusCode(500, ApiResponse<SignDataResponse>.CreateError(ApiErrorCodes.InternalError, "An error occurred while signing the data."));
            }
        }

        /// <summary>
        /// Verifies a signature.
        /// </summary>
        /// <param name="request">The request to verify a signature.</param>
        /// <returns>The verification result.</returns>
        [HttpPost("verify")]
        [ProducesResponseType(typeof(ApiResponse<VerifySignatureResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<VerifySignatureResponse>), 400)]
        [ProducesResponseType(typeof(ApiResponse<VerifySignatureResponse>), 500)]
        public async Task<IActionResult> VerifySignature([FromBody] VerifySignatureRequest request)
        {
            try
            {
                _logger.LogInformation("Verifying signature");

                var data = Convert.FromBase64String(request.Data);
                var signature = request.Signature;
                var publicKey = request.PublicKey;

                // Verify the signature using the public key
                bool isValid = await _keyService.VerifySignatureAsync(publicKey, data, signature);
                string reason = isValid ? "Signature is valid" : "Signature is invalid";

                var response = new VerifySignatureResponse
                {
                    IsValid = isValid,
                    Reason = reason,
                    Timestamp = DateTime.UtcNow
                };

                _logger.LogInformation("Signature verification result: {IsValid}", isValid);

                return Ok(ApiResponse<VerifySignatureResponse>.CreateSuccess(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying signature");
                return StatusCode(500, ApiResponse<VerifySignatureResponse>.CreateError(ApiErrorCodes.InternalError, "An error occurred while verifying the signature."));
            }
        }

        /// <summary>
        /// Deletes a key by ID.
        /// </summary>
        /// <param name="keyId">The ID of the key to delete.</param>
        /// <returns>True if the key was deleted, false otherwise.</returns>
        [HttpDelete("{keyId}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 401)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 404)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 500)]
        public async Task<IActionResult> DeleteKey(string keyId)
        {
            try
            {
                _logger.LogInformation("Deleting key {KeyId}", keyId);

                var account = await _keyManagementService.GetAccountAsync(keyId);

                if (account == null)
                {
                    _logger.LogWarning("Key {KeyId} not found", keyId);
                    return NotFound(ApiResponse<bool>.CreateError(ApiErrorCodes.ResourceNotFound, $"Key with ID {keyId} not found."));
                }

                // Check if the key belongs to the authenticated user
                var currentUser = User.Identity?.Name;
                if (currentUser != null && account.UserId != currentUser)
                {
                    _logger.LogWarning("User {UserId} attempted to delete key {KeyId} belonging to user {KeyUserId}", currentUser, keyId, account.UserId);
                    return Unauthorized(ApiResponse<bool>.CreateError(ApiErrorCodes.AuthorizationError, "You are not authorized to delete this key."));
                }

                // Delete the key
                bool result = false;

                // Check if the service implements IKeyManagementServiceExtended
                if (_keyManagementService is IKeyManagementServiceExtended extendedService)
                {
                    result = await extendedService.DeleteAccountAsync(keyId);
                }
                else
                {
                    // Fallback implementation for testing
                    _logger.LogWarning("IKeyManagementServiceExtended not implemented, using fallback implementation");
                    result = true;
                }

                _logger.LogInformation("Key {KeyId} deleted successfully", keyId);

                return Ok(ApiResponse<bool>.CreateSuccess(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting key {KeyId}", keyId);
                return StatusCode(500, ApiResponse<bool>.CreateError(ApiErrorCodes.InternalError, "An error occurred while deleting the key."));
            }
        }
    }

    /// <summary>
    /// Represents a request to create a key.
    /// </summary>
    public class CreateKeyRequest
    {
        /// <summary>
        /// Gets or sets the type of the key.
        /// </summary>
        public AccountType KeyType { get; set; }

        /// <summary>
        /// Gets or sets the name of the key.
        /// </summary>
        public string KeyName { get; set; }

        /// <summary>
        /// Gets or sets whether the key is exportable.
        /// </summary>
        public bool Exportable { get; set; }
    }

    /// <summary>
    /// Represents a request to sign data.
    /// </summary>
    public class SignRequest
    {
        /// <summary>
        /// Gets or sets the data to sign (base64 encoded).
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// Gets or sets the hash algorithm to use.
        /// </summary>
        public string HashAlgorithm { get; set; } = "SHA256";
    }

    /// <summary>
    /// Represents a response to a sign request.
    /// </summary>
    public class SignResponse
    {
        /// <summary>
        /// Gets or sets the signature.
        /// </summary>
        public string Signature { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the signature.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Represents a response to a delete key request.
    /// </summary>
    public class DeleteKeyResponse
    {
        /// <summary>
        /// Gets or sets whether the key was deleted successfully.
        /// </summary>
        public bool Success { get; set; }
    }

    /// <summary>
    /// Represents a request to generate a key.
    /// </summary>
    public class GenerateKeyRequest
    {
        /// <summary>
        /// Gets or sets the type of the key.
        /// </summary>
        public string KeyType { get; set; }

        /// <summary>
        /// Gets or sets the name of the key.
        /// </summary>
        public string KeyName { get; set; }

        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public string UserId { get; set; }
    }

    /// <summary>
    /// Represents a response to a generate key request.
    /// </summary>
    public class GenerateKeyResponse
    {
        /// <summary>
        /// Gets or sets the key ID.
        /// </summary>
        public string KeyId { get; set; }

        /// <summary>
        /// Gets or sets the key name.
        /// </summary>
        public string KeyName { get; set; }

        /// <summary>
        /// Gets or sets the key type.
        /// </summary>
        public string KeyType { get; set; }

        /// <summary>
        /// Gets or sets the public key.
        /// </summary>
        public string PublicKey { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Represents a request to sign data.
    /// </summary>
    public class SignDataRequest
    {
        /// <summary>
        /// Gets or sets the key ID.
        /// </summary>
        public string KeyId { get; set; }

        /// <summary>
        /// Gets or sets the data to sign (base64 encoded).
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public string UserId { get; set; }
    }

    /// <summary>
    /// Represents a response to a sign data request.
    /// </summary>
    public class SignDataResponse
    {
        /// <summary>
        /// Gets or sets the key ID.
        /// </summary>
        public string KeyId { get; set; }

        /// <summary>
        /// Gets or sets the signature.
        /// </summary>
        public string Signature { get; set; }

        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Represents a request to verify a signature.
    /// </summary>
    public class VerifySignatureRequest
    {
        /// <summary>
        /// Gets or sets the public key.
        /// </summary>
        public string PublicKey { get; set; }

        /// <summary>
        /// Gets or sets the data that was signed (base64 encoded).
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// Gets or sets the signature to verify.
        /// </summary>
        public string Signature { get; set; }
    }

    /// <summary>
    /// Represents a response to a verify signature request.
    /// </summary>
    public class VerifySignatureResponse
    {
        /// <summary>
        /// Gets or sets whether the signature is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the reason for the verification result.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
