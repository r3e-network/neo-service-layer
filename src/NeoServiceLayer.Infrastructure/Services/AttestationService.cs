using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Infrastructure.Data.Repositories;
using ITeeHostService = NeoServiceLayer.Core.Interfaces.ITeeHostService;

namespace NeoServiceLayer.Infrastructure.Services
{
    /// <summary>
    /// Implementation of the attestation service.
    /// </summary>
    public class AttestationService : IAttestationService
    {
        private readonly ITeeHostService _teeHostService;
        private readonly IAttestationProofRepository _attestationProofRepository;
        private readonly ILogger<AttestationService> _logger;

        /// <summary>
        /// Initializes a new instance of the AttestationService class.
        /// </summary>
        /// <param name="teeHostService">The TEE host service.</param>
        /// <param name="attestationProofRepository">The attestation proof repository.</param>
        /// <param name="logger">The logger.</param>
        public AttestationService(
            ITeeHostService teeHostService,
            IAttestationProofRepository attestationProofRepository,
            ILogger<AttestationService> logger)
        {
            _teeHostService = teeHostService ?? throw new ArgumentNullException(nameof(teeHostService));
            _attestationProofRepository = attestationProofRepository ?? throw new ArgumentNullException(nameof(attestationProofRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<AttestationProof> GenerateAttestationProofAsync()
        {
            _logger.LogInformation("Generating attestation proof");

            try
            {
                // Get attestation proof from TEE
                var attestationProof = await _teeHostService.GetAttestationProofAsync();

                // Store attestation proof in the database
                await _attestationProofRepository.AddAsync(attestationProof);

                _logger.LogInformation("Attestation proof generated successfully");

                return attestationProof;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating attestation proof");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> VerifyAttestationProofAsync(AttestationProof attestationProof)
        {
            _logger.LogInformation("Verifying attestation proof");

            if (attestationProof == null)
            {
                throw new ArgumentNullException(nameof(attestationProof));
            }

            try
            {
                // Verify attestation proof with TEE
                var isValid = await _teeHostService.VerifyAttestationProofAsync(attestationProof);

                _logger.LogInformation("Attestation proof verification completed with result: {IsValid}", isValid);

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying attestation proof");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<AttestationProof> GetCurrentAttestationProofAsync()
        {
            _logger.LogInformation("Getting current attestation proof");

            try
            {
                // Get the current attestation proof from the database
                var currentProof = await _attestationProofRepository.GetCurrentAsync();

                // If there is no current attestation proof, generate a new one
                if (currentProof == null)
                {
                    _logger.LogInformation("Current attestation proof is null");

                    // Generate a new attestation proof from the TEE
                    _logger.LogInformation("No current attestation proof found, generating a new one");
                    return await GenerateAttestationProofAsync();
                }

                // If the attestation proof has expired, generate a new one
                if (currentProof.ExpiresAt <= DateTime.UtcNow)
                {
                    _logger.LogInformation("Current attestation proof is expired, generating a new one");
                    return await GenerateAttestationProofAsync();
                }

                _logger.LogInformation("Current attestation proof retrieved successfully");

                return currentProof;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current attestation proof");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<AttestationProof> GetAttestationProofAsync(string attestationProofId)
        {
            _logger.LogInformation("Getting attestation proof {AttestationProofId}", attestationProofId);

            if (string.IsNullOrEmpty(attestationProofId))
            {
                throw new ArgumentException("Attestation proof ID is required", nameof(attestationProofId));
            }

            try
            {
                var attestationProof = await _attestationProofRepository.GetByIdAsync(attestationProofId);

                if (attestationProof != null)
                {
                    _logger.LogInformation("Attestation proof {AttestationProofId} retrieved successfully", attestationProofId);
                    return attestationProof;
                }

                _logger.LogWarning("Attestation proof {AttestationProofId} not found", attestationProofId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attestation proof {AttestationProofId}", attestationProofId);
                throw;
            }
        }
    }
}
