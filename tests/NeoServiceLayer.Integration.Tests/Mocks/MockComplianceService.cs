using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;
using VerificationResult = NeoServiceLayer.Shared.Models.VerificationResult;

namespace NeoServiceLayer.Integration.Tests.Mocks
{
    public class MockComplianceService : IComplianceService
    {
        private readonly ILogger<MockComplianceService> _logger;
        // Use a static dictionary to persist verification results across instances
        private static readonly Dictionary<string, NeoServiceLayer.Shared.Models.VerificationResult> _verificationResults = new Dictionary<string, NeoServiceLayer.Shared.Models.VerificationResult>();

        public MockComplianceService(ILogger<MockComplianceService> logger)
        {
            _logger = logger;
        }

        public async Task<string> VerifyIdentityAsync(string identityData, string verificationType)
        {
            _logger.LogInformation("Verifying identity with type {VerificationType}", verificationType);

            var verificationId = Guid.NewGuid().ToString();
            var verificationResult = new VerificationResult
            {
                VerificationId = verificationId,
                Status = "Pending",
                Reason = "Verification in progress",
                Metadata = new Dictionary<string, object>
                {
                    { "verificationType", verificationType },
                    { "identityData", identityData },
                    { "status", "pending" }
                },
                VerificationType = verificationType,
                IdentityData = identityData,
                CreatedAt = DateTime.UtcNow
            };

            _verificationResults[verificationId] = verificationResult;

            // Simulate async processing
            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                await System.Threading.Tasks.Task.Delay(2000); // Simulate processing time
                verificationResult.Status = "Completed";
                verificationResult.Verified = true;
                verificationResult.Score = 0.95;
                verificationResult.Reason = "Verification successful";
                verificationResult.Metadata["status"] = "completed";
                verificationResult.ProcessedAt = DateTime.UtcNow;
                verificationResult.Metadata["source"] = "mock";
            });

            return verificationId;
        }

        public async Task<NeoServiceLayer.Shared.Models.VerificationResult> GetVerificationResultAsync(string verificationId)
        {
            _logger.LogInformation("Getting verification result for {VerificationId}", verificationId);

            if (_verificationResults.TryGetValue(verificationId, out var result))
            {
                return result;
            }

            return null;
        }

        public async Task<ComplianceCheckResult> CheckTransactionComplianceAsync(string transactionData)
        {
            _logger.LogInformation("Checking transaction compliance");

            var result = new ComplianceCheckResult
            {
                Compliant = true,
                Reason = "Transaction complies with all regulations",
                RiskScore = 0.05
            };

            return result;
        }

        // The following methods are not part of the IComplianceService interface
        // but are kept for backward compatibility with existing tests

        public async Task<bool> VerifyTransactionAsync(string transactionData)
        {
            _logger.LogInformation("Verifying transaction data");

            // For mock purposes, we'll consider all transactions valid
            return true;
        }

        public async Task<bool> CheckComplianceAsync(string address, string transactionType)
        {
            _logger.LogInformation("Checking compliance for address {Address} and transaction type {TransactionType}", address, transactionType);

            // For mock purposes, we'll consider all addresses compliant
            return true;
        }

        public async Task<AttestationVerificationResult> VerifyAttestationAsync(AttestationProof attestationProof)
        {
            _logger.LogInformation("Verifying attestation proof {ProofId}", attestationProof.Id);

            var result = new AttestationVerificationResult
            {
                Valid = true,
                Reason = "Attestation proof verified successfully",
                EnclaveIdentity = new EnclaveIdentity
                {
                    MrEnclave = attestationProof.MrEnclave,
                    MrSigner = attestationProof.MrSigner,
                    ProductId = attestationProof.ProductId,
                    SecurityVersion = attestationProof.SecurityVersion
                },
                Timestamp = DateTime.UtcNow
            };

            return result;
        }
    }
}
