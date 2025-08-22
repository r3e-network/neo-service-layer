using Microsoft.Extensions.Logging;
using NeoServiceLayer.Services.Core.SGX;
using NeoServiceLayer.Tee.Host.Services;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.SocialRecovery
{
    /// <summary>
    /// Enclave operations for the Social Recovery Service.
    /// </summary>
    public partial class SocialRecoveryService
    {
        private IEnclaveManager? _enclaveManager;

        /// <summary>
        /// Sets the enclave manager for SGX operations.
        /// </summary>
        /// <param name="enclaveManager">The enclave manager.</param>
        public void SetEnclaveManager(IEnclaveManager enclaveManager)
        {
            _enclaveManager = enclaveManager ?? throw new ArgumentNullException(nameof(enclaveManager));
        }

        /// <summary>
        /// Processes guardian approval using privacy-preserving computation in SGX.
        /// </summary>
        /// <param name="guardianAddress">The guardian's address.</param>
        /// <param name="recoveryRequest">The recovery request.</param>
        /// <returns>The privacy-preserving approval result.</returns>
        private async Task<PrivacyApprovalResult> ProcessGuardianApprovalAsync(
            string guardianAddress, RecoveryRequest recoveryRequest)
        {
            if (_enclaveManager == null)
            {
                throw new InvalidOperationException("Enclave manager not initialized");
            }

            // Prepare guardian proof for privacy-preserving approval
            var guardianProof = new
            {
                guardianId = guardianAddress,
                nonce = Guid.NewGuid().ToString(),
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                signature = GenerateGuardianSignature(guardianAddress, recoveryRequest.RecoveryId),
                weight = await GetGuardianWeightAsync(guardianAddress)
            };

            // Prepare recovery data
            var recoveryData = new
            {
                accountId = recoveryRequest.AccountAddress,
                threshold = recoveryRequest.RequiredConfirmations
            };

            var operation = "approval";

            var jsParams = new
            {
                operation,
                recoveryData,
                guardianProofs = new[] { guardianProof }
            };

            string paramsJson = JsonSerializer.Serialize(jsParams);

            // Execute privacy-preserving guardian approval in SGX
            string result = await _enclaveManager.ExecuteJavaScriptAsync(
                PrivacyComputingJavaScriptTemplates.SocialRecoveryOperations,
                paramsJson);

            if (string.IsNullOrEmpty(result))
                throw new InvalidOperationException("Enclave returned null or empty result");

            var resultJson = JsonSerializer.Deserialize<JsonElement>(result);

            if (!resultJson.TryGetProperty("success", out var success) || !success.GetBoolean())
            {
                throw new InvalidOperationException("Privacy-preserving approval failed in enclave");
            }

            // Extract privacy-preserving approval result
            var recoveryProof = resultJson.GetProperty("recoveryProof");

            return new PrivacyApprovalResult
            {
                RecoveryId = recoveryProof.GetProperty("recoveryId").GetString() ?? "",
                MeetsThreshold = recoveryProof.GetProperty("meetsThreshold").GetBoolean(),
                ApprovalCount = recoveryProof.GetProperty("approvalCount").GetInt32(),
                TotalWeight = recoveryProof.GetProperty("totalWeight").GetInt32(),
                RequiredThreshold = recoveryProof.GetProperty("requiredThreshold").GetInt32(),
                Proof = recoveryProof.GetProperty("proof").GetString() ?? "",
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(recoveryProof.GetProperty("timestamp").GetInt64()),
                Success = true
            };
        }

        /// <summary>
        /// Validates recovery request using privacy-preserving computation.
        /// </summary>
        /// <param name="recoveryRequest">The recovery request.</param>
        /// <param name="guardianApprovals">The guardian approvals.</param>
        /// <returns>True if the recovery is valid.</returns>
        private async Task<bool> ValidateRecoveryWithPrivacyAsync(
            RecoveryRequest recoveryRequest, List<string> guardianApprovals)
        {
            if (_enclaveManager == null)
            {
                throw new InvalidOperationException("Enclave manager not initialized");
            }

            // Prepare guardian proofs for all approvals
            var guardianProofs = new List<object>();
            foreach (var guardianAddress in guardianApprovals)
            {
                var proof = new
                {
                    guardianId = guardianAddress,
                    nonce = Guid.NewGuid().ToString(),
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    signature = GenerateGuardianSignature(guardianAddress, recoveryRequest.RecoveryId),
                    weight = await GetGuardianWeightAsync(guardianAddress)
                };
                guardianProofs.Add(proof);
            }

            var recoveryData = new
            {
                accountId = recoveryRequest.AccountAddress,
                threshold = recoveryRequest.RequiredConfirmations
            };

            var jsParams = new
            {
                operation = "validate",
                recoveryData,
                guardianProofs = guardianProofs.ToArray()
            };

            string paramsJson = JsonSerializer.Serialize(jsParams);

            string result = await _enclaveManager.ExecuteJavaScriptAsync(
                PrivacyComputingJavaScriptTemplates.SocialRecoveryOperations,
                paramsJson);

            if (string.IsNullOrEmpty(result))
                return false;

            try
            {
                var resultJson = JsonSerializer.Deserialize<JsonElement>(result);
                if (!resultJson.TryGetProperty("success", out var success) || !success.GetBoolean())
                    return false;

                var recoveryProof = resultJson.GetProperty("recoveryProof");
                return recoveryProof.GetProperty("meetsThreshold").GetBoolean();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Generates privacy-preserving recovery proof.
        /// </summary>
        /// <param name="recoveryRequest">The recovery request.</param>
        /// <param name="guardianApprovals">The guardian approvals.</param>
        /// <returns>The recovery proof.</returns>
        private async Task<RecoveryProof> GenerateRecoveryProofAsync(
            RecoveryRequest recoveryRequest, List<string> guardianApprovals)
        {
            if (_enclaveManager == null)
            {
                throw new InvalidOperationException("Enclave manager not initialized");
            }

            var guardianProofs = new List<object>();
            foreach (var guardianAddress in guardianApprovals)
            {
                var proof = new
                {
                    guardianId = guardianAddress,
                    nonce = Guid.NewGuid().ToString(),
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    signature = GenerateGuardianSignature(guardianAddress, recoveryRequest.RecoveryId),
                    weight = await GetGuardianWeightAsync(guardianAddress)
                };
                guardianProofs.Add(proof);
            }

            var recoveryData = new
            {
                accountId = recoveryRequest.AccountAddress,
                threshold = recoveryRequest.RequiredConfirmations
            };

            var jsParams = new
            {
                operation = "generate_proof",
                recoveryData,
                guardianProofs = guardianProofs.ToArray()
            };

            string paramsJson = JsonSerializer.Serialize(jsParams);

            string result = await _enclaveManager.ExecuteJavaScriptAsync(
                PrivacyComputingJavaScriptTemplates.SocialRecoveryOperations,
                paramsJson);

            if (string.IsNullOrEmpty(result))
                throw new InvalidOperationException("Failed to generate recovery proof");

            var resultJson = JsonSerializer.Deserialize<JsonElement>(result);
            var recoveryProof = resultJson.GetProperty("recoveryProof");

            return new RecoveryProof
            {
                RecoveryId = recoveryProof.GetProperty("recoveryId").GetString() ?? "",
                ProofHash = recoveryProof.GetProperty("proof").GetString() ?? "",
                ApprovalCount = recoveryProof.GetProperty("approvalCount").GetInt32(),
                TotalWeight = recoveryProof.GetProperty("totalWeight").GetInt32(),
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(recoveryProof.GetProperty("timestamp").GetInt64()),
                IsValid = recoveryProof.GetProperty("meetsThreshold").GetBoolean()
            };
        }

        /// <summary>
        /// Generates a guardian signature for approval.
        /// </summary>
        private string GenerateGuardianSignature(string guardianAddress, string recoveryId)
        {
            try
            {
                // Production cryptographic signature for guardian approval
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var messageData = $"{guardianAddress}|{recoveryId}|{timestamp}";
                
                // Use enclave sealing key for guardian signature
                var enclaveKey = await GetEnclaveSealiingKeyAsync();
                var messageBytes = System.Text.Encoding.UTF8.GetBytes(messageData);
                
                using var hmac = new System.Security.Cryptography.HMACSHA256(enclaveKey);
                var signature = hmac.ComputeHash(messageBytes);
                
                // Include timestamp in signature for verification
                var fullSignature = new byte[signature.Length + 8];
                Array.Copy(signature, 0, fullSignature, 0, signature.Length);
                Array.Copy(BitConverter.GetBytes(timestamp), 0, fullSignature, signature.Length, 8);
                
                return Convert.ToBase64String(fullSignature);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate guardian signature for {GuardianAddress}", guardianAddress);
                throw new InvalidOperationException("Guardian signature generation failed", ex);
            }
        }

        /// <summary>
        /// Gets guardian weight for approval calculation.
        /// </summary>
        private async Task<int> GetGuardianWeightAsync(string guardianAddress)
        {
            // In a real implementation, this would calculate weight based on:
            // - Guardian reputation
            // - Stake amount
            // - Historical performance
            await Task.CompletedTask;

            if (_guardians.TryGetValue(guardianAddress, out var guardian))
            {
                // Weight based on reputation (0-10000) normalized to 1-10
                var weight = 1 + (int)(guardian.ReputationScore * 9 / 10000);
                return Math.Max(1, Math.Min(10, weight));
            }

            return 1; // Default weight
        }

        /// <summary>
        /// Represents a privacy-preserving approval result.
        /// </summary>
        private class PrivacyApprovalResult
        {
            public string RecoveryId { get; set; } = "";
            public bool MeetsThreshold { get; set; }
            public int ApprovalCount { get; set; }
            public int TotalWeight { get; set; }
            public int RequiredThreshold { get; set; }
            public string Proof { get; set; } = "";
            public DateTimeOffset Timestamp { get; set; }
            public bool Success { get; set; }
        }

        /// <summary>
        /// Represents a recovery proof.
        /// </summary>
        private class RecoveryProof
        {
            public string RecoveryId { get; set; } = "";
            public string ProofHash { get; set; } = "";
            public int ApprovalCount { get; set; }
            public int TotalWeight { get; set; }
            public DateTimeOffset Timestamp { get; set; }
            public bool IsValid { get; set; }
        }
    }
}
