using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.TestInfrastructure
{
    /// <summary>
    /// Mock SGX service for testing SGX-dependent functionality
    /// without requiring actual SGX hardware or drivers.
    /// </summary>
    public class MockSgxService
    {
        private readonly ILogger<MockSgxService> _logger;
        private readonly Dictionary<string, byte[]> _mockSecrets = new();
        private bool _isInitialized;

        public MockSgxService(ILogger<MockSgxService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool IsInitialized => _isInitialized;

        public Task<bool> InitializeAsync()
        {
            _logger.LogInformation("Initializing mock SGX service");
            _isInitialized = true;
            return Task.FromResult(true);
        }

        public Task<byte[]> SealDataAsync(string keyId, byte[] data)
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Mock SGX service not initialized");

            _logger.LogDebug("Sealing data with key: {KeyId}", keyId);
            
            // Mock sealing by storing the data with a prefix
            var sealedData = new byte[data.Length + 16];
            var prefix = System.Text.Encoding.UTF8.GetBytes("SEALED:");
            Array.Copy(prefix, 0, sealedData, 0, Math.Min(prefix.Length, 8));
            Array.Copy(data, 0, sealedData, 8, data.Length);
            
            _mockSecrets[keyId] = sealedData;
            return Task.FromResult(sealedData);
        }

        public Task<byte[]> UnsealDataAsync(string keyId, byte[] sealedData)
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Mock SGX service not initialized");

            _logger.LogDebug("Unsealing data with key: {KeyId}", keyId);
            
            // Mock unsealing by removing the prefix
            if (sealedData.Length < 8)
                throw new ArgumentException("Invalid sealed data format");

            var unsealedData = new byte[sealedData.Length - 8];
            Array.Copy(sealedData, 8, unsealedData, 0, unsealedData.Length);
            
            return Task.FromResult(unsealedData);
        }

        public Task<string> GenerateKeyAsync(string keyType, int keySize)
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Mock SGX service not initialized");

            _logger.LogDebug("Generating key of type: {KeyType}, size: {KeySize}", keyType, keySize);
            
            // Generate a mock key ID
            var keyId = $"mock-{keyType}-{keySize}-{Guid.NewGuid():N}";
            
            // Store mock key material
            var keyMaterial = new byte[keySize / 8];
            new Random().NextBytes(keyMaterial);
            _mockSecrets[keyId] = keyMaterial;
            
            return Task.FromResult(keyId);
        }

        public Task<bool> VerifyAttestationAsync(byte[] attestation)
        {
            if (!_isInitialized)
                return Task.FromResult(false);

            _logger.LogDebug("Verifying attestation");
            
            // Mock verification - always pass for testing
            return Task.FromResult(attestation?.Length > 0);
        }

        public Task<byte[]> CreateAttestationAsync()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Mock SGX service not initialized");

            _logger.LogDebug("Creating mock attestation");
            
            var attestation = new Dictionary<string, object>
            {
                ["type"] = "mock-sgx",
                ["version"] = "1.0.0",
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ["measurement"] = Convert.ToBase64String(new byte[32]) // Mock measurement
            };

            var json = System.Text.Json.JsonSerializer.Serialize(attestation);
            return Task.FromResult(System.Text.Encoding.UTF8.GetBytes(json));
        }

        public void Dispose()
        {
            if (_isInitialized)
            {
                _logger.LogInformation("Disposing mock SGX service");
                _mockSecrets.Clear();
                _isInitialized = false;
            }
        }
    }
}