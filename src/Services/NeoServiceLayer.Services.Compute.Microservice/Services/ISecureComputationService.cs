using Neo.Compute.Service.Models;

namespace Neo.Compute.Service.Services;

public interface ISecureComputationService
{
    Task<SecureComputationResponse> PerformComputationAsync(SecureComputationRequest request);
    Task<SecureComputationSession?> GetSessionAsync(Guid sessionId);
    Task<bool> TerminateSessionAsync(Guid sessionId);
    Task<List<SecureComputationSession>> GetActiveSessionsAsync();
    Task<List<SecureComputationSession>> GetSessionsForEnclaveAsync(Guid enclaveId);
    Task<bool> ValidateSessionAsync(Guid sessionId);
}