using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Randomness;

namespace NeoServiceLayer.Web.Controllers;

/// <summary>
/// API controller for randomness generation operations.
/// </summary>
[Tags("Randomness")]
public class RandomnessController : BaseApiController
{
    private readonly IRandomnessService _randomnessService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RandomnessController"/> class.
    /// </summary>
    /// <param name="randomnessService">The randomness service.</param>
    /// <param name="logger">The logger.</param>
    public RandomnessController(
        IRandomnessService randomnessService,
        ILogger<RandomnessController> logger) : base(logger)
    {
        _randomnessService = randomnessService;
    }

    /// <summary>
    /// Generates a random number within the specified range.
    /// </summary>
    /// <param name="min">The minimum value (inclusive).</param>
    /// <param name="max">The maximum value (inclusive).</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>A random number between min and max (inclusive).</returns>
    /// <response code="200">Random number generated successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Random number generation failed.</response>
    [HttpGet("number/{min}/{max}/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<int>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> GenerateRandomNumber(
        [FromRoute] int min,
        [FromRoute] int max,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            if (min > max)
            {
                return BadRequest(CreateErrorResponse("Minimum value must be less than or equal to maximum value"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var randomNumber = await _randomnessService.GenerateRandomNumberAsync(min, max, blockchain);

            Logger.LogInformation("Generated random number {RandomNumber} between {Min} and {Max} on {BlockchainType} by user {UserId}",
                randomNumber, min, max, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(randomNumber, "Random number generated successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GenerateRandomNumber");
        }
    }

    /// <summary>
    /// Generates random bytes of the specified length.
    /// </summary>
    /// <param name="length">The number of bytes to generate.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>Random bytes as a base64 string.</returns>
    /// <response code="200">Random bytes generated successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Random bytes generation failed.</response>
    [HttpGet("bytes/{length}/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> GenerateRandomBytes(
        [FromRoute] int length,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            if (length <= 0)
            {
                return BadRequest(CreateErrorResponse("Length must be greater than zero"));
            }

            if (length > 1024)
            {
                return BadRequest(CreateErrorResponse("Length cannot exceed 1024 bytes"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var randomBytes = await _randomnessService.GenerateRandomBytesAsync(length, blockchain);
            var base64Bytes = Convert.ToBase64String(randomBytes);

            Logger.LogInformation("Generated {Length} random bytes on {BlockchainType} by user {UserId}",
                length, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(base64Bytes, "Random bytes generated successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GenerateRandomBytes");
        }
    }

    /// <summary>
    /// Generates a random string of the specified length using an optional character set.
    /// </summary>
    /// <param name="length">The length of the string to generate.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <param name="charset">Optional character set to use. If not provided, uses alphanumeric characters.</param>
    /// <returns>A random string.</returns>
    /// <response code="200">Random string generated successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Random string generation failed.</response>
    [HttpGet("string/{length}/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> GenerateRandomString(
        [FromRoute] int length,
        [FromRoute] string blockchainType,
        [FromQuery] string? charset = null)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            if (length <= 0)
            {
                return BadRequest(CreateErrorResponse("Length must be greater than zero"));
            }

            if (length > 1000)
            {
                return BadRequest(CreateErrorResponse("Length cannot exceed 1000 characters"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var randomString = await _randomnessService.GenerateRandomStringAsync(length, charset, blockchain);

            Logger.LogInformation("Generated random string of length {Length} on {BlockchainType} by user {UserId}",
                length, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(randomString, "Random string generated successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GenerateRandomString");
        }
    }

    /// <summary>
    /// Generates a verifiable random number with cryptographic proof.
    /// </summary>
    /// <param name="min">The minimum value (inclusive).</param>
    /// <param name="max">The maximum value (inclusive).</param>
    /// <param name="seed">The seed for random generation.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>A verifiable random result containing the random number and proof.</returns>
    /// <response code="200">Verifiable random number generated successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Verifiable random number generation failed.</response>
    [HttpPost("verifiable/{min}/{max}/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<VerifiableRandomResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> GenerateVerifiableRandomNumber(
        [FromRoute] int min,
        [FromRoute] int max,
        [FromBody] string seed,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            if (min > max)
            {
                return BadRequest(CreateErrorResponse("Minimum value must be less than or equal to maximum value"));
            }

            if (string.IsNullOrEmpty(seed))
            {
                return BadRequest(CreateErrorResponse("Seed cannot be null or empty"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _randomnessService.GenerateVerifiableRandomNumberAsync(min, max, seed, blockchain);

            Logger.LogInformation("Generated verifiable random number {RandomNumber} with request ID {RequestId} on {BlockchainType} by user {UserId}",
                result.Value, result.RequestId, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(result, "Verifiable random number generated successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GenerateVerifiableRandomNumber");
        }
    }

    /// <summary>
    /// Verifies the authenticity of a verifiable random result.
    /// </summary>
    /// <param name="result">The verifiable random result to verify.</param>
    /// <returns>The verification result.</returns>
    /// <response code="200">Verification completed successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Verification failed.</response>
    [HttpPost("verify")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> VerifyRandomNumber([FromBody] VerifiableRandomResult result)
    {
        try
        {
            if (result == null)
            {
                return BadRequest(CreateErrorResponse("Verifiable random result cannot be null"));
            }

            if (string.IsNullOrEmpty(result.Proof))
            {
                return BadRequest(CreateErrorResponse("Proof cannot be null or empty"));
            }

            var isValid = await _randomnessService.VerifyRandomNumberAsync(result);

            Logger.LogInformation("Verified random number {RandomNumber} with request ID {RequestId}: {IsValid} by user {UserId}",
                result.Value, result.RequestId, isValid, GetCurrentUserId());

            return Ok(CreateResponse(isValid, $"Random number verification completed - Result: {(isValid ? "Valid" : "Invalid")}"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "VerifyRandomNumber");
        }
    }
}
