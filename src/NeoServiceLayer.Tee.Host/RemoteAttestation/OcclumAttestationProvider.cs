using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Host.Exceptions;
using NeoServiceLayer.Tee.Host.Occlum;

namespace NeoServiceLayer.Tee.Host.RemoteAttestation
{
    /// <summary>
    /// Provides attestation services for Occlum enclaves.
    /// </summary>
    public class OcclumAttestationProvider : IAttestationProvider
    {
        private readonly ILogger<OcclumAttestationProvider> _logger;
        private readonly IOcclumManager _occlumManager;

        /// <summary>
        /// Initializes a new instance of the OcclumAttestationProvider class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="occlumManager">The Occlum manager.</param>
        public OcclumAttestationProvider(ILogger<OcclumAttestationProvider> logger, IOcclumManager occlumManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _occlumManager = occlumManager ?? throw new ArgumentNullException(nameof(occlumManager));
        }

        /// <summary>
        /// Gets an attestation report for the enclave.
        /// </summary>
        /// <param name="reportData">Optional data to include in the report.</param>
        /// <returns>The attestation report.</returns>
        public byte[] GetAttestationReport(byte[] reportData)
        {
            try
            {
                _logger.LogInformation("Getting attestation report");

                // Create a temporary file for the report data
                string tempDir = Path.Combine(Path.GetTempPath(), "occlum_attestation");
                Directory.CreateDirectory(tempDir);
                string reportDataPath = Path.Combine(tempDir, $"report_data_{Guid.NewGuid():N}.bin");
                string reportPath = Path.Combine(tempDir, $"report_{Guid.NewGuid():N}.bin");

                try
                {
                    // Write the report data to a file
                    if (reportData != null && reportData.Length > 0)
                    {
                        File.WriteAllBytes(reportDataPath, reportData);
                    }
                    else
                    {
                        // Create an empty file
                        File.WriteAllBytes(reportDataPath, new byte[0]);
                    }

                    // Execute the attestation command
                    string[] args = new string[] { reportDataPath, reportPath };
                    int exitCode = _occlumManager.ExecuteCommandAsync("/bin/generate_attestation", args).Result;

                    if (exitCode != 0)
                    {
                        throw new EnclaveOperationException($"Attestation command failed with exit code {exitCode}");
                    }

                    // Read the report
                    byte[] report = File.ReadAllBytes(reportPath);
                    return report;
                }
                finally
                {
                    // Clean up the temporary files
                    try
                    {
                        if (File.Exists(reportDataPath))
                        {
                            File.Delete(reportDataPath);
                        }

                        if (File.Exists(reportPath))
                        {
                            File.Delete(reportPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temporary files");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attestation report");
                throw new EnclaveOperationException("Error getting attestation report", ex);
            }
        }

        /// <summary>
        /// Verifies an attestation report.
        /// </summary>
        /// <param name="report">The attestation report to verify.</param>
        /// <returns>True if the report is valid, false otherwise.</returns>
        public bool VerifyAttestationReport(byte[] report)
        {
            try
            {
                _logger.LogInformation("Verifying attestation report");

                // Create a temporary file for the report
                string tempDir = Path.Combine(Path.GetTempPath(), "occlum_attestation");
                Directory.CreateDirectory(tempDir);
                string reportPath = Path.Combine(tempDir, $"report_{Guid.NewGuid():N}.bin");

                try
                {
                    // Write the report to a file
                    File.WriteAllBytes(reportPath, report);

                    // Execute the verification command
                    string[] args = new string[] { reportPath };
                    int exitCode = _occlumManager.ExecuteCommandAsync("/bin/verify_attestation", args).Result;

                    return exitCode == 0;
                }
                finally
                {
                    // Clean up the temporary file
                    try
                    {
                        if (File.Exists(reportPath))
                        {
                            File.Delete(reportPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temporary file");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying attestation report");
                return false;
            }
        }

        /// <summary>
        /// Gets the enclave measurements.
        /// </summary>
        /// <returns>The enclave measurements.</returns>
        public EnclaveMeasurements GetEnclaveMeasurements()
        {
            try
            {
                _logger.LogInformation("Getting enclave measurements");

                // Create a temporary file for the measurements
                string tempDir = Path.Combine(Path.GetTempPath(), "occlum_attestation");
                Directory.CreateDirectory(tempDir);
                string measurementsPath = Path.Combine(tempDir, $"measurements_{Guid.NewGuid():N}.json");

                try
                {
                    // Execute the measurements command
                    string[] args = new string[] { measurementsPath };
                    int exitCode = _occlumManager.ExecuteCommandAsync("/bin/get_measurements", args).Result;

                    if (exitCode != 0)
                    {
                        throw new EnclaveOperationException($"Measurements command failed with exit code {exitCode}");
                    }

                    // Read the measurements
                    string measurementsJson = File.ReadAllText(measurementsPath);
                    var measurements = System.Text.Json.JsonSerializer.Deserialize<EnclaveMeasurements>(measurementsJson);
                    return measurements;
                }
                finally
                {
                    // Clean up the temporary file
                    try
                    {
                        if (File.Exists(measurementsPath))
                        {
                            File.Delete(measurementsPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temporary file");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enclave measurements");
                return new EnclaveMeasurements
                {
                    MrEnclave = new byte[32],
                    MrSigner = new byte[32],
                    ProductId = 1,
                    SecurityVersion = 1,
                    Attributes = 0
                };
            }
        }

        /// <summary>
        /// Seals data using the enclave's sealing key.
        /// </summary>
        /// <param name="data">The data to seal.</param>
        /// <returns>The sealed data.</returns>
        public byte[] SealData(byte[] data)
        {
            try
            {
                _logger.LogInformation("Sealing data");

                // Create temporary files for the data and sealed data
                string tempDir = Path.Combine(Path.GetTempPath(), "occlum_attestation");
                Directory.CreateDirectory(tempDir);
                string dataPath = Path.Combine(tempDir, $"data_{Guid.NewGuid():N}.bin");
                string sealedDataPath = Path.Combine(tempDir, $"sealed_data_{Guid.NewGuid():N}.bin");

                try
                {
                    // Write the data to a file
                    File.WriteAllBytes(dataPath, data);

                    // Execute the sealing command
                    string[] args = new string[] { dataPath, sealedDataPath };
                    int exitCode = _occlumManager.ExecuteCommandAsync("/bin/seal_data", args).Result;

                    if (exitCode != 0)
                    {
                        throw new EnclaveOperationException($"Sealing command failed with exit code {exitCode}");
                    }

                    // Read the sealed data
                    byte[] sealedData = File.ReadAllBytes(sealedDataPath);
                    return sealedData;
                }
                finally
                {
                    // Clean up the temporary files
                    try
                    {
                        if (File.Exists(dataPath))
                        {
                            File.Delete(dataPath);
                        }

                        if (File.Exists(sealedDataPath))
                        {
                            File.Delete(sealedDataPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temporary files");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sealing data");
                throw new EnclaveOperationException("Error sealing data", ex);
            }
        }

        /// <summary>
        /// Unseals data using the enclave's sealing key.
        /// </summary>
        /// <param name="sealedData">The sealed data to unseal.</param>
        /// <returns>The unsealed data.</returns>
        public byte[] UnsealData(byte[] sealedData)
        {
            try
            {
                _logger.LogInformation("Unsealing data");

                // Create temporary files for the sealed data and unsealed data
                string tempDir = Path.Combine(Path.GetTempPath(), "occlum_attestation");
                Directory.CreateDirectory(tempDir);
                string sealedDataPath = Path.Combine(tempDir, $"sealed_data_{Guid.NewGuid():N}.bin");
                string unsealedDataPath = Path.Combine(tempDir, $"unsealed_data_{Guid.NewGuid():N}.bin");

                try
                {
                    // Write the sealed data to a file
                    File.WriteAllBytes(sealedDataPath, sealedData);

                    // Execute the unsealing command
                    string[] args = new string[] { sealedDataPath, unsealedDataPath };
                    int exitCode = _occlumManager.ExecuteCommandAsync("/bin/unseal_data", args).Result;

                    if (exitCode != 0)
                    {
                        throw new EnclaveOperationException($"Unsealing command failed with exit code {exitCode}");
                    }

                    // Read the unsealed data
                    byte[] unsealedData = File.ReadAllBytes(unsealedDataPath);
                    return unsealedData;
                }
                finally
                {
                    // Clean up the temporary files
                    try
                    {
                        if (File.Exists(sealedDataPath))
                        {
                            File.Delete(sealedDataPath);
                        }

                        if (File.Exists(unsealedDataPath))
                        {
                            File.Delete(unsealedDataPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temporary files");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsealing data");
                throw new EnclaveOperationException("Error unsealing data", ex);
            }
        }
    }
}
