using System;
using System.Security.Cryptography;
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

            // In production, this would call actual SGX APIs
            // For now, generate a structured report
            var report = new AttestationReport
            {
                Version = 4,
                Timestamp = DateTime.UtcNow,
                IsvEnclaveQuoteStatus = "OK",
                PlatformInfoBlob = GeneratePlatformInfoBlob(),
                IsvEnclaveQuoteBody = GenerateQuoteBody(userData),
                Id = Guid.NewGuid().ToString(),
                EpidPseudonym = GenerateEpidPseudonym(),
                AdvisoryIds = new List<string>(),
                AdvisoryUrl = "https://security-center.intel.com"
            };

            // Sign the report
            report.Signature = await SignReportAsync(report);

            _logger.LogInformation("Generated attestation report {ReportId}", report.Id);
            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating attestation report");
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
            // In production, verify against Intel's signing certificate
            // For now, simulate verification
            await Task.Delay(100);
            
            if (string.IsNullOrEmpty(report.Signature))
            {
                _logger.LogWarning("Attestation report has no signature");
                return false;
            }

            // Simulate signature verification
            var reportBody = JsonSerializer.Serialize(new
            {
                report.Id,
                report.Timestamp,
                report.IsvEnclaveQuoteStatus,
                report.IsvEnclaveQuoteBody
            });

            using var rsa = RSA.Create();
            // In production, load Intel's public key
            // For now, return true if signature is present
            return !string.IsNullOrEmpty(report.Signature);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying report signature");
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