using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.Integration.Tests.Mocks
{
    public class MockRandomnessService : IRandomnessService
    {
        private readonly ILogger<MockRandomnessService> _logger;
        private readonly Random _random;
        private readonly List<RandomnessRequest> _requests = new List<RandomnessRequest>();

        public MockRandomnessService(ILogger<MockRandomnessService> logger)
        {
            _logger = logger;
            _random = new Random();
        }

        public async Task<int> GenerateRandomNumberAsync(int min, int max, string? seed = null)
        {
            _logger.LogInformation("Generating random number between {Min} and {Max} with seed {Seed}", min, max, seed);

            if (min > max)
            {
                throw new ArgumentException("Min must be less than or equal to max", nameof(min));
            }

            var random = GetRandomFromSeed(seed);
            return random.Next(min, max + 1);
        }

        public async Task<IEnumerable<int>> GenerateRandomNumbersAsync(int count, int min, int max, string? seed = null)
        {
            _logger.LogInformation("Generating {Count} random numbers between {Min} and {Max} with seed {Seed}", count, min, max, seed);

            if (count <= 0)
            {
                throw new ArgumentException("Count must be greater than 0", nameof(count));
            }

            if (min > max)
            {
                throw new ArgumentException("Min must be less than or equal to max", nameof(min));
            }

            var numbers = new List<int>(count);
            var random = GetRandomFromSeed(seed);

            for (int i = 0; i < count; i++)
            {
                numbers.Add(random.Next(min, max + 1));
            }

            return numbers.AsEnumerable();
        }

        public async Task<string> GenerateRandomStringAsync(int length, string charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789", string? seed = null)
        {
            _logger.LogInformation("Generating random string of length {Length} with seed {Seed}", length, seed);

            if (length <= 0)
            {
                throw new ArgumentException("Length must be greater than 0", nameof(length));
            }

            var random = GetRandomFromSeed(seed);
            var result = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                result.Append(charset[random.Next(charset.Length)]);
            }

            return result.ToString();
        }

        public async Task<byte[]> GenerateRandomBytesAsync(int length, string? seed = null)
        {
            _logger.LogInformation("Generating {Length} random bytes with seed {Seed}", length, seed);

            if (length <= 0)
            {
                throw new ArgumentException("Length must be greater than 0", nameof(length));
            }

            var bytes = new byte[length];
            var random = GetRandomFromSeed(seed);
            random.NextBytes(bytes);

            return bytes;
        }

        public async Task<bool> VerifyRandomnessProofAsync(IEnumerable<int> randomNumbers, string proof, string? seed = null)
        {
            _logger.LogInformation("Verifying randomness proof");

            // For mock purposes, we'll always return true
            return true;
        }

        public async Task<byte[]> SignDataAsync(byte[] data)
        {
            _logger.LogInformation("Signing data");

            // Use HMAC-SHA256 for deterministic signatures
            using var hmac = new HMACSHA256(Guid.NewGuid().ToByteArray());
            return hmac.ComputeHash(data);
        }

        private Random GetRandomFromSeed(string? seed)
        {
            if (string.IsNullOrEmpty(seed))
            {
                return _random;
            }

            // Create a deterministic random number generator based on the seed
            using var sha256 = SHA256.Create();
            var seedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(seed));
            var seedInt = BitConverter.ToInt32(seedBytes, 0);
            return new Random(seedInt);
        }

        // The following methods are not part of the IRandomnessService interface
        // but are kept for backward compatibility with existing tests

        public async Task<RandomnessRequest> RequestRandomnessAsync(string userId, int numBytes, string? purpose = null)
        {
            _logger.LogInformation("Requesting randomness for user {UserId} with {NumBytes} bytes for purpose {Purpose}", userId, numBytes, purpose);

            var request = new RandomnessRequest
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                NumBytes = numBytes,
                Purpose = purpose ?? string.Empty,
                Status = RandomnessStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _requests.Add(request);

            // Simulate async processing
            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                await System.Threading.Tasks.Task.Delay(1000); // Simulate processing time

                var randomBytes = new byte[numBytes];
                _random.NextBytes(randomBytes);

                request.Status = RandomnessStatus.Completed;
                request.CompletedAt = DateTime.UtcNow;
                request.RandomnessHex = BitConverter.ToString(randomBytes).Replace("-", "");
                request.Seed = Guid.NewGuid().ToString();
                request.Proof = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            });

            return request;
        }

        public async Task<RandomnessRequest> GetRandomnessRequestAsync(string requestId)
        {
            _logger.LogInformation("Getting randomness request {RequestId}", requestId);

            var request = _requests.FirstOrDefault(r => r.Id == requestId);
            return request!;
        }

        public async Task<IEnumerable<RandomnessRequest>> GetRandomnessRequestsAsync(string userId)
        {
            _logger.LogInformation("Getting randomness requests for user {UserId}", userId);

            var requests = _requests.Where(r => r.UserId == userId).ToList();
            return requests.AsEnumerable();
        }

        public async Task<bool> VerifyRandomnessAsync(string requestId, string randomnessHex, string proof)
        {
            _logger.LogInformation("Verifying randomness for request {RequestId}", requestId);

            var request = _requests.FirstOrDefault(r => r.Id == requestId);
            if (request != null && request.Status == RandomnessStatus.Completed)
            {
                // Verify the proof against the randomness
                // 1. Verify the signature on the proof
                bool signatureValid = await _keyService.VerifySignatureAsync(
                    randomnessRequest.PublicKey,
                    Encoding.UTF8.GetBytes(randomnessRequest.Randomness),
                    Convert.FromBase64String(proof));

                if (!signatureValid)
                {
                    return false;
                }

                // 2. Verify the randomness matches the request
                return request.RandomnessHex == randomnessHex;
            }

            return false;
        }
    }
}
