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
}
