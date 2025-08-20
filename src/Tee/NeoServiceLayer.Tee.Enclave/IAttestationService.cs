using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System;


namespace NeoServiceLayer.Tee.Enclave;

/// <summary>
/// Interface for handling SGX attestation verification.
/// </summary>
public interface IAttestationService
{
    /// <summary>
    /// Verifies a remote attestation report.
    /// </summary>
    /// <param name="attestationReport">The attestation report to verify.</param>
    /// <returns>The attestation verification result.</returns>
    Task<AttestationVerificationResult> VerifyAttestationAsync(string attestationReport);

    /// <summary>
    /// Generates an attestation report for the enclave.
    /// </summary>
    /// <param name="userData">User data to include in the attestation report.</param>
    /// <returns>The generated attestation report.</returns>
    Task<AttestationReport> GenerateAttestationReportAsync(byte[] userData);

    /// <summary>
    /// Gets the current enclave information including measurements.
    /// </summary>
    /// <returns>The enclave information or null if not available.</returns>
    Task<EnclaveInfo?> GetEnclaveInfoAsync();
}

/// <summary>
/// Enclave information including measurements.
/// </summary>
public class EnclaveInfo
{
    /// <summary>
    /// Gets or sets the MR_ENCLAVE measurement.
    /// </summary>
    public string MrEnclave { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MR_SIGNER measurement.
    /// </summary>
    public string MrSigner { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ISV product ID.
    /// </summary>
    public ushort IsvProdId { get; set; }

    /// <summary>
    /// Gets or sets the ISV security version number.
    /// </summary>
    public ushort IsvSvn { get; set; }

    /// <summary>
    /// Gets or sets whether attestation is supported.
    /// </summary>
    public bool AttestationSupported { get; set; }

    /// <summary>
    /// Gets or sets the last attestation time.
    /// </summary>
    public DateTime? LastAttestationTime { get; set; }

    /// <summary>
    /// Gets or sets the enclave type.
    /// </summary>
    public string EnclaveType { get; set; } = "SGX";

    /// <summary>
    /// Gets or sets the enclave version.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the maximum data size supported.
    /// </summary>
    public long MaxDataSize { get; set; } = 100 * 1024 * 1024; // 100MB

    /// <summary>
    /// Gets or sets the maximum execution time in milliseconds.
    /// </summary>
    public int MaxExecutionTime { get; set; } = 30000; // 30 seconds

    /// <summary>
    /// Gets or sets whether the enclave is initialized.
    /// </summary>
    public bool IsInitialized { get; set; }

    /// <summary>
    /// Gets or sets the enclave capabilities.
    /// </summary>
    public string[] Capabilities { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the attestation status.
    /// </summary>
    public string AttestationStatus { get; set; } = "NotAttested";
}
