using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Services.Randomness;
using NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.Api.Controllers;

/// <summary>
/// API controller for randomness generation services.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/randomness")]
[ApiVersion("1.0")]
[Authorize]
public class RandomnessController : ControllerBase
{
    private readonly IRandomnessService _randomnessService;
    private readonly ILogger<RandomnessController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RandomnessController"/> class.
    /// </summary>
    /// <param name="randomnessService">The randomness service.</param>
    /// <param name="logger">The logger.</param>
    public RandomnessController(IRandomnessService randomnessService, ILogger<RandomnessController> logger)
    {
        _randomnessService = randomnessService ?? throw new ArgumentNullException(nameof(randomnessService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates a random number within a specified range.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="min">The minimum value (inclusive).</param>
    /// <param name="max">The maximum value (exclusive).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A random number.</returns>
    [HttpGet("number/{blockchainType}")]
    [ProducesResponseType(typeof(RandomNumberResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RandomNumberResponse>> GenerateRandomNumberAsync(
        [FromRoute] BlockchainType blockchainType,
        [FromQuery] long min = 0,
        [FromQuery] long max = long.MaxValue,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (min >= max)
            {
                return BadRequest("Minimum value must be less than maximum value.");
            }

            var result = await _randomnessService.GenerateRandomNumberAsync(min, max, blockchainType, cancellationToken);

            var response = new RandomNumberResponse
            {
                Value = result.Value,
                Min = min,
                Max = max,
                Timestamp = result.Timestamp,
                RequestId = result.RequestId,
                Proof = result.Proof
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid parameters for random number generation");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating random number");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while generating the random number");
        }
    }

    /// <summary>
    /// Generates random bytes.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="length">The number of bytes to generate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Random bytes as base64 string.</returns>
    [HttpGet("bytes/{blockchainType}")]
    [ProducesResponseType(typeof(RandomBytesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RandomBytesResponse>> GenerateRandomBytesAsync(
        [FromRoute] BlockchainType blockchainType,
        [FromQuery] int length = 32,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (length <= 0 || length > 1024)
            {
                return BadRequest("Length must be between 1 and 1024 bytes.");
            }

            var result = await _randomnessService.GenerateRandomBytesAsync(length, blockchainType, cancellationToken);

            var response = new RandomBytesResponse
            {
                Data = Convert.ToBase64String(result.Data),
                Length = length,
                Timestamp = result.Timestamp,
                RequestId = result.RequestId,
                Proof = result.Proof
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid parameters for random bytes generation");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating random bytes");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while generating random bytes");
        }
    }

    /// <summary>
    /// Generates a random string.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="length">The length of the string to generate.</param>
    /// <param name="charset">The character set to use (alphanumeric, letters, digits, hex).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A random string.</returns>
    [HttpGet("string/{blockchainType}")]
    [ProducesResponseType(typeof(RandomStringResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RandomStringResponse>> GenerateRandomStringAsync(
        [FromRoute] BlockchainType blockchainType,
        [FromQuery] int length = 16,
        [FromQuery] string charset = "alphanumeric",
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (length <= 0 || length > 256)
            {
                return BadRequest("Length must be between 1 and 256 characters.");
            }

            var validCharsets = new[] { "alphanumeric", "letters", "digits", "hex" };
            if (!validCharsets.Contains(charset.ToLowerInvariant()))
            {
                return BadRequest($"Invalid charset. Valid options are: {string.Join(", ", validCharsets)}");
            }

            var result = await _randomnessService.GenerateRandomStringAsync(length, charset, blockchainType, cancellationToken);

            var response = new RandomStringResponse
            {
                Value = result.Value,
                Length = length,
                Charset = charset,
                Timestamp = result.Timestamp,
                RequestId = result.RequestId,
                Proof = result.Proof
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid parameters for random string generation");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating random string");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while generating the random string");
        }
    }

    /// <summary>
    /// Generates verifiable random value with cryptographic proof.
    /// </summary>
    /// <param name="request">The verifiable random generation request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A verifiable random value with proof.</returns>
    [HttpPost("verifiable/{blockchainType}")]
    [ProducesResponseType(typeof(VerifiableRandomResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VerifiableRandomResponse>> GenerateVerifiableRandomAsync(
        [FromBody] VerifiableRandomRequest request,
        [FromRoute] BlockchainType blockchainType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var result = await _randomnessService.GenerateVerifiableRandomAsync(
                request.Seed,
                request.ProofType,
                blockchainType,
                cancellationToken);

            var response = new VerifiableRandomResponse
            {
                Value = Convert.ToBase64String(result.Value),
                Seed = request.Seed,
                Proof = result.Proof,
                PublicKey = result.PublicKey,
                Signature = result.Signature,
                Timestamp = result.Timestamp,
                RequestId = result.RequestId
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid parameters for verifiable random generation");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating verifiable random value");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while generating verifiable random value");
        }
    }

    /// <summary>
    /// Verifies a random number generated by the service.
    /// </summary>
    /// <param name="request">The verification request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The verification result.</returns>
    [HttpPost("verify")]
    [ProducesResponseType(typeof(RandomVerificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RandomVerificationResponse>> VerifyRandomNumberAsync(
        [FromBody] RandomVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var isValid = await _randomnessService.VerifyRandomNumberAsync(
                request.Value,
                request.Proof,
                request.Signature,
                cancellationToken);

            var response = new RandomVerificationResponse
            {
                IsValid = isValid,
                Value = request.Value,
                VerifiedAt = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid parameters for random number verification");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying random number");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while verifying the random number");
        }
    }
}

/// <summary>
/// Response model for random number generation.
/// </summary>
public class RandomNumberResponse
{
    /// <summary>
    /// Gets or sets the random value.
    /// </summary>
    public long Value { get; set; }

    /// <summary>
    /// Gets or sets the minimum value.
    /// </summary>
    public long Min { get; set; }

    /// <summary>
    /// Gets or sets the maximum value.
    /// </summary>
    public long Max { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the request ID.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cryptographic proof.
    /// </summary>
    public string? Proof { get; set; }
}

/// <summary>
/// Response model for random bytes generation.
/// </summary>
public class RandomBytesResponse
{
    /// <summary>
    /// Gets or sets the random data as base64 string.
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the length in bytes.
    /// </summary>
    public int Length { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the request ID.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cryptographic proof.
    /// </summary>
    public string? Proof { get; set; }
}

/// <summary>
/// Response model for random string generation.
/// </summary>
public class RandomStringResponse
{
    /// <summary>
    /// Gets or sets the random string value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the length in characters.
    /// </summary>
    public int Length { get; set; }

    /// <summary>
    /// Gets or sets the character set used.
    /// </summary>
    public string Charset { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the request ID.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cryptographic proof.
    /// </summary>
    public string? Proof { get; set; }
}

/// <summary>
/// Request model for verifiable random generation.
/// </summary>
public class VerifiableRandomRequest
{
    /// <summary>
    /// Gets or sets the seed value.
    /// </summary>
    public string? Seed { get; set; }

    /// <summary>
    /// Gets or sets the proof type.
    /// </summary>
    public string ProofType { get; set; } = "VRF";
}

/// <summary>
/// Response model for verifiable random generation.
/// </summary>
public class VerifiableRandomResponse
{
    /// <summary>
    /// Gets or sets the random value as base64 string.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the seed used.
    /// </summary>
    public string? Seed { get; set; }

    /// <summary>
    /// Gets or sets the cryptographic proof.
    /// </summary>
    public string Proof { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the public key for verification.
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the signature.
    /// </summary>
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the request ID.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;
}

/// <summary>
/// Request model for random number verification.
/// </summary>
public class RandomVerificationRequest
{
    /// <summary>
    /// Gets or sets the value to verify.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the proof to verify against.
    /// </summary>
    public string Proof { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the signature to verify.
    /// </summary>
    public string Signature { get; set; } = string.Empty;
}

/// <summary>
/// Response model for random number verification.
/// </summary>
public class RandomVerificationResponse
{
    /// <summary>
    /// Gets or sets whether the verification was successful.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the verified value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the verification timestamp.
    /// </summary>
    public DateTime VerifiedAt { get; set; }
}