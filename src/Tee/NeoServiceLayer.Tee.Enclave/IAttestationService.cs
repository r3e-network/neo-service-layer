using System;
using System.Threading.Tasks;

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
}
