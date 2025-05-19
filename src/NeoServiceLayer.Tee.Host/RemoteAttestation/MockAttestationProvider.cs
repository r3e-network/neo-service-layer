using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Shared.Models.Attestation;

namespace NeoServiceLayer.Tee.Host.RemoteAttestation
{
    /// <summary>
    /// A mock attestation provider for testing.
    /// </summary>
    public class MockAttestationProvider : IAttestationProvider
    {
        private readonly ILogger<MockAttestationProvider> _logger;
        private readonly byte[] _mockMrEnclave;
        private readonly byte[] _mockMrSigner;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockAttestationProvider"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for logging information and errors.</param>
        public MockAttestationProvider(ILogger<MockAttestationProvider> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Generate mock measurements
            _mockMrEnclave = new byte[32];
            _mockMrSigner = new byte[32];

            // Fill with recognizable pattern
            for (int i = 0; i < 32; i++)
            {
                _mockMrEnclave[i] = (byte)(i + 1);
                _mockMrSigner[i] = (byte)(32 - i);
            }

            _logger.LogInformation("Mock attestation provider initialized");
        }

        /// <summary>
        /// Generates a mock attestation proof.
        /// </summary>
        /// <param name="reportData">Optional data to include in the report.</param>
        /// <returns>A mock attestation proof.</returns>
        public Task<AttestationProof> GenerateAttestationProofAsync(byte[] reportData = null)
        {
            _logger.LogInformation("Generating mock attestation proof");

            try
            {
                // Create a mock report
                var reportBuilder = new StringBuilder();
                reportBuilder.AppendLine("{");
                reportBuilder.AppendLine("  \"type\": \"SGX\",");
                reportBuilder.AppendLine("  \"mrenclave\": \"" + Convert.ToBase64String(_mockMrEnclave) + "\",");
                reportBuilder.AppendLine("  \"mrsigner\": \"" + Convert.ToBase64String(_mockMrSigner) + "\",");
                reportBuilder.AppendLine("  \"timestamp\": \"" + DateTime.UtcNow.ToString("o") + "\",");

                // Add report data if provided
                if (reportData != null && reportData.Length > 0)
                {
                    using (var sha256 = SHA256.Create())
                    {
                        byte[] hash = sha256.ComputeHash(reportData);
                        reportBuilder.AppendLine("  \"reportDataHash\": \"" + Convert.ToBase64String(hash) + "\",");
                    }
                }

                reportBuilder.AppendLine("  \"isvprodid\": 1,");
                reportBuilder.AppendLine("  \"isvsvn\": 1,");
                reportBuilder.AppendLine("  \"attributes\": \"0000000000000000\"");
                reportBuilder.AppendLine("}");

                string reportJson = reportBuilder.ToString();
                byte[] reportBytes = Encoding.UTF8.GetBytes(reportJson);

                // Create the attestation proof
                var proof = new AttestationProof
                {
                    EnclaveType = "OpenEnclave",
                    Report = Convert.ToBase64String(reportBytes),
                    Timestamp = DateTime.UtcNow,
                    AdditionalData = new Dictionary<string, string>
                    {
                        { "ReportSize", reportBytes.Length.ToString() },
                        { "ReportFormat", "SGX" },
                        { "IsMock", "true" }
                    }
                };

                // Add a hash of the report data if provided
                if (reportData != null && reportData.Length > 0)
                {
                    using (var sha256 = SHA256.Create())
                    {
                        byte[] hash = sha256.ComputeHash(reportData);
                        proof.AdditionalData.Add("ReportDataHash", Convert.ToBase64String(hash));
                    }
                }

                return Task.FromResult(proof);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating mock attestation proof");
                throw;
            }
        }

        /// <summary>
        /// Verifies a mock attestation proof.
        /// </summary>
        /// <param name="attestationProof">The attestation proof to verify.</param>
        /// <returns>True if the attestation proof is valid, false otherwise.</returns>
        public Task<bool> VerifyAttestationProofAsync(AttestationProof attestationProof)
        {
            _logger.LogInformation("Verifying mock attestation proof");

            if (attestationProof == null)
            {
                throw new ArgumentNullException(nameof(attestationProof));
            }

            try
            {
                // Check if this is a mock proof
                if (attestationProof.AdditionalData.TryGetValue("IsMock", out string isMock) && isMock == "true")
                {
                    // Always verify mock proofs as valid
                    return Task.FromResult(true);
                }

                // For non-mock proofs, try to parse the report
                byte[] reportBytes = Convert.FromBase64String(attestationProof.Report);
                string reportJson = Encoding.UTF8.GetString(reportBytes);

                // Check if the report contains the expected fields
                return Task.FromResult(
                    reportJson.Contains("\"type\": \"SGX\"") &&
                    reportJson.Contains("\"mrenclave\"") &&
                    reportJson.Contains("\"mrsigner\"") &&
                    reportJson.Contains("\"timestamp\""));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying mock attestation proof");
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Disposes the mock attestation provider.
        /// </summary>
        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
