using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Shared.Models;

namespace NeoServiceLayer.Api.Controllers
{
    /// <summary>
    /// Controller for generating random numbers.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class RandomnessController : ControllerBase
    {
        private readonly IRandomnessService _randomnessService;
        private readonly ILogger<RandomnessController> _logger;

        /// <summary>
        /// Initializes a new instance of the RandomnessController class.
        /// </summary>
        /// <param name="randomnessService">The randomness service.</param>
        /// <param name="logger">The logger.</param>
        public RandomnessController(IRandomnessService randomnessService, ILogger<RandomnessController> logger)
        {
            _randomnessService = randomnessService ?? throw new ArgumentNullException(nameof(randomnessService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Generates random numbers.
        /// </summary>
        /// <param name="request">The request to generate random numbers.</param>
        /// <returns>The generated random numbers.</returns>
        [HttpPost]
        [HttpPost("numbers")]
        [ProducesResponseType(typeof(ApiResponse<RandomNumbersResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<RandomNumbersResponse>), 400)]
        [ProducesResponseType(typeof(ApiResponse<RandomNumbersResponse>), 401)]
        [ProducesResponseType(typeof(ApiResponse<RandomNumbersResponse>), 500)]
        public async Task<IActionResult> GenerateRandomNumbers([FromBody] GenerateRandomNumbersRequest request)
        {
            try
            {
                _logger.LogInformation("Generating {Count} random numbers between {Min} and {Max}", request.Count, request.Min, request.Max);

                if (request.Count <= 0)
                {
                    _logger.LogWarning("Invalid count: {Count}", request.Count);
                    return BadRequest(ApiResponse<RandomNumbersResponse>.CreateError(ApiErrorCodes.ValidationError, "Count must be greater than 0."));
                }

                if (request.Min >= request.Max)
                {
                    _logger.LogWarning("Invalid range: {Min} to {Max}", request.Min, request.Max);
                    return BadRequest(ApiResponse<RandomNumbersResponse>.CreateError(ApiErrorCodes.ValidationError, "Min must be less than Max."));
                }

                var randomNumbers = await _randomnessService.GenerateRandomNumbersAsync(request.Count, request.Min, request.Max, request.Seed);

                // Generate a proof for the random numbers
                var proof = await GenerateProofAsync(randomNumbers.ToList(), request.Seed);

                var response = new RandomNumbersResponse
                {
                    RandomNumbers = randomNumbers,
                    Proof = proof,
                    Seed = request.Seed,
                    Timestamp = DateTime.UtcNow
                };

                _logger.LogInformation("Generated {Count} random numbers successfully", request.Count);

                return Ok(ApiResponse<RandomNumbersResponse>.CreateSuccess(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating random numbers");
                return StatusCode(500, ApiResponse<RandomNumbersResponse>.CreateError(ApiErrorCodes.InternalError, "An error occurred while generating random numbers."));
            }
        }

        /// <summary>
        /// Verifies the randomness proof.
        /// </summary>
        /// <param name="request">The request to verify the randomness proof.</param>
        /// <returns>The verification result.</returns>
        [HttpPost("verify")]
        [ProducesResponseType(typeof(ApiResponse<VerifyRandomnessResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<VerifyRandomnessResponse>), 400)]
        [ProducesResponseType(typeof(ApiResponse<VerifyRandomnessResponse>), 401)]
        [ProducesResponseType(typeof(ApiResponse<VerifyRandomnessResponse>), 500)]
        public async Task<IActionResult> VerifyRandomness([FromBody] VerifyRandomnessRequest request)
        {
            try
            {
                _logger.LogInformation("Verifying randomness proof");

                if (request.RandomNumbers == null || !request.RandomNumbers.Any())
                {
                    _logger.LogWarning("Random numbers are null or empty");
                    return BadRequest(ApiResponse<VerifyRandomnessResponse>.CreateError(ApiErrorCodes.ValidationError, "Random numbers are required."));
                }

                if (string.IsNullOrEmpty(request.Proof))
                {
                    _logger.LogWarning("Proof is null or empty");
                    return BadRequest(ApiResponse<VerifyRandomnessResponse>.CreateError(ApiErrorCodes.ValidationError, "Proof is required."));
                }

                var isValid = await _randomnessService.VerifyRandomnessProofAsync(request.RandomNumbers, request.Proof, request.Seed);

                var response = new VerifyRandomnessResponse
                {
                    Valid = isValid
                };

                _logger.LogInformation("Randomness proof verification completed with result: {IsValid}", isValid);

                return Ok(ApiResponse<VerifyRandomnessResponse>.CreateSuccess(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying randomness proof");
                return StatusCode(500, ApiResponse<VerifyRandomnessResponse>.CreateError(ApiErrorCodes.InternalError, "An error occurred while verifying the randomness proof."));
            }
        }

        /// <summary>
        /// Generates a random string.
        /// </summary>
        /// <param name="request">The request to generate a random string.</param>
        /// <returns>The generated random string.</returns>
        [HttpPost("string")]
        [ProducesResponseType(typeof(ApiResponse<RandomStringResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<RandomStringResponse>), 400)]
        [ProducesResponseType(typeof(ApiResponse<RandomStringResponse>), 401)]
        [ProducesResponseType(typeof(ApiResponse<RandomStringResponse>), 500)]
        public async Task<IActionResult> GenerateRandomString([FromBody] GenerateRandomStringRequest request)
        {
            try
            {
                _logger.LogInformation("Generating random string of length {Length}", request.Length);

                if (request.Length <= 0)
                {
                    _logger.LogWarning("Invalid length: {Length}", request.Length);
                    return BadRequest(ApiResponse<RandomStringResponse>.CreateError(ApiErrorCodes.ValidationError, "Length must be greater than 0."));
                }

                var charset = string.IsNullOrEmpty(request.Charset) ? "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789" : request.Charset;
                var randomString = await _randomnessService.GenerateRandomStringAsync(request.Length, charset, request.Seed);

                // Generate a proof for the random string
                var proof = await GenerateProofAsync(randomString, request.Seed);

                var response = new RandomStringResponse
                {
                    RandomString = randomString,
                    Proof = proof,
                    Seed = request.Seed,
                    Timestamp = DateTime.UtcNow
                };

                _logger.LogInformation("Generated random string successfully");

                return Ok(ApiResponse<RandomStringResponse>.CreateSuccess(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating random string");
                return StatusCode(500, ApiResponse<RandomStringResponse>.CreateError(ApiErrorCodes.InternalError, "An error occurred while generating a random string."));
            }
        }

        /// <summary>
        /// Generates random bytes.
        /// </summary>
        /// <param name="request">The request to generate random bytes.</param>
        /// <returns>The generated random bytes.</returns>
        [HttpPost("bytes")]
        [ProducesResponseType(typeof(ApiResponse<RandomBytesResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<RandomBytesResponse>), 400)]
        [ProducesResponseType(typeof(ApiResponse<RandomBytesResponse>), 401)]
        [ProducesResponseType(typeof(ApiResponse<RandomBytesResponse>), 500)]
        public async Task<IActionResult> GenerateRandomBytes([FromBody] GenerateRandomBytesRequest request)
        {
            try
            {
                _logger.LogInformation("Generating {Length} random bytes", request.Length);

                if (request.Length <= 0)
                {
                    _logger.LogWarning("Invalid length: {Length}", request.Length);
                    return BadRequest(ApiResponse<RandomBytesResponse>.CreateError(ApiErrorCodes.ValidationError, "Length must be greater than 0."));
                }

                var randomBytes = await _randomnessService.GenerateRandomBytesAsync(request.Length, request.Seed);

                // Generate a proof for the random bytes
                var proof = await GenerateProofAsync(randomBytes, request.Seed);

                var response = new RandomBytesResponse
                {
                    RandomBytes = Convert.ToBase64String(randomBytes),
                    Proof = proof,
                    Seed = request.Seed,
                    Timestamp = DateTime.UtcNow
                };

                _logger.LogInformation("Generated {Length} random bytes successfully", request.Length);

                return Ok(ApiResponse<RandomBytesResponse>.CreateSuccess(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating random bytes");
                return StatusCode(500, ApiResponse<RandomBytesResponse>.CreateError(ApiErrorCodes.InternalError, "An error occurred while generating random bytes."));
            }
        }

        private async Task<string> GenerateProofAsync<T>(T data, string seed)
        {
            try
            {
                // Create a hash of the data and seed using SHA-256
                using var sha256 = System.Security.Cryptography.SHA256.Create();

                // Serialize the data to bytes
                var dataJson = System.Text.Json.JsonSerializer.Serialize(data);
                var dataBytes = System.Text.Encoding.UTF8.GetBytes(dataJson);

                // Combine with seed
                var seedBytes = string.IsNullOrEmpty(seed)
                    ? Array.Empty<byte>()
                    : System.Text.Encoding.UTF8.GetBytes(seed);

                var combinedBytes = new byte[dataBytes.Length + seedBytes.Length];
                Buffer.BlockCopy(dataBytes, 0, combinedBytes, 0, dataBytes.Length);
                Buffer.BlockCopy(seedBytes, 0, combinedBytes, dataBytes.Length, seedBytes.Length);

                // Get the hash
                var hashBytes = await Task.Run(() => sha256.ComputeHash(combinedBytes));

                // Get a signature from the TEE
                var signature = await _randomnessService.SignDataAsync(hashBytes);

                // Combine hash and signature
                var proofData = new
                {
                    Hash = Convert.ToBase64String(hashBytes),
                    Signature = Convert.ToBase64String(signature),
                    Timestamp = DateTime.UtcNow,
                    Source = "Neo Service Layer TEE"
                };

                // Serialize the proof
                return System.Text.Json.JsonSerializer.Serialize(proofData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating randomness proof");
                throw;
            }
        }
    }

    /// <summary>
    /// Represents a request to generate random numbers.
    /// </summary>
    public class GenerateRandomNumbersRequest
    {
        /// <summary>
        /// Gets or sets the number of random numbers to generate.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Gets or sets the minimum value (inclusive).
        /// </summary>
        public int Min { get; set; }

        /// <summary>
        /// Gets or sets the maximum value (inclusive).
        /// </summary>
        public int Max { get; set; }

        /// <summary>
        /// Gets or sets the seed for deterministic randomness.
        /// </summary>
        public string Seed { get; set; }
    }

    /// <summary>
    /// Represents a response to a generate random numbers request.
    /// </summary>
    public class RandomNumbersResponse
    {
        /// <summary>
        /// Gets or sets the generated random numbers.
        /// </summary>
        public IEnumerable<int> RandomNumbers { get; set; }

        /// <summary>
        /// Gets or sets the proof that the random numbers were generated fairly.
        /// </summary>
        public string Proof { get; set; }

        /// <summary>
        /// Gets or sets the seed used to generate the random numbers.
        /// </summary>
        public string Seed { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the generation.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Represents a request to verify a randomness proof.
    /// </summary>
    public class VerifyRandomnessRequest
    {
        /// <summary>
        /// Gets or sets the random numbers to verify.
        /// </summary>
        public IEnumerable<int> RandomNumbers { get; set; }

        /// <summary>
        /// Gets or sets the proof to verify.
        /// </summary>
        public string Proof { get; set; }

        /// <summary>
        /// Gets or sets the seed used to generate the random numbers.
        /// </summary>
        public string Seed { get; set; }
    }

    /// <summary>
    /// Represents a response to a verify randomness request.
    /// </summary>
    public class VerifyRandomnessResponse
    {
        /// <summary>
        /// Gets or sets whether the proof is valid.
        /// </summary>
        public bool Valid { get; set; }
    }

    /// <summary>
    /// Represents a request to generate a random string.
    /// </summary>
    public class GenerateRandomStringRequest
    {
        /// <summary>
        /// Gets or sets the length of the string to generate.
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// Gets or sets the character set to use.
        /// </summary>
        public string Charset { get; set; }

        /// <summary>
        /// Gets or sets the seed for deterministic randomness.
        /// </summary>
        public string Seed { get; set; }
    }

    /// <summary>
    /// Represents a response to a generate random string request.
    /// </summary>
    public class RandomStringResponse
    {
        /// <summary>
        /// Gets or sets the generated random string.
        /// </summary>
        public string RandomString { get; set; }

        /// <summary>
        /// Gets or sets the proof that the random string was generated fairly.
        /// </summary>
        public string Proof { get; set; }

        /// <summary>
        /// Gets or sets the seed used to generate the random string.
        /// </summary>
        public string Seed { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the generation.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Represents a request to generate random bytes.
    /// </summary>
    public class GenerateRandomBytesRequest
    {
        /// <summary>
        /// Gets or sets the length of the bytes to generate.
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// Gets or sets the seed for deterministic randomness.
        /// </summary>
        public string Seed { get; set; }
    }

    /// <summary>
    /// Represents a response to a generate random bytes request.
    /// </summary>
    public class RandomBytesResponse
    {
        /// <summary>
        /// Gets or sets the generated random bytes (base64 encoded).
        /// </summary>
        public string RandomBytes { get; set; }

        /// <summary>
        /// Gets or sets the proof that the random bytes were generated fairly.
        /// </summary>
        public string Proof { get; set; }

        /// <summary>
        /// Gets or sets the seed used to generate the random bytes.
        /// </summary>
        public string Seed { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the generation.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
