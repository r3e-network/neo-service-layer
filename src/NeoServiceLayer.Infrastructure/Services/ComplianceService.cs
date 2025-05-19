using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Infrastructure.Data.Repositories;
using NeoServiceLayer.Shared.Models;
using ITeeHostService = NeoServiceLayer.Core.Interfaces.ITeeHostService;

namespace NeoServiceLayer.Infrastructure.Services
{
    /// <summary>
    /// Implementation of the compliance service.
    /// </summary>
    public class ComplianceService : IComplianceService
    {
        private readonly ITeeHostService _teeHostService;
        private readonly IVerificationResultRepository _verificationResultRepository;
        private readonly ILogger<ComplianceService> _logger;

        /// <summary>
        /// Initializes a new instance of the ComplianceService class.
        /// </summary>
        /// <param name="teeHostService">The TEE host service.</param>
        /// <param name="verificationResultRepository">The verification result repository.</param>
        /// <param name="logger">The logger.</param>
        public ComplianceService(
            ITeeHostService teeHostService,
            IVerificationResultRepository verificationResultRepository,
            ILogger<ComplianceService> logger)
        {
            _teeHostService = teeHostService ?? throw new ArgumentNullException(nameof(teeHostService));
            _verificationResultRepository = verificationResultRepository ?? throw new ArgumentNullException(nameof(verificationResultRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<string> VerifyIdentityAsync(string identityData, string verificationType)
        {
            _logger.LogInformation("Verifying identity with type {VerificationType}", verificationType);

            if (string.IsNullOrEmpty(identityData))
            {
                throw new ArgumentException("Identity data is required", nameof(identityData));
            }

            if (string.IsNullOrEmpty(verificationType))
            {
                throw new ArgumentException("Verification type is required", nameof(verificationType));
            }

            try
            {
                // Create a message to send to the TEE
                var message = TeeMessage.Create(TeeMessageType.Compliance, JsonSerializer.Serialize(new
                {
                    Action = "verify_identity",
                    IdentityData = identityData,
                    VerificationType = verificationType
                }));

                // Send the message to the TEE
                var response = await _teeHostService.SendMessageAsync(message);

                // Parse the response
                var result = JsonSerializer.Deserialize<Dictionary<string, string>>(response.Data);
                var verificationId = result["verification_id"];

                // Create a verification result
                var verificationResult = new VerificationResult
                {
                    VerificationId = verificationId,
                    Status = "pending",
                    CreatedAt = DateTime.UtcNow
                };

                // Store verification result in the database
                await _verificationResultRepository.AddVerificationResultAsync(verificationResult, verificationType, identityData);

                // Process verification asynchronously
                _ = ProcessVerificationAsync(verificationId, identityData, verificationType);

                _logger.LogInformation("Identity verification initiated with ID {VerificationId}", verificationId);

                return verificationId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying identity");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<VerificationResult> GetVerificationResultAsync(string verificationId)
        {
            _logger.LogInformation("Getting verification result for {VerificationId}", verificationId);

            if (string.IsNullOrEmpty(verificationId))
            {
                throw new ArgumentException("Verification ID is required", nameof(verificationId));
            }

            try
            {
                var verificationResult = await _verificationResultRepository.GetVerificationResultByIdAsync(verificationId);

                if (verificationResult != null)
                {
                    _logger.LogInformation("Verification result for {VerificationId} retrieved successfully", verificationId);
                    return verificationResult;
                }

                _logger.LogWarning("Verification {VerificationId} not found", verificationId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting verification result for {VerificationId}", verificationId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ComplianceCheckResult> CheckTransactionComplianceAsync(string transactionData)
        {
            _logger.LogInformation("Checking transaction compliance");

            if (string.IsNullOrEmpty(transactionData))
            {
                throw new ArgumentException("Transaction data is required", nameof(transactionData));
            }

            try
            {
                // Create a message to send to the TEE
                var message = TeeMessage.Create(TeeMessageType.Compliance, JsonSerializer.Serialize(new
                {
                    Action = "check_transaction_compliance",
                    TransactionData = transactionData
                }));

                // Send the message to the TEE
                var response = await _teeHostService.SendMessageAsync(message);

                // Parse the response
                var result = JsonSerializer.Deserialize<ComplianceCheckResult>(response.Data);

                _logger.LogInformation("Transaction compliance check completed with result: {IsCompliant}", result.Compliant);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking transaction compliance");
                throw;
            }
        }

        private async Task ProcessVerificationAsync(string verificationId, string identityData, string verificationType)
        {
            try
            {
                _logger.LogInformation("Processing verification {VerificationId}", verificationId);

                // Get the verification result from the database
                var verificationResult = await _verificationResultRepository.GetVerificationResultByIdAsync(verificationId);
                if (verificationResult == null)
                {
                    _logger.LogWarning("Verification {VerificationId} not found", verificationId);
                    return;
                }

                // Simulate verification processing
                await Task.Delay(2000);

                // Create a message to send to the TEE
                var message = TeeMessage.Create(TeeMessageType.Compliance, JsonSerializer.Serialize(new
                {
                    Action = "process_verification",
                    VerificationId = verificationId,
                    IdentityData = identityData,
                    VerificationType = verificationType
                }));

                // Send the message to the TEE
                var response = await _teeHostService.SendMessageAsync(message);

                // Parse the response
                var result = JsonSerializer.Deserialize<Dictionary<string, object>>(response.Data);

                // Update verification result
                verificationResult.Status = "completed";
                verificationResult.Verified = (bool)result["verified"];
                verificationResult.Score = Convert.ToDouble(result["score"]);
                verificationResult.Reason = (string)result["reason"];
                verificationResult.ProcessedAt = DateTime.UtcNow;

                // Update the verification result in the database
                await _verificationResultRepository.UpdateVerificationResultAsync(verificationResult);

                _logger.LogInformation("Verification {VerificationId} processed successfully", verificationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing verification {VerificationId}", verificationId);

                try
                {
                    // Get the verification result from the database
                    var verificationResult = await _verificationResultRepository.GetVerificationResultByIdAsync(verificationId);
                    if (verificationResult != null)
                    {
                        // Update verification result with error
                        verificationResult.Status = "failed";
                        verificationResult.Reason = ex.Message;
                        verificationResult.ProcessedAt = DateTime.UtcNow;

                        // Update the verification result in the database
                        await _verificationResultRepository.UpdateVerificationResultAsync(verificationResult);
                    }
                }
                catch (Exception innerEx)
                {
                    _logger.LogError(innerEx, "Error updating verification result for {VerificationId}", verificationId);
                }
            }
        }
    }
}
