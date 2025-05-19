using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Shared.Models;
using ITeeHostService = NeoServiceLayer.Core.Interfaces.ITeeHostService;

namespace NeoServiceLayer.Infrastructure.Services
{
    /// <summary>
    /// Implementation of the randomness service.
    /// </summary>
    public class RandomnessService : IRandomnessService
    {
        private readonly ITeeHostService _teeHostService;
        private readonly ILogger<RandomnessService> _logger;

        /// <summary>
        /// Initializes a new instance of the RandomnessService class.
        /// </summary>
        /// <param name="teeHostService">The TEE host service.</param>
        /// <param name="logger">The logger.</param>
        public RandomnessService(ITeeHostService teeHostService, ILogger<RandomnessService> logger)
        {
            _teeHostService = teeHostService ?? throw new ArgumentNullException(nameof(teeHostService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<int> GenerateRandomNumberAsync(int min, int max, string? seed = null)
        {
            _logger.LogInformation("Generating random number between {Min} and {Max}", min, max);

            if (min >= max)
            {
                throw new ArgumentException("Min must be less than Max", nameof(min));
            }

            try
            {
                // Create a message to send to the TEE
                var message = TeeMessage.Create(TeeMessageType.Randomness, JsonSerializer.Serialize(new
                {
                    Action = "generate_random_number",
                    Min = min,
                    Max = max,
                    Seed = seed
                }));

                // Send the message to the TEE
                var response = await _teeHostService.SendMessageAsync(message);

                // Parse the response
                var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(response.Data);
                var randomNumber = result["number"].GetInt32();

                _logger.LogInformation("Random number generated successfully");

                return randomNumber;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating random number");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<int>> GenerateRandomNumbersAsync(int count, int min, int max, string? seed = null)
        {
            _logger.LogInformation("Generating {Count} random numbers between {Min} and {Max}", count, min, max);

            if (count <= 0)
            {
                throw new ArgumentException("Count must be greater than 0", nameof(count));
            }

            if (min >= max)
            {
                throw new ArgumentException("Min must be less than Max", nameof(min));
            }

            try
            {
                // Create a message to send to the TEE
                var message = TeeMessage.Create(TeeMessageType.Randomness, JsonSerializer.Serialize(new
                {
                    Action = "generate_random_numbers",
                    Count = count,
                    Min = min,
                    Max = max,
                    Seed = seed
                }));

                // Send the message to the TEE
                var response = await _teeHostService.SendMessageAsync(message);

                // Parse the response
                var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(response.Data);
                var randomNumbers = JsonSerializer.Deserialize<int[]>(result["numbers"].GetRawText());

                _logger.LogInformation("Generated {Count} random numbers successfully", count);

                return randomNumbers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating random numbers");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<byte[]> GenerateRandomBytesAsync(int length, string? seed = null)
        {
            _logger.LogInformation("Generating {Length} random bytes", length);

            if (length <= 0)
            {
                throw new ArgumentException("Length must be greater than 0", nameof(length));
            }

            try
            {
                // Create a message to send to the TEE
                var message = TeeMessage.Create(TeeMessageType.Randomness, JsonSerializer.Serialize(new
                {
                    Action = "generate_random_bytes",
                    Length = length,
                    Seed = seed
                }));

                // Send the message to the TEE
                var response = await _teeHostService.SendMessageAsync(message);

                // Parse the response
                var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(response.Data);
                var randomBytes = Convert.FromBase64String(result["bytes"].GetString());

                _logger.LogInformation("Generated {Length} random bytes successfully", length);

                return randomBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating random bytes");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> VerifyRandomnessProofAsync(IEnumerable<int> randomNumbers, string proof, string? seed = null)
        {
            _logger.LogInformation("Verifying randomness proof");

            if (randomNumbers == null || !randomNumbers.Any())
            {
                throw new ArgumentException("Random numbers are required", nameof(randomNumbers));
            }

            if (string.IsNullOrEmpty(proof))
            {
                throw new ArgumentException("Proof is required", nameof(proof));
            }

            try
            {
                // Create a message to send to the TEE
                var message = TeeMessage.Create(TeeMessageType.Randomness, JsonSerializer.Serialize(new
                {
                    Action = "verify_randomness_proof",
                    RandomNumbers = randomNumbers.ToArray(),
                    Proof = proof,
                    Seed = seed
                }));

                // Send the message to the TEE
                var response = await _teeHostService.SendMessageAsync(message);

                // Parse the response
                var jsonResult = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(response.Data);
                var isValid = jsonResult["valid"].GetBoolean();

                _logger.LogInformation("Randomness proof verification completed with result: {IsValid}", isValid);

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying randomness proof");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string> GenerateRandomStringAsync(int length, string charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789", string? seed = null)
        {
            _logger.LogInformation("Generating random string of length {Length}", length);

            if (length <= 0)
            {
                throw new ArgumentException("Length must be greater than 0", nameof(length));
            }

            if (string.IsNullOrEmpty(charset))
            {
                throw new ArgumentException("Charset is required", nameof(charset));
            }

            try
            {
                // Create a message to send to the TEE
                var message = TeeMessage.Create(TeeMessageType.Randomness, JsonSerializer.Serialize(new
                {
                    Action = "generate_random_string",
                    Length = length,
                    Charset = charset,
                    Seed = seed
                }));

                // Send the message to the TEE
                var response = await _teeHostService.SendMessageAsync(message);

                // Parse the response
                var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(response.Data);
                var randomString = result["randomString"].GetString();

                _logger.LogInformation("Generated random string successfully");

                return randomString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating random string");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<byte[]> SignDataAsync(byte[] data)
        {
            _logger.LogInformation("Signing data of length {Length}", data.Length);

            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("Data is required", nameof(data));
            }

            try
            {
                // Create a message to send to the TEE
                var message = TeeMessage.Create(TeeMessageType.KeyManagement, JsonSerializer.Serialize(new
                {
                    Action = "sign",
                    Data = Convert.ToBase64String(data),
                    KeyId = "randomness_signing_key" // Use a dedicated key for randomness proofs
                }));

                // Send the message to the TEE
                var response = await _teeHostService.SendMessageAsync(message);

                // Parse the response
                var result = JsonSerializer.Deserialize<Dictionary<string, string>>(response.Data);
                var signature = Convert.FromBase64String(result["signature"]);

                _logger.LogInformation("Data signed successfully");

                return signature;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error signing data");
                throw;
            }
        }
    }
}
