using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;
using Task = System.Threading.Tasks.Task;

namespace NeoServiceLayer.Integration.Tests.Mocks
{
    public class MockAttestationService : IAttestationService
    {
        private readonly ILogger<MockAttestationService> _logger;

        public MockAttestationService(ILogger<MockAttestationService> logger)
        {
            _logger = logger;
        }

        public Task<AttestationProof> GenerateAttestationProofAsync()
        {
            _logger.LogInformation("Generating mock attestation proof");

            var attestationProof = new AttestationProof
            {
                Id = Guid.NewGuid().ToString(),
                MrEnclave = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
                MrSigner = "fedcba9876543210fedcba9876543210fedcba9876543210fedcba9876543210",
                ProductId = "12345",
                SecurityVersion = "1.0",
                Attributes = "attributes",
                Report = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                Signature = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                Metadata = new Dictionary<string, object>
                {
                    { "IsSimulationMode", true }
                }
            };

            return Task.FromResult(attestationProof);
        }

        public Task<AttestationProof> GetAttestationProofAsync(string id)
        {
            _logger.LogInformation("Getting mock attestation proof by ID: {Id}", id);

            var attestationProof = new AttestationProof
            {
                Id = id,
                MrEnclave = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
                MrSigner = "fedcba9876543210fedcba9876543210fedcba9876543210fedcba9876543210",
                ProductId = "12345",
                SecurityVersion = "1.0",
                Attributes = "attributes",
                Report = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                Signature = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                Metadata = new Dictionary<string, object>
                {
                    { "IsSimulationMode", true }
                }
            };

            return Task.FromResult(attestationProof);
        }

        public Task<AttestationProof> GetCurrentAttestationProofAsync()
        {
            _logger.LogInformation("Getting mock attestation proof");

            var attestationProof = new AttestationProof
            {
                Id = Guid.NewGuid().ToString(),
                MrEnclave = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
                MrSigner = "fedcba9876543210fedcba9876543210fedcba9876543210fedcba9876543210",
                ProductId = "12345",
                SecurityVersion = "1.0",
                Attributes = "attributes",
                Report = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                Signature = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                Metadata = new Dictionary<string, object>
                {
                    { "IsSimulationMode", true }
                }
            };

            return Task.FromResult(attestationProof);
        }

        public Task<bool> VerifyAttestationProofAsync(AttestationProof attestationProof)
        {
            _logger.LogInformation("Verifying mock attestation proof");
            return Task.FromResult(true);
        }

        public Task<AttestationVerificationResult> VerifyAttestationAsync(AttestationProof attestationProof)
        {
            _logger.LogInformation("Verifying mock attestation");

            var result = new AttestationVerificationResult
            {
                Valid = true,
                EnclaveIdentity = new EnclaveIdentity
                {
                    MrEnclave = attestationProof?.MrEnclave ?? "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
                    MrSigner = attestationProof?.MrSigner ?? "fedcba9876543210fedcba9876543210fedcba9876543210fedcba9876543210"
                },
                Timestamp = DateTime.UtcNow
            };

            return Task.FromResult(result);
        }

        public Task<AttestationProof> GetAttestationProofByIdAsync(string id)
        {
            _logger.LogInformation("Getting mock attestation proof by ID");

            var attestationProof = new AttestationProof
            {
                Id = id,
                MrEnclave = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
                MrSigner = "fedcba9876543210fedcba9876543210fedcba9876543210fedcba9876543210",
                ProductId = "12345",
                SecurityVersion = "1.0",
                Attributes = "attributes",
                Report = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                Signature = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                Metadata = new Dictionary<string, object>
                {
                    { "IsSimulationMode", true }
                }
            };

            return Task.FromResult(attestationProof);
        }

        public Task<List<AttestationProof>> GetAttestationProofsAsync(int limit = 10, int offset = 0)
        {
            _logger.LogInformation("Getting mock attestation proofs");

            var attestationProofs = new List<AttestationProof>();
            for (int i = 0; i < limit; i++)
            {
                attestationProofs.Add(new AttestationProof
                {
                    Id = Guid.NewGuid().ToString(),
                    MrEnclave = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
                    MrSigner = "fedcba9876543210fedcba9876543210fedcba9876543210fedcba9876543210",
                    ProductId = "12345",
                    SecurityVersion = "1.0",
                    Attributes = "attributes",
                    Report = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                    Signature = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                    CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    Metadata = new Dictionary<string, object>
                    {
                        { "IsSimulationMode", true }
                    }
                });
            }

            return Task.FromResult(attestationProofs);
        }
    }
}
