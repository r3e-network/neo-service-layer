using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Host.Exceptions;

namespace NeoServiceLayer.Tee.Host.RemoteAttestation
{
    /// <summary>
    /// Provides attestation services for Occlum enclaves.
    /// </summary>
    public class OcclumAttestation
    {
        private readonly ILogger _logger;
        private readonly IAttestationProvider _attestationProvider;
        private readonly RSA _signingKey;

        /// <summary>
        /// Initializes a new instance of the OcclumAttestation class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="attestationProvider">The attestation provider.</param>
        public OcclumAttestation(ILogger logger, IAttestationProvider attestationProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _attestationProvider = attestationProvider ?? throw new ArgumentNullException(nameof(attestationProvider));
            
            // Create a signing key for the enclave (in a real implementation, this would be derived from the enclave identity)
            _signingKey = RSA.Create(2048);
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
                return _attestationProvider.GetAttestationReport(reportData);
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
                return _attestationProvider.VerifyAttestationReport(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying attestation report");
                return false;
            }
        }

        /// <summary>
        /// Verifies an attestation report with expected MRENCLAVE and MRSIGNER values.
        /// </summary>
        /// <param name="report">The attestation report to verify.</param>
        /// <param name="expectedMrEnclave">The expected MRENCLAVE value.</param>
        /// <param name="expectedMrSigner">The expected MRSIGNER value.</param>
        /// <returns>True if the report is valid and matches the expected values, false otherwise.</returns>
        public bool VerifyAttestationReport(byte[] report, byte[] expectedMrEnclave, byte[] expectedMrSigner)
        {
            try
            {
                _logger.LogInformation("Verifying attestation report with expected measurements");
                
                // First, verify the report itself
                if (!VerifyAttestationReport(report))
                {
                    return false;
                }
                
                // Get the measurements from the report
                var measurements = _attestationProvider.GetMeasurementsFromReport(report);
                
                // Check MRENCLAVE if provided
                if (expectedMrEnclave != null && expectedMrEnclave.Length > 0)
                {
                    if (!ByteArraysEqual(measurements.MrEnclave, expectedMrEnclave))
                    {
                        _logger.LogWarning("MRENCLAVE does not match expected value");
                        return false;
                    }
                }
                
                // Check MRSIGNER if provided
                if (expectedMrSigner != null && expectedMrSigner.Length > 0)
                {
                    if (!ByteArraysEqual(measurements.MrSigner, expectedMrSigner))
                    {
                        _logger.LogWarning("MRSIGNER does not match expected value");
                        return false;
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying attestation report with expected measurements");
                return false;
            }
        }

        /// <summary>
        /// Signs data using the enclave's private key.
        /// </summary>
        /// <param name="data">The data to sign.</param>
        /// <returns>The signature.</returns>
        public byte[] Sign(byte[] data)
        {
            try
            {
                _logger.LogInformation("Signing data");
                return _signingKey.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error signing data");
                throw new EnclaveOperationException("Error signing data", ex);
            }
        }

        /// <summary>
        /// Verifies a signature using the enclave's public key.
        /// </summary>
        /// <param name="data">The data to verify.</param>
        /// <param name="signature">The signature to verify.</param>
        /// <returns>True if the signature is valid, false otherwise.</returns>
        public bool Verify(byte[] data, byte[] signature)
        {
            try
            {
                _logger.LogInformation("Verifying signature");
                return _signingKey.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying signature");
                return false;
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
                return _attestationProvider.SealData(data);
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
                return _attestationProvider.UnsealData(sealedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsealing data");
                throw new EnclaveOperationException("Error unsealing data", ex);
            }
        }
        
        /// <summary>
        /// Compares two byte arrays for equality.
        /// </summary>
        /// <param name="a">The first byte array.</param>
        /// <param name="b">The second byte array.</param>
        /// <returns>True if the arrays are equal, false otherwise.</returns>
        private static bool ByteArraysEqual(byte[] a, byte[] b)
        {
            if (a == null || b == null)
            {
                return a == b;
            }
            
            if (a.Length != b.Length)
            {
                return false;
            }
            
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            
            return true;
        }
    }
}
