using Neo.Compute.Service.Models;

namespace Neo.Compute.Service.Services;

public interface IAttestationService
{
    Task<AttestationResponse> PerformAttestationAsync(AttestationRequest request);
    Task<AttestationResponse?> GetAttestationAsync(Guid attestationId);
    Task<List<AttestationResponse>> GetAttestationsForEnclaveAsync(Guid enclaveId);
    Task<bool> ValidateAttestationAsync(Guid attestationId);
    Task<bool> IsEnclaveAttestedAsync(Guid enclaveId);
    Task<AttestationResponse?> GetLatestAttestationAsync(Guid enclaveId);
    Task<bool> RevokeAttestationAsync(Guid attestationId);
}