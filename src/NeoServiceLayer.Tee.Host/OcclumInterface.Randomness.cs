using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Host.Exceptions;
using NeoServiceLayer.Tee.Host.Models;

namespace NeoServiceLayer.Tee.Host
{
    /// <summary>
    /// Randomness generation functionality for the OcclumInterface.
    /// </summary>
    public partial class OcclumInterface
    {
        /// <inheritdoc/>
        public async Task<ulong> GenerateRandomNumberAsync(ulong min, ulong max, string userId, string requestId)
        {
            CheckDisposed();
            
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }
            
            if (string.IsNullOrEmpty(requestId))
            {
                throw new ArgumentException("Request ID cannot be null or empty", nameof(requestId));
            }
            
            if (min >= max)
            {
                throw new ArgumentException("Minimum value must be less than maximum value", nameof(min));
            }
            
            _logger.LogInformation("Generating random number between {Min} and {Max} for user {UserId}, request {RequestId}", 
                min, max, userId, requestId);
            
            try
            {
                // Generate a random seed from enclave identity and request-specific data
                string seedData = $"{_measurements.MrEnclave}:{_measurements.MrSigner}:{userId}:{requestId}:{DateTimeOffset.UtcNow.Ticks}";
                byte[] seedBytes = Encoding.UTF8.GetBytes(seedData);
                
                // Get an enclave-based random value
                byte[] randomBytes = GetRandomBytes(32);
                
                // Combine the two sources of randomness
                byte[] combinedBytes = new byte[seedBytes.Length + randomBytes.Length];
                Buffer.BlockCopy(seedBytes, 0, combinedBytes, 0, seedBytes.Length);
                Buffer.BlockCopy(randomBytes, 0, combinedBytes, seedBytes.Length, randomBytes.Length);
                
                // Create a SHA-256 hash of the combined bytes
                byte[] hashBytes;
                using (var sha = System.Security.Cryptography.SHA256.Create())
                {
                    hashBytes = sha.ComputeHash(combinedBytes);
                }
                
                // Convert the hash to a ulong
                ulong randomValue = BitConverter.ToUInt64(hashBytes, 0);
                
                // Scale to the requested range
                ulong range = max - min;
                ulong scaledValue = (randomValue % range) + min;
                
                // Store the random number generation for auditing and verification
                await StoreRandomNumberGenerationAsync(scaledValue, min, max, userId, requestId);
                
                return scaledValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating random number for user {UserId}, request {RequestId}", userId, requestId);
                throw new EnclaveOperationException("Failed to generate random number", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> VerifyRandomNumberAsync(ulong randomNumber, ulong min, ulong max, string userId, string requestId, string proof)
        {
            CheckDisposed();
            
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }
            
            if (string.IsNullOrEmpty(requestId))
            {
                throw new ArgumentException("Request ID cannot be null or empty", nameof(requestId));
            }
            
            _logger.LogInformation("Verifying random number {RandomNumber} for user {UserId}, request {RequestId}", 
                randomNumber, userId, requestId);
            
            try
            {
                // Retrieve the stored random number generation
                byte[] storedData = await RetrievePersistentDataAsync($"random:{userId}:{requestId}");
                
                if (storedData == null || storedData.Length == 0)
                {
                    _logger.LogWarning("No stored random number generation found for user {UserId}, request {RequestId}", 
                        userId, requestId);
                    return false;
                }
                
                string storedJson = Encoding.UTF8.GetString(storedData);
                var stored = System.Text.Json.JsonSerializer.Deserialize<RandomNumberGeneration>(storedJson);
                
                if (stored == null)
                {
                    _logger.LogWarning("Failed to deserialize stored random number generation for user {UserId}, request {RequestId}", 
                        userId, requestId);
                    return false;
                }
                
                // Verify that the provided random number matches the stored one
                if (stored.RandomNumber != randomNumber)
                {
                    _logger.LogWarning("Provided random number {ProvidedNumber} does not match stored random number {StoredNumber} for user {UserId}, request {RequestId}", 
                        randomNumber, stored.RandomNumber, userId, requestId);
                    return false;
                }
                
                // Verify that the provided range matches the stored range
                if (stored.Min != min || stored.Max != max)
                {
                    _logger.LogWarning("Provided range [{ProvidedMin}, {ProvidedMax}] does not match stored range [{StoredMin}, {StoredMax}] for user {UserId}, request {RequestId}", 
                        min, max, stored.Min, stored.Max, userId, requestId);
                    return false;
                }
                
                // Verify the proof
                if (stored.Proof != proof)
                {
                    _logger.LogWarning("Provided proof does not match stored proof for user {UserId}, request {RequestId}", 
                        userId, requestId);
                    return false;
                }
                
                _logger.LogInformation("Random number {RandomNumber} verified successfully for user {UserId}, request {RequestId}", 
                    randomNumber, userId, requestId);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying random number for user {UserId}, request {RequestId}", userId, requestId);
                throw new EnclaveOperationException("Failed to verify random number", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<string> GetRandomNumberProofAsync(ulong randomNumber, ulong min, ulong max, string userId, string requestId)
        {
            CheckDisposed();
            
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }
            
            if (string.IsNullOrEmpty(requestId))
            {
                throw new ArgumentException("Request ID cannot be null or empty", nameof(requestId));
            }
            
            _logger.LogInformation("Getting proof for random number {RandomNumber} for user {UserId}, request {RequestId}", 
                randomNumber, userId, requestId);
            
            try
            {
                // Retrieve the stored random number generation
                byte[] storedData = await RetrievePersistentDataAsync($"random:{userId}:{requestId}");
                
                if (storedData == null || storedData.Length == 0)
                {
                    _logger.LogWarning("No stored random number generation found for user {UserId}, request {RequestId}", 
                        userId, requestId);
                    throw new EnclaveOperationException("No stored random number generation found");
                }
                
                string storedJson = Encoding.UTF8.GetString(storedData);
                var stored = System.Text.Json.JsonSerializer.Deserialize<RandomNumberGeneration>(storedJson);
                
                if (stored == null)
                {
                    _logger.LogWarning("Failed to deserialize stored random number generation for user {UserId}, request {RequestId}", 
                        userId, requestId);
                    throw new EnclaveOperationException("Failed to deserialize stored random number generation");
                }
                
                // Verify that the provided random number matches the stored one
                if (stored.RandomNumber != randomNumber)
                {
                    _logger.LogWarning("Provided random number {ProvidedNumber} does not match stored random number {StoredNumber} for user {UserId}, request {RequestId}", 
                        randomNumber, stored.RandomNumber, userId, requestId);
                    throw new EnclaveOperationException("Provided random number does not match stored random number");
                }
                
                // Verify that the provided range matches the stored range
                if (stored.Min != min || stored.Max != max)
                {
                    _logger.LogWarning("Provided range [{ProvidedMin}, {ProvidedMax}] does not match stored range [{StoredMin}, {StoredMax}] for user {UserId}, request {RequestId}", 
                        min, max, stored.Min, stored.Max, userId, requestId);
                    throw new EnclaveOperationException("Provided range does not match stored range");
                }
                
                // Return the proof
                return stored.Proof;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting proof for random number for user {UserId}, request {RequestId}", userId, requestId);
                throw new EnclaveOperationException("Failed to get proof for random number", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<string> GenerateSeedAsync(string userId, string requestId)
        {
            CheckDisposed();
            
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }
            
            if (string.IsNullOrEmpty(requestId))
            {
                throw new ArgumentException("Request ID cannot be null or empty", nameof(requestId));
            }
            
            _logger.LogInformation("Generating seed for user {UserId}, request {RequestId}", userId, requestId);
            
            try
            {
                // Generate a random seed based on enclave identity and user/request data
                string seedData = $"{_measurements.MrEnclave}:{_measurements.MrSigner}:{userId}:{requestId}:{DateTimeOffset.UtcNow.Ticks}";
                byte[] seedBytes = Encoding.UTF8.GetBytes(seedData);
                
                // Get an enclave-based random value
                byte[] randomBytes = GetRandomBytes(32);
                
                // Combine the two sources of randomness
                byte[] combinedBytes = new byte[seedBytes.Length + randomBytes.Length];
                Buffer.BlockCopy(seedBytes, 0, combinedBytes, 0, seedBytes.Length);
                Buffer.BlockCopy(randomBytes, 0, combinedBytes, seedBytes.Length, randomBytes.Length);
                
                // Create a SHA-256 hash of the combined bytes
                byte[] hashBytes;
                using (var sha = System.Security.Cryptography.SHA256.Create())
                {
                    hashBytes = sha.ComputeHash(combinedBytes);
                }
                
                // Convert the hash to a hex string
                string seed = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                
                // Store the seed for auditing and verification
                await StoreSeedGenerationAsync(seed, userId, requestId);
                
                return seed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating seed for user {UserId}, request {RequestId}", userId, requestId);
                throw new EnclaveOperationException("Failed to generate seed", ex);
            }
        }

        /// <summary>
        /// Stores information about a random number generation for later verification.
        /// </summary>
        /// <param name="randomNumber">The generated random number.</param>
        /// <param name="min">The minimum value of the range.</param>
        /// <param name="max">The maximum value of the range.</param>
        /// <param name="userId">The ID of the user who requested the random number.</param>
        /// <param name="requestId">The ID of the request.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task StoreRandomNumberGenerationAsync(ulong randomNumber, ulong min, ulong max, string userId, string requestId)
        {
            // Generate a proof for the random number
            string proofData = $"{_measurements.MrEnclave}:{_measurements.MrSigner}:{randomNumber}:{min}:{max}:{userId}:{requestId}:{DateTimeOffset.UtcNow.Ticks}";
            byte[] proofBytes = Encoding.UTF8.GetBytes(proofData);
            
            // Sign the proof
            byte[] signatureBytes = SignData(proofBytes);
            
            // Convert the signature to a hex string
            string proof = Convert.ToBase64String(signatureBytes);
            
            // Create a record of the random number generation
            var randomNumberGeneration = new RandomNumberGeneration
            {
                RandomNumber = randomNumber,
                Min = min,
                Max = max,
                UserId = userId,
                RequestId = requestId,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Proof = proof
            };
            
            // Serialize the record
            string json = System.Text.Json.JsonSerializer.Serialize(randomNumberGeneration);
            
            // Store the record
            await StorePersistentDataAsync($"random:{userId}:{requestId}", Encoding.UTF8.GetBytes(json));
        }

        /// <summary>
        /// Stores information about a seed generation for later verification.
        /// </summary>
        /// <param name="seed">The generated seed.</param>
        /// <param name="userId">The ID of the user who requested the seed.</param>
        /// <param name="requestId">The ID of the request.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task StoreSeedGenerationAsync(string seed, string userId, string requestId)
        {
            // Create a record of the seed generation
            var seedGeneration = new SeedGeneration
            {
                Seed = seed,
                UserId = userId,
                RequestId = requestId,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            
            // Serialize the record
            string json = System.Text.Json.JsonSerializer.Serialize(seedGeneration);
            
            // Store the record
            await StorePersistentDataAsync($"seed:{userId}:{requestId}", Encoding.UTF8.GetBytes(json));
        }
    }
} 