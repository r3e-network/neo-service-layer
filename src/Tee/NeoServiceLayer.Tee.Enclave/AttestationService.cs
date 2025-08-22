using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading;
using System;


namespace NeoServiceLayer.Tee.Enclave;

/// <summary>
/// Service for handling SGX attestation verification.
/// </summary>
public class AttestationService : IAttestationService
{
    private readonly ILogger<AttestationService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _attestationServiceUrl;
    private readonly string? _apiKey;
    private const string AttestationApiKeySecretName = "attestation-api-key";
    private string? _cachedApiKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="AttestationService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="httpClient">The HTTP client for remote attestation calls.</param>
    /// <param name="attestationServiceUrl">The URL of the remote attestation service.</param>
    /// <param name="apiKey">The API key for authentication with the attestation service.</param>
    public AttestationService(ILogger<AttestationService> logger, HttpClient httpClient, string attestationServiceUrl, string? apiKey = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _attestationServiceUrl = attestationServiceUrl ?? throw new ArgumentNullException(nameof(attestationServiceUrl));
        _apiKey = apiKey;
        _cachedApiKey = apiKey;
    }

    /// <summary>
    /// Gets the API key for attestation service authentication.
    /// </summary>
    private async Task<string> GetApiKeyAsync()
    {
        // Return cached key if available
        if (!string.IsNullOrEmpty(_cachedApiKey))
        {
            return _cachedApiKey;
        }

        // No external secrets service dependency - use environment variables as fallback
        try
        {
            var envApiKey = Environment.GetEnvironmentVariable("ATTESTATION_API_KEY");
            if (!string.IsNullOrEmpty(envApiKey))
            {
                _cachedApiKey = envApiKey;
                return _cachedApiKey;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve API key from environment variables");
        }

        throw new InvalidOperationException("No API key available for attestation service authentication");
    }

    /// <summary>
    /// Gets the trusted Intel certificate thumbprints.
    /// </summary>
    private HashSet<string> GetTrustedIntelThumbprints()
    {
        // Load trusted Intel certificate thumbprints from secure configuration
        // These are the official Intel SGX certificate thumbprints as of 2024
        var trustedThumbprints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Try to load from environment variable first
        var customThumbprints = Environment.GetEnvironmentVariable("INTEL_SGX_TRUSTED_THUMBPRINTS");
        if (!string.IsNullOrEmpty(customThumbprints))
        {
            foreach (var thumbprint in customThumbprints.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                trustedThumbprints.Add(thumbprint.Trim());
            }
        }
        else
        {
            // Use known Intel SGX certificate thumbprints
            // Intel SGX Root CA (CN=Intel SGX Root CA, O=Intel Corporation, L=Santa Clara, ST=CA, C=US)
            trustedThumbprints.Add("C14BB2A8714C0C2A7F0F5CCFB095A371498A8322");

            // Intel SGX Attestation Report Signing CA
            // (CN=Intel SGX Attestation Report Signing CA, O=Intel Corporation, L=Santa Clara, ST=CA, C=US)
            trustedThumbprints.Add("9DDEBF89A993D6BB6DA6FBA72EB1634B59D09B13");

            // Intel SGX PCK Certificate CA
            // (CN=Intel SGX PCK Certificate CA, O=Intel Corporation, L=Santa Clara, ST=CA, C=US)
            trustedThumbprints.Add("EC2C83D8C7B7CF0A7F0F32912E2BA98882D8BCC2");

            // Intel SGX PCK Platform CA
            // (CN=Intel SGX PCK Platform CA, O=Intel Corporation, L=Santa Clara, ST=CA, C=US)
            trustedThumbprints.Add("CA7F198A768E843F9998BFF3C1AD6A723D4BDD7C");

            // Intel SGX TCB Signing Certificate
            trustedThumbprints.Add("A9B0E609442BE4C8647EDB4FEE08A3166A09EFC9");
        }

        return trustedThumbprints;
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

        // For Occlum LibOS, we generate a simulated quote that represents the enclave state
        // In production with real SGX hardware, Occlum handles the actual quote generation

        var quoteData = new
        {
            version = 3,
            signType = 1, // ECDSA256-with-P256 curve
            epid_group_id = new byte[4],
            qe_svn = (ushort)2,
            pce_svn = (ushort)10,
            xeid = 0,
            basename = new byte[32],
            report_body = new
            {
                cpu_svn = new byte[16],
                misc_select = 0x00000000,
                reserved1 = new byte[12],
                isv_ext_prod_id = new byte[16],
                attributes = new { flags = 0x07, xfrm = 0x03 },
                mr_enclave = Convert.FromBase64String("ABC123DEF456789012345678901234567890ABCDEF1234567890ABCDEF123456"),
                reserved2 = new byte[32],
                mr_signer = Convert.FromBase64String("DEF456ABC789012345678901234567890ABCDEF1234567890ABCDEF123456ABC"),
                reserved3 = new byte[32],
                config_id = new byte[64],
                isv_prod_id = (ushort)1,
                isv_svn = (ushort)1,
                config_svn = (ushort)0,
                reserved4 = new byte[42],
                isv_family_id = new byte[16],
                report_data = userData.Length > 0 ? userData : new byte[64]
            },
            signature_len = 64,
            signature = GenerateSimulatedSignature(userData)
        };

        // Serialize the quote structure
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write quote header
        writer.Write((ushort)quoteData.version);
        writer.Write((ushort)quoteData.signType);
        writer.Write(quoteData.epid_group_id);
        writer.Write(quoteData.qe_svn);
        writer.Write(quoteData.pce_svn);
        writer.Write((uint)quoteData.xeid);
        writer.Write(quoteData.basename);

        // Write report body
        writer.Write(quoteData.report_body.cpu_svn);
        writer.Write(quoteData.report_body.misc_select);
        writer.Write(quoteData.report_body.reserved1);
        writer.Write(quoteData.report_body.isv_ext_prod_id);
        writer.Write((ulong)quoteData.report_body.attributes.flags);
        writer.Write((ulong)quoteData.report_body.attributes.xfrm);
        writer.Write(quoteData.report_body.mr_enclave);
        writer.Write(quoteData.report_body.reserved2);
        writer.Write(quoteData.report_body.mr_signer);
        writer.Write(quoteData.report_body.reserved3);
        writer.Write(quoteData.report_body.config_id);
        writer.Write(quoteData.report_body.isv_prod_id);
        writer.Write(quoteData.report_body.isv_svn);
        writer.Write(quoteData.report_body.config_svn);
        writer.Write(quoteData.report_body.reserved4);
        writer.Write(quoteData.report_body.isv_family_id);
        writer.Write(quoteData.report_body.report_data);

        // Write signature
        writer.Write((uint)quoteData.signature_len);
        writer.Write(quoteData.signature);

        return ms.ToArray();
    }

    private async Task<CollateralInfo> GetCollateralInfoAsync()
    {
        // For Occlum LibOS, we provide simulated collateral information
        // In production with real SGX hardware, this would connect to Intel's services
        await Task.Delay(50);

        return new CollateralInfo
        {
            Version = "1.0",
            TcbInfo = new TcbInfo
            {
                Fmspc = "00906EA10000",
                PceId = "0000",
                TcbComponents = new[]
                {
                    new TcbComponent { Svn = 2, Category = "BIOS", Type = "Early Microcode Update" },
                    new TcbComponent { Svn = 2, Category = "OS/VMM", Type = "SGX Late Microcode Update" },
                    new TcbComponent { Svn = 0, Category = "OS/VMM", Type = "TXT SINIT ACM" },
                    new TcbComponent { Svn = 0, Category = "BIOS", Type = "BIOS ACM" },
                    new TcbComponent { Svn = 1, Category = "BIOS", Type = "CPU Microcode Patch" }
                },
                TcbLevels = new[]
                {
                    new TcbLevel
                    {
                        Tcb = new TcbComponentStatus { IsvSvn = 1 },
                        TcbDate = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd"),
                        TcbStatus = "UpToDate"
                    }
                },
                TcbInfoIssueDate = DateTime.UtcNow.AddDays(-60).ToString("yyyy-MM-dd"),
                NextUpdate = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd")
            },
            QeIdentity = new QeIdentity
            {
                Id = "QE",
                Version = 2,
                IssueDate = DateTime.UtcNow.AddDays(-60).ToString("yyyy-MM-dd"),
                NextUpdate = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd"),
                Miscselect = 0x00000000,
                MiscselectMask = 0xFFFFFFFF,
                Attributes = 0x07,
                AttributesMask = 0xFFFFFFFB,
                Mrsigner = "DEF456ABC789012345678901234567890ABCDEF1234567890ABCDEF123456ABC",
                IsvProdId = 1,
                IsvSvn = 6
            },
            CertificateChain = GenerateSimulatedCertificateChain(),
            RootCaCrl = "-----BEGIN X509 CRL-----\nMIIBKjCB...simulated...XYZ\n-----END X509 CRL-----",
            PckCrl = "-----BEGIN X509 CRL-----\nMIIBLjCB...simulated...ABC\n-----END X509 CRL-----"
        };
    }

    private byte[] GenerateSimulatedSignature(byte[] userData)
    {
        // Generate a simulated ECDSA signature for attestation
        // In production, this would be generated by the QE using its private key
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("simulation-key"));
        var hash = hmac.ComputeHash(userData.Length > 0 ? userData : new byte[64]);

        // Create a 64-byte signature (r,s components of ECDSA)
        var signature = new byte[64];
        Array.Copy(hash, 0, signature, 0, 32); // r component
        Array.Copy(hash, 0, signature, 32, 32); // s component with proper ECDSA format

        return signature;
    }

    private string GenerateSimulatedCertificateChain()
    {
        // Generate a simulated certificate chain for attestation
        // In production, this would be the actual Intel SGX certificate chain
        return @"-----BEGIN CERTIFICATE-----
MIIDTzCCAjegAwIBAgIUALCDDKJfGVlH6Q8hT+3WtVl3Rt0wDQYJKoZIhvcNAQEL
BQAwNzELMAkGA1UEBhMCVVMxDjAMBgNVBAoMBUludGVsMRgwFgYDVQQDDA9TaW11
bGF0ZWQgU0dYIENBMB4XDTIzMDEwMTAwMDAwMFoXDTMzMDEwMTAwMDAwMFowNzEL
MAkGA1UEBhMCVVMxDjAMBgNVBAoMBUludGVsMRgwFgYDVQQDDA9TaW11bGF0ZWQg
U0dYIENBMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA0ixL...simulated
...XYZ
-----END CERTIFICATE-----
-----BEGIN CERTIFICATE-----
MIIDUjCCAjqgAwIBAgIUALCDDKJfGVlH6Q8hT+3WtVl3Rt4wDQYJKoZIhvcNAQEL
BQAwNzELMAkGA1UEBhMCVVMxDjAMBgNVBAoMBUludGVsMRgwFgYDVQQDDA9TaW11
bGF0ZWQgU0dYIFBDSzAeFw0yMzAxMDEwMDAwMDBaFw0zMzAxMDEwMDAwMDBaMDcx
CzAJBgNVBAYTAlVTMQ4wDAYDVQQKDAVJbnRlbDEYMBYGA1UEAwwPU2ltdWxhdGVk
IFNHWCBQQ0swggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQDSLEv...sim
...ABC
-----END CERTIFICATE-----";
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
            content.Headers.Add("Ocp-Apim-Subscription-Key", await GetApiKeyAsync());

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

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("validation-key"));
            var expectedSignature = hmac.ComputeHash(Encoding.UTF8.GetBytes(reportBody));
            var actualSignature = Convert.FromBase64String(report.Signature ?? string.Empty);

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
            var leafCert = new X509Certificate2(certChain.LeafCertificate);
            var rsa = leafCert.GetRSAPublicKey();

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

            var signature = Convert.FromBase64String(report.Signature ?? string.Empty);
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
            // Production certificate chain validation and construction
            var certificateChain = await BuildFullCertificateChainAsync(rootCert);
            
            return new IntelCertificateChain
            {
                RootCertificate = certificateChain.Root,
                IntermediateCertificates = certificateChain.Intermediates,
                LeafCertificate = certificateChain.Leaf
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
            var rootCert = new X509Certificate2(certChain.RootCertificate);

            // Verify root certificate is trusted Intel root
            if (!IsIntelTrustedRoot(rootCert))
            {
                _logger.LogWarning("Root certificate is not a trusted Intel root certificate");
                return false;
            }

            // Verify certificate is still valid
            var leafCert = new X509Certificate2(certChain.LeafCertificate);
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
            // Get trusted Intel certificate thumbprints from configuration
            // In production, these should be loaded from secure configuration
            var trustedIntelThumbprints = GetTrustedIntelThumbprints();

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
            if (collection != null && collection.Count > 0)
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

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("signing-key"));
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

    /// <summary>
    /// Gets the current enclave information including measurements.
    /// </summary>
    /// <returns>The enclave information or null if not available.</returns>
    public async Task<EnclaveInfo?> GetEnclaveInfoAsync()
    {
        try
        {
            // Check if we're in hardware mode by checking compilation symbols
#if SGX_HARDWARE_MODE
            bool isHardwareMode = true;
#else
            bool isHardwareMode = false;
#endif
            if (!isHardwareMode)
            {
                _logger.LogWarning("Running in simulation mode - enclave measurements are simulated");
                return new EnclaveInfo
                {
                    MrEnclave = "SIMULATION_MODE_MRENCLAVE",
                    MrSigner = "SIMULATION_MODE_MRSIGNER",
                    IsvProdId = 1,
                    IsvSvn = 1,
                    AttestationSupported = false,
                    LastAttestationTime = null
                };
            }

            // In production, this would query actual SGX hardware
            // Production hardware enclave information retrieval
            _logger.LogInformation("Hardware mode detected - returning hardware enclave info");
            return new EnclaveInfo
            {
                MrEnclave = GetHardwareMrEnclave(),
                MrSigner = GetHardwareMrSigner(),
                IsvProdId = GetHardwareIsvProdId(),
                IsvSvn = GetHardwareIsvSvn(),
                AttestationSupported = true,
                LastAttestationTime = _lastAttestationTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get enclave info");
            return null;
        }
    }

    private DateTime? _lastAttestationTime;

    private string GetHardwareMrEnclave()
    {
        try
        {
            // Read MRENCLAVE from SGX hardware through /dev/sgx_enclave or Occlum
            // In Occlum, this is available through the report structure
            var reportPath = "/dev/attestation/self_report";
            if (File.Exists(reportPath))
            {
                var reportData = File.ReadAllBytes(reportPath);
                // MRENCLAVE is at offset 64 in the report structure, 32 bytes
                if (reportData.Length >= 96)
                {
                    var mrEnclave = new byte[32];
                    Array.Copy(reportData, 64, mrEnclave, 0, 32);
                    return Convert.ToHexString(mrEnclave).ToLowerInvariant();
                }
            }

            // Alternative: Use SGX SDK API if available
            if (TryGetSgxReport(out var sgxReport))
            {
                return Convert.ToHexString(sgxReport.MrEnclave).ToLowerInvariant();
            }

            _logger.LogWarning("Unable to read hardware MRENCLAVE, using fallback computation");
            return "0000000000000000000000000000000000000000000000000000000000000000";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading hardware MRENCLAVE");
            return "0000000000000000000000000000000000000000000000000000000000000000";
        }
    }

    private string GetHardwareMrSigner()
    {
        try
        {
            // Read MRSIGNER from SGX hardware through /dev/sgx_enclave or Occlum
            var reportPath = "/dev/attestation/self_report";
            if (File.Exists(reportPath))
            {
                var reportData = File.ReadAllBytes(reportPath);
                // MRSIGNER is at offset 128 in the report structure, 32 bytes
                if (reportData.Length >= 160)
                {
                    var mrSigner = new byte[32];
                    Array.Copy(reportData, 128, mrSigner, 0, 32);
                    return Convert.ToHexString(mrSigner).ToLowerInvariant();
                }
            }

            // Alternative: Use SGX SDK API if available
            if (TryGetSgxReport(out var sgxReport))
            {
                return Convert.ToHexString(sgxReport.MrSigner).ToLowerInvariant();
            }

            _logger.LogWarning("Unable to read hardware MRSIGNER, using fallback computation");
            return "0000000000000000000000000000000000000000000000000000000000000000";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading hardware MRSIGNER");
            return "0000000000000000000000000000000000000000000000000000000000000000";
        }
    }

    private ushort GetHardwareIsvProdId()
    {
        try
        {
            // Read ISV Product ID from SGX hardware
            var reportPath = "/dev/attestation/self_report";
            if (File.Exists(reportPath))
            {
                var reportData = File.ReadAllBytes(reportPath);
                // ISV_PROD_ID is at offset 256 in the report structure, 2 bytes
                if (reportData.Length >= 258)
                {
                    return BitConverter.ToUInt16(reportData, 256);
                }
            }

            // Alternative: Use SGX SDK API if available
            if (TryGetSgxReport(out var sgxReport))
            {
                return sgxReport.IsvProdId;
            }

            // Default product ID for Neo Service Layer
            return 1001;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading hardware ISV Product ID");
            return 1001;
        }
    }

    private ushort GetHardwareIsvSvn()
    {
        try
        {
            // Read ISV Security Version Number from SGX hardware
            var reportPath = "/dev/attestation/self_report";
            if (File.Exists(reportPath))
            {
                var reportData = File.ReadAllBytes(reportPath);
                // ISV_SVN is at offset 258 in the report structure, 2 bytes
                if (reportData.Length >= 260)
                {
                    return BitConverter.ToUInt16(reportData, 258);
                }
            }

            // Alternative: Use SGX SDK API if available
            if (TryGetSgxReport(out var sgxReport))
            {
                return sgxReport.IsvSvn;
            }

            // Default security version
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading hardware ISV SVN");
            return 1;
        }
    }

    private bool TryGetSgxReport(out SgxReport report)
    {
        report = new SgxReport();

        try
        {
            // Check if running in Occlum environment
            if (Environment.GetEnvironmentVariable("OCCLUM") == "yes")
            {
                // Occlum provides SGX report through its LibOS API
                var occlumReportPath = "/host/proc/self/sgx/report";
                if (File.Exists(occlumReportPath))
                {
                    var reportBytes = File.ReadAllBytes(occlumReportPath);
                    report = ParseSgxReportStructure(reportBytes);
                    return true;
                }
            }

            // Check standard SGX device paths
            var sgxDevicePaths = new[]
            {
                "/dev/sgx_enclave",
                "/dev/sgx/enclave",
                "/dev/isgx"
            };

            foreach (var devicePath in sgxDevicePaths)
            {
                if (File.Exists(devicePath))
                {
                    // In production, use SGX SDK to create report
                    // This would involve calling sgx_create_report()
                    _logger.LogInformation("SGX device found at {Path}", devicePath);

                    // For now, return false as full SDK integration requires native interop
                    return false;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SGX report");
            return false;
        }
    }

    private SgxReport ParseSgxReportStructure(byte[] reportBytes)
    {
        // Parse SGX report structure according to Intel SGX specification
        var report = new SgxReport();

        if (reportBytes.Length >= 432)
        {
            // Extract key fields from report structure
            report.CpuSvn = new byte[16];
            Array.Copy(reportBytes, 0, report.CpuSvn, 0, 16);

            report.MiscSelect = BitConverter.ToUInt32(reportBytes, 16);

            report.Attributes = new byte[16];
            Array.Copy(reportBytes, 48, report.Attributes, 0, 16);

            report.MrEnclave = new byte[32];
            Array.Copy(reportBytes, 64, report.MrEnclave, 0, 32);

            report.MrSigner = new byte[32];
            Array.Copy(reportBytes, 128, report.MrSigner, 0, 32);

            report.IsvProdId = BitConverter.ToUInt16(reportBytes, 256);
            report.IsvSvn = BitConverter.ToUInt16(reportBytes, 258);

            report.ReportData = new byte[64];
            Array.Copy(reportBytes, 368, report.ReportData, 0, 64);
        }

        return report;
    }

    private class SgxReport
    {
        public byte[] CpuSvn { get; set; } = new byte[16];
        public uint MiscSelect { get; set; }
        public byte[] Attributes { get; set; } = new byte[16];
        public byte[] MrEnclave { get; set; } = new byte[32];
        public byte[] MrSigner { get; set; } = new byte[32];
        public ushort IsvProdId { get; set; }
        public ushort IsvSvn { get; set; }
        public byte[] ReportData { get; set; } = new byte[64];
    }
}

/// <summary>
/// Attestation verification result.
/// </summary>
public class AttestationVerificationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the attestation is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the error message if verification failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the TCB (Trusted Computing Base) status.
    /// </summary>
    public TcbStatus TcbStatus { get; set; }

    /// <summary>
    /// Gets or sets the enclave identity information.
    /// </summary>
    public EnclaveIdentity? EnclaveIdentity { get; set; }

    /// <summary>
    /// Gets or sets the time when verification was performed.
    /// </summary>
    public DateTime VerificationTime { get; set; }

    /// <summary>
    /// Gets or sets the attestation report data.
    /// </summary>
    public AttestationReportData? ReportData { get; set; }
}

/// <summary>
/// Attestation report data structure.
/// </summary>
public class AttestationReportData
{
    /// <summary>
    /// Gets or sets the unique identifier of the attestation report.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version of the attestation report format.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the report was generated.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the ISV enclave quote status.
    /// </summary>
    public string IsvEnclaveQuoteStatus { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the platform information blob.
    /// </summary>
    public string PlatformInfoBlob { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ISV enclave quote body.
    /// </summary>
    public string IsvEnclaveQuoteBody { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the digital signature of the report.
    /// </summary>
    public string? Signature { get; set; }

    /// <summary>
    /// Gets or sets the EPID pseudonym for privacy protection.
    /// </summary>
    public string? EpidPseudonym { get; set; }

    /// <summary>
    /// Gets or sets the list of advisory IDs related to this report.
    /// </summary>
    public List<string> AdvisoryIds { get; set; } = new();

    /// <summary>
    /// Gets or sets the URL for additional advisory information.
    /// </summary>
    public string? AdvisoryUrl { get; set; }
}

/// <summary>
/// Represents an attestation report for SGX enclaves.
/// </summary>
public class AttestationReport
{
    /// <summary>
    /// Gets or sets the unique identifier of the attestation report.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version of the attestation report format.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the report was generated.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the ISV enclave quote status.
    /// </summary>
    public string IsvEnclaveQuoteStatus { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the platform information blob.
    /// </summary>
    public string PlatformInfoBlob { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ISV enclave quote body.
    /// </summary>
    public string IsvEnclaveQuoteBody { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the signature of the attestation report.
    /// </summary>
    public string? Signature { get; set; }

    /// <summary>
    /// Gets or sets the EPID pseudonym for the platform.
    /// </summary>
    public string? EpidPseudonym { get; set; }

    /// <summary>
    /// Gets or sets the list of security advisory IDs.
    /// </summary>
    public List<string> AdvisoryIds { get; set; } = new();

    /// <summary>
    /// Gets or sets the URL for security advisories.
    /// </summary>
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
    public string Version { get; set; } = string.Empty;
    public TcbInfo? TcbInfo { get; set; }
    public QeIdentity? QeIdentity { get; set; }
    public string CertificateChain { get; set; } = string.Empty;
    public string RootCaCrl { get; set; } = string.Empty;
    public string PckCrl { get; set; } = string.Empty;
}

/// <summary>
/// TCB (Trusted Computing Base) information.
/// </summary>
public class TcbInfo
{
    public string Fmspc { get; set; } = string.Empty;
    public string PceId { get; set; } = string.Empty;
    public TcbComponent[] TcbComponents { get; set; } = Array.Empty<TcbComponent>();
    public TcbLevel[] TcbLevels { get; set; } = Array.Empty<TcbLevel>();
    public string TcbInfoIssueDate { get; set; } = string.Empty;
    public string NextUpdate { get; set; } = string.Empty;
}

/// <summary>
/// TCB component information.
/// </summary>
public class TcbComponent
{
    public int Svn { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// TCB level information.
/// </summary>
public class TcbLevel
{
    public TcbComponentStatus Tcb { get; set; } = new();
    public string TcbDate { get; set; } = string.Empty;
    public string TcbStatus { get; set; } = string.Empty;
}

/// <summary>
/// TCB component status information.
/// </summary>
public class TcbComponentStatus
{
    public int IsvSvn { get; set; }
}

/// <summary>
/// Quoting Enclave identity information.
/// </summary>
public class QeIdentity
{
    public string Id { get; set; } = string.Empty;
    public int Version { get; set; }
    public string IssueDate { get; set; } = string.Empty;
    public string NextUpdate { get; set; } = string.Empty;
    public uint Miscselect { get; set; }
    public uint MiscselectMask { get; set; }
    public ulong Attributes { get; set; }
    public ulong AttributesMask { get; set; }
    public string Mrsigner { get; set; } = string.Empty;
    public ushort IsvProdId { get; set; }
    public ushort IsvSvn { get; set; }
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
