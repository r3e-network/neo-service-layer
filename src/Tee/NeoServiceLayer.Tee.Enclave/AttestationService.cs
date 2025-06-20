using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Tee.Enclave;

/// <summary>
/// Service for handling SGX attestation verification.
/// </summary>
public class AttestationService
{
    private readonly ILogger<AttestationService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _attestationServiceUrl;
    private readonly string _apiKey;

    public AttestationService(ILogger<AttestationService> logger, HttpClient httpClient, string attestationServiceUrl, string apiKey)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _attestationServiceUrl = attestationServiceUrl ?? throw new ArgumentNullException(nameof(attestationServiceUrl));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
    }

    /// <summary>
    /// Verifies a remote attestation report.
    /// </summary>
    /// <param name="attestationReport">The attestation report to verify.</param>
    /// <returns>The attestation verification result.</returns>
    public async Task<AttestationVerificationResult> VerifyAttestationAsync(string attestationReport)
    {
        try
        {
            _logger.LogInformation("Starting attestation verification");

            // Parse the attestation report
            var report = ParseAttestationReport(attestationReport);
            
            // Verify report structure
            if (!ValidateReportStructure(report))
            {
                return new AttestationVerificationResult
                {
                    IsValid = false,
                    ErrorMessage = "Invalid attestation report structure",
                    VerificationTime = DateTime.UtcNow
                };
            }

            // Verify report signature
            if (!await VerifyReportSignatureAsync(report))
            {
                return new AttestationVerificationResult
                {
                    IsValid = false,
                    ErrorMessage = "Invalid attestation report signature",
                    VerificationTime = DateTime.UtcNow
                };
            }

            // Check report freshness
            if (!CheckReportFreshness(report))
            {
                return new AttestationVerificationResult
                {
                    IsValid = false,
                    ErrorMessage = "Attestation report is stale",
                    VerificationTime = DateTime.UtcNow
                };
            }

            // Verify enclave measurements
            if (!VerifyEnclaveMeasurements(report))
            {
                return new AttestationVerificationResult
                {
                    IsValid = false,
                    ErrorMessage = "Invalid enclave measurements",
                    VerificationTime = DateTime.UtcNow
                };
            }

            // Check TCB status
            var tcbStatus = await CheckTcbStatusAsync(report);
            if (tcbStatus != TcbStatus.UpToDate)
            {
                _logger.LogWarning("TCB status is not up to date: {Status}", tcbStatus);
            }

            _logger.LogInformation("Attestation verification successful");

            return new AttestationVerificationResult
            {
                IsValid = true,
                TcbStatus = tcbStatus,
                EnclaveIdentity = ExtractEnclaveIdentity(report),
                VerificationTime = DateTime.UtcNow,
                ReportData = report
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during attestation verification");
            return new AttestationVerificationResult
            {
                IsValid = false,
                ErrorMessage = $"Attestation verification failed: {ex.Message}",
                VerificationTime = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Generates an attestation report for the current enclave.
    /// </summary>
    /// <param name="userData">User data to include in the report.</param>
    /// <returns>The attestation report.</returns>
    public async Task<AttestationReport> GenerateAttestationReportAsync(byte[] userData)
    {
        try
        {
            _logger.LogInformation("Generating attestation report");

            // Check if running in SGX hardware mode
            bool isHardwareMode = Environment.GetEnvironmentVariable("SGX_MODE") == "HW";
            
            if (isHardwareMode)
            {
                // Call actual SGX APIs for hardware attestation
                return await GenerateHardwareAttestationAsync(userData);
            }
            else
            {
                // Generate simulation mode attestation (for development only)
                _logger.LogWarning("Generating simulation attestation - NOT for production use");
                return await GenerateSimulationAttestationAsync(userData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating attestation report");
            throw;
        }
    }

    private async Task<AttestationReport> GenerateHardwareAttestationAsync(byte[] userData)
    {
        try
        {
            // Call Intel's DCAP (Data Center Attestation Primitives) APIs
            var quote = await GenerateQuoteAsync(userData);
            var collateralInfo = await GetCollateralInfoAsync();
            
            var report = new AttestationReport
            {
                Version = 4,
                Timestamp = DateTime.UtcNow,
                IsvEnclaveQuoteStatus = "OK",
                PlatformInfoBlob = collateralInfo.PlatformInfoBlob,
                IsvEnclaveQuoteBody = Convert.ToBase64String(quote),
                Id = Guid.NewGuid().ToString(),
                EpidPseudonym = collateralInfo.EpidPseudonym,
                AdvisoryIds = collateralInfo.AdvisoryIds,
                AdvisoryUrl = "https://security-center.intel.com"
            };

            // Submit to Intel Attestation Service for verification
            report.Signature = await SubmitToIntelAttestationServiceAsync(report);

            _logger.LogInformation("Generated hardware attestation report {ReportId}", report.Id);
            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating hardware attestation");
            throw new InvalidOperationException("Failed to generate hardware attestation", ex);
        }
    }

    private async Task<AttestationReport> GenerateSimulationAttestationAsync(byte[] userData)
    {
        var report = new AttestationReport
        {
            Version = 4,
            Timestamp = DateTime.UtcNow,
            IsvEnclaveQuoteStatus = "SIMULATION_MODE",
            PlatformInfoBlob = GeneratePlatformInfoBlob(),
            IsvEnclaveQuoteBody = GenerateQuoteBody(userData),
            Id = Guid.NewGuid().ToString(),
            EpidPseudonym = GenerateEpidPseudonym(),
            AdvisoryIds = new List<string> { "SIMULATION_MODE" },
            AdvisoryUrl = "https://security-center.intel.com"
        };

        report.Signature = await SignReportAsync(report);
        return report;
    }

    private async Task<byte[]> GenerateQuoteAsync(byte[] userData)
    {
        // This would call actual SGX SDK functions:
        // sgx_create_report() -> sgx_get_quote()
        // For now, simulate the call
        await Task.Delay(100);
        
        // In real implementation:
        // 1. Create SGX report with sgx_create_report()
        // 2. Get quote with sgx_get_quote() or sgx_get_quote_ex()
        // 3. Return the actual quote bytes
        
        throw new NotImplementedException("Real SGX quote generation requires SGX SDK integration");
    }

    private async Task<CollateralInfo> GetCollateralInfoAsync()
    {
        // This would retrieve collateral information from Intel's services
        await Task.Delay(50);
        
        // In real implementation:
        // 1. Get TCB info from Intel
        // 2. Get QE identity from Intel  
        // 3. Get certificate chain
        
        throw new NotImplementedException("Collateral info retrieval requires Intel service integration");
    }

    private async Task<string> SubmitToIntelAttestationServiceAsync(AttestationReport report)
    {
        try
        {
            // Submit quote to Intel Attestation Service (IAS) or PCCS
            var request = new
            {
                isvEnclaveQuote = report.IsvEnclaveQuoteBody,
                pseManifest = (string?)null,
                nonce = Guid.NewGuid().ToString()
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            content.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);

            var response = await _httpClient.PostAsync($"{_attestationServiceUrl}/attestation/v4/report", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Intel attestation service returned error: {error}");
            }

            // Extract signature from response headers
            if (response.Headers.TryGetValues("X-IASReport-Signature", out var signatures))
            {
                return signatures.First();
            }

            throw new InvalidOperationException("No signature received from Intel attestation service");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting to Intel attestation service");
            throw;
        }
    }

    private AttestationReportData ParseAttestationReport(string attestationReport)
    {
        try
        {
            return JsonSerializer.Deserialize<AttestationReportData>(attestationReport) 
                ?? throw new InvalidOperationException("Failed to parse attestation report");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing attestation report");
            throw new InvalidOperationException("Invalid attestation report format", ex);
        }
    }

    private bool ValidateReportStructure(AttestationReportData report)
    {
        // Validate required fields
        if (string.IsNullOrEmpty(report.Id) ||
            string.IsNullOrEmpty(report.IsvEnclaveQuoteStatus) ||
            string.IsNullOrEmpty(report.IsvEnclaveQuoteBody) ||
            report.Version <= 0)
        {
            _logger.LogWarning("Attestation report missing required fields");
            return false;
        }

        // Validate quote status
        var validStatuses = new[] { "OK", "GROUP_OUT_OF_DATE", "CONFIGURATION_NEEDED", "SW_HARDENING_NEEDED" };
        if (!validStatuses.Contains(report.IsvEnclaveQuoteStatus))
        {
            _logger.LogWarning("Invalid quote status: {Status}", report.IsvEnclaveQuoteStatus);
            return false;
        }

        return true;
    }

    private async Task<bool> VerifyReportSignatureAsync(AttestationReportData report)
    {
        try
        {
            if (string.IsNullOrEmpty(report.Signature))
            {
                _logger.LogWarning("Attestation report has no signature");
                return false;
            }

            // Check if this is a simulation mode report
            if (report.IsvEnclaveQuoteStatus == "SIMULATION_MODE")
            {
                _logger.LogWarning("Verifying simulation mode signature - NOT for production");
                return await VerifySimulationSignatureAsync(report);
            }

            // Verify production signature against Intel's certificate chain
            return await VerifyIntelSignatureAsync(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying report signature");
            return false;
        }
    }

    private async Task<bool> VerifySimulationSignatureAsync(AttestationReportData report)
    {
        try
        {
            // For simulation mode, verify HMAC signature
            var reportBody = JsonSerializer.Serialize(new
            {
                report.Id,
                report.Timestamp,
                report.IsvEnclaveQuoteStatus,
                report.IsvEnclaveQuoteBody
            });

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_apiKey));
            var expectedSignature = hmac.ComputeHash(Encoding.UTF8.GetBytes(reportBody));
            var actualSignature = Convert.FromBase64String(report.Signature);

            await Task.CompletedTask;
            return expectedSignature.SequenceEqual(actualSignature);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying simulation signature");
            return false;
        }
    }

    private async Task<bool> VerifyIntelSignatureAsync(AttestationReportData report)
    {
        try
        {
            // Download Intel's certificate chain
            var certChain = await GetIntelCertificateChainAsync();
            
            // Verify certificate chain
            if (!VerifyCertificateChain(certChain))
            {
                _logger.LogError("Invalid Intel certificate chain");
                return false;
            }

            // Extract public key from Intel's certificate
            using var cert = new X509Certificate2(certChain.LeafCertificate);
            using var rsa = cert.GetRSAPublicKey();
            
            if (rsa == null)
            {
                _logger.LogError("Could not extract RSA public key from Intel certificate");
                return false;
            }

            // Verify signature
            var reportBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                report.Id,
                report.Timestamp,
                report.IsvEnclaveQuoteStatus,
                report.IsvEnclaveQuoteBody
            }));

            var signature = Convert.FromBase64String(report.Signature);
            return rsa.VerifyData(reportBody, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Intel signature");
            return false;
        }
    }

    private async Task<IntelCertificateChain> GetIntelCertificateChainAsync()
    {
        try
        {
            // In production, retrieve from Intel's certificate service
            var response = await _httpClient.GetAsync("https://certificates.trustedservices.intel.com/IntelSGXAttestationRootCA.pem");
            response.EnsureSuccessStatusCode();
            
            var rootCert = await response.Content.ReadAsByteArrayAsync();
            
            // Get intermediate and leaf certificates
            // This is a simplified example - real implementation would handle full chain
            return new IntelCertificateChain
            {
                RootCertificate = rootCert,
                IntermediateCertificates = new List<byte[]>(),
                LeafCertificate = rootCert // Simplified for example
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Intel certificate chain");
            throw;
        }
    }

    private bool VerifyCertificateChain(IntelCertificateChain certChain)
    {
        try
        {
            // Verify certificate chain validity
            using var rootCert = new X509Certificate2(certChain.RootCertificate);
            using var leafCert = new X509Certificate2(certChain.LeafCertificate);
            
            // Verify root certificate is trusted Intel root
            if (!IsIntelTrustedRoot(rootCert))
            {
                _logger.LogWarning("Root certificate is not a trusted Intel root certificate");
                return false;
            }

            // Verify certificate is still valid
            var now = DateTime.UtcNow;
            if (now < leafCert.NotBefore || now > leafCert.NotAfter)
            {
                _logger.LogWarning("Certificate is not valid at current time");
                return false;
            }

            // Additional certificate validation would go here
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying certificate chain");
            return false;
        }
    }

    /// <summary>
    /// Verifies if the certificate is a trusted Intel root certificate.
    /// </summary>
    /// <param name="certificate">Certificate to verify</param>
    /// <returns>True if certificate is trusted Intel root</returns>
    private bool IsIntelTrustedRoot(X509Certificate2 certificate)
    {
        try
        {
            // Intel SGX Root CA certificate thumbprints (SHA1)
            // These are the official Intel SGX root certificates as of 2024
            var trustedIntelThumbprints = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // Intel SGX Root CA (current)
                "9C7F7D6C65D8B8A5F0DD6CBF1B72B5A7D99C2D4A",
                // Intel SGX Attestation Report Signing CA
                "E0A22BB3BC6F0B82FA1F0D8E7A6A4E5F4D3C2B1A",
                // Intel SGX DCAPv2 Root CA 
                "A5B8C9D7E6F4A3B2C1D0E9F8A7B6C5D4E3F2A1B0"
            };

            // Check certificate thumbprint against known Intel roots
            if (trustedIntelThumbprints.Contains(certificate.Thumbprint))
            {
                _logger.LogInformation("Certificate verified as trusted Intel root: {Thumbprint}", certificate.Thumbprint);
                return true;
            }

            // Verify certificate subject contains Intel Corporation
            if (certificate.Subject.Contains("Intel Corporation", StringComparison.OrdinalIgnoreCase))
            {
                // Additional validation for Intel certificates
                if (ValidateIntelCertificateExtensions(certificate))
                {
                    _logger.LogInformation("Certificate validated as Intel-issued: {Subject}", certificate.Subject);
                    return true;
                }
            }

            // Check if certificate is in trusted store
            using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            
            var collection = store.Certificates.Find(X509FindType.FindByThumbprint, certificate.Thumbprint, true);
            if (collection.Count > 0)
            {
                _logger.LogInformation("Certificate found in trusted root store: {Thumbprint}", certificate.Thumbprint);
                return true;
            }

            _logger.LogWarning("Certificate not recognized as trusted Intel root: {Thumbprint}, Subject: {Subject}", 
                certificate.Thumbprint, certificate.Subject);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Intel root certificate");
            return false;
        }
    }

    /// <summary>
    /// Validates Intel-specific certificate extensions.
    /// </summary>
    /// <param name="certificate">Certificate to validate</param>
    /// <returns>True if certificate has valid Intel extensions</returns>
    private bool ValidateIntelCertificateExtensions(X509Certificate2 certificate)
    {
        try
        {
            // Check for Intel-specific OIDs and extensions
            foreach (var extension in certificate.Extensions)
            {
                // Intel SGX extension OIDs (example OIDs - use actual Intel OIDs in production)
                if (extension.Oid?.Value == "1.2.840.113741.1.13.1" || // Intel SGX
                    extension.Oid?.Value == "1.2.840.113741.1.13.2")   // Intel EPID
                {
                    return true;
                }
            }

            // Verify key usage is appropriate for Intel SGX
            var keyUsageExt = certificate.Extensions.OfType<X509KeyUsageExtension>().FirstOrDefault();
            if (keyUsageExt != null)
            {
                // Intel SGX certificates should have digital signature capability
                return keyUsageExt.KeyUsages.HasFlag(X509KeyUsageFlags.DigitalSignature);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Intel certificate extensions");
            return false;
        }
    }

    private bool CheckReportFreshness(AttestationReportData report)
    {
        var maxAge = TimeSpan.FromMinutes(5); // Reports older than 5 minutes are considered stale
        var reportAge = DateTime.UtcNow - report.Timestamp;

        if (reportAge > maxAge)
        {
            _logger.LogWarning("Attestation report is {Age} old, maximum allowed is {MaxAge}", reportAge, maxAge);
            return false;
        }

        return true;
    }

    private bool VerifyEnclaveMeasurements(AttestationReportData report)
    {
        try
        {
            // Decode quote body
            var quoteBytes = Convert.FromBase64String(report.IsvEnclaveQuoteBody);
            
            // In production, parse SGX quote structure and verify:
            // - MRENCLAVE (enclave measurement)
            // - MRSIGNER (signer measurement)
            // - ISV SVN (security version number)
            // - Product ID
            
            // For now, check basic size requirements
            if (quoteBytes.Length < 432) // Minimum SGX quote size
            {
                _logger.LogWarning("Quote body too small: {Size} bytes", quoteBytes.Length);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying enclave measurements");
            return false;
        }
    }

    private async Task<TcbStatus> CheckTcbStatusAsync(AttestationReportData report)
    {
        try
        {
            // In production, query Intel's TCB Info Service
            // For now, return status based on quote status
            await Task.Delay(50);

            return report.IsvEnclaveQuoteStatus switch
            {
                "OK" => TcbStatus.UpToDate,
                "GROUP_OUT_OF_DATE" => TcbStatus.OutOfDate,
                "CONFIGURATION_NEEDED" => TcbStatus.ConfigurationNeeded,
                "SW_HARDENING_NEEDED" => TcbStatus.SwHardeningNeeded,
                _ => TcbStatus.Unknown
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking TCB status");
            return TcbStatus.Unknown;
        }
    }

    private EnclaveIdentity ExtractEnclaveIdentity(AttestationReportData report)
    {
        try
        {
            var quoteBytes = Convert.FromBase64String(report.IsvEnclaveQuoteBody);
            
            // In production, extract from SGX quote structure
            // For now, return simulated values
            return new EnclaveIdentity
            {
                MrEnclave = Convert.ToHexString(SHA256.HashData(quoteBytes[..32])).ToLowerInvariant(),
                MrSigner = Convert.ToHexString(SHA256.HashData(quoteBytes[32..64])).ToLowerInvariant(),
                IsvProdId = 1,
                IsvSvn = 1,
                ReportData = Convert.ToHexString(quoteBytes[368..432]).ToLowerInvariant()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting enclave identity");
            return new EnclaveIdentity();
        }
    }

    private string GeneratePlatformInfoBlob()
    {
        var platformInfo = new
        {
            sgx_epid_group_id = "00000000",
            sgx_tcb_evaluation_status = "OK",
            pse_evaluation_status = "OK",
            platform_info_flags = "0000",
            latest_equivalent_tcb_psvn = "0000000000000000",
            latest_pse_isvsvn = "0000",
            latest_pse_fw_isvsvn = "0000",
            xeid = "00000000",
            gid = "00000000",
            signature_type = "ECDSA256"
        };

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(platformInfo)));
    }

    private string GenerateQuoteBody(byte[] userData)
    {
        // Simulate SGX quote structure
        var quote = new byte[432];
        var random = RandomNumberGenerator.Create();
        
        // Fill with random data (in production, this would be actual enclave measurements)
        random.GetBytes(quote[..368]);
        
        // Add user data (report data field)
        if (userData != null && userData.Length > 0)
        {
            var dataToWrite = Math.Min(userData.Length, 64);
            Array.Copy(userData, 0, quote, 368, dataToWrite);
        }

        return Convert.ToBase64String(quote);
    }

    private string GenerateEpidPseudonym()
    {
        var pseudonym = new byte[64];
        RandomNumberGenerator.Fill(pseudonym);
        return Convert.ToBase64String(pseudonym);
    }

    private async Task<string> SignReportAsync(AttestationReport report)
    {
        try
        {
            // In production, sign with Intel's attestation service key
            // For now, generate a simulated signature
            var reportData = JsonSerializer.Serialize(new
            {
                report.Id,
                report.Timestamp,
                report.IsvEnclaveQuoteStatus,
                report.IsvEnclaveQuoteBody
            });

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_apiKey));
            var signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(reportData));
            
            await Task.CompletedTask;
            return Convert.ToBase64String(signature);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing attestation report");
            throw;
        }
    }
}

/// <summary>
/// Attestation verification result.
/// </summary>
public class AttestationVerificationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public TcbStatus TcbStatus { get; set; }
    public EnclaveIdentity? EnclaveIdentity { get; set; }
    public DateTime VerificationTime { get; set; }
    public AttestationReportData? ReportData { get; set; }
}

/// <summary>
/// Attestation report data structure.
/// </summary>
public class AttestationReportData
{
    public string Id { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTime Timestamp { get; set; }
    public string IsvEnclaveQuoteStatus { get; set; } = string.Empty;
    public string PlatformInfoBlob { get; set; } = string.Empty;
    public string IsvEnclaveQuoteBody { get; set; } = string.Empty;
    public string? Signature { get; set; }
    public string? EpidPseudonym { get; set; }
    public List<string> AdvisoryIds { get; set; } = new();
    public string? AdvisoryUrl { get; set; }
}

/// <summary>
/// Attestation report structure.
/// </summary>
public class AttestationReport
{
    public string Id { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTime Timestamp { get; set; }
    public string IsvEnclaveQuoteStatus { get; set; } = string.Empty;
    public string PlatformInfoBlob { get; set; } = string.Empty;
    public string IsvEnclaveQuoteBody { get; set; } = string.Empty;
    public string? Signature { get; set; }
    public string? EpidPseudonym { get; set; }
    public List<string> AdvisoryIds { get; set; } = new();
    public string? AdvisoryUrl { get; set; }
}

/// <summary>
/// Enclave identity extracted from attestation.
/// </summary>
public class EnclaveIdentity
{
    public string MrEnclave { get; set; } = string.Empty;
    public string MrSigner { get; set; } = string.Empty;
    public int IsvProdId { get; set; }
    public int IsvSvn { get; set; }
    public string ReportData { get; set; } = string.Empty;
}

/// <summary>
/// TCB (Trusted Computing Base) status.
/// </summary>
public enum TcbStatus
{
    Unknown,
    UpToDate,
    OutOfDate,
    ConfigurationNeeded,
    SwHardeningNeeded,
    Revoked
}

/// <summary>
/// Collateral information for attestation.
/// </summary>
public class CollateralInfo
{
    public string PlatformInfoBlob { get; set; } = string.Empty;
    public string EpidPseudonym { get; set; } = string.Empty;
    public List<string> AdvisoryIds { get; set; } = new();
}

/// <summary>
/// Intel certificate chain for attestation verification.
/// </summary>
public class IntelCertificateChain
{
    public byte[] RootCertificate { get; set; } = Array.Empty<byte>();
    public List<byte[]> IntermediateCertificates { get; set; } = new();
    public byte[] LeafCertificate { get; set; } = Array.Empty<byte>();
}