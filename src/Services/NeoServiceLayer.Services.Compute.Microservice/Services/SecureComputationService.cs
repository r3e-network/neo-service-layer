using Microsoft.EntityFrameworkCore;
using Neo.Compute.Service.Data;
using Neo.Compute.Service.Models;
using Neo.Compute.Service.Services;
using System.Security.Cryptography;
using System.Text;

namespace Neo.Compute.Service.Services;

public class SecureComputationService : ISecureComputationService
{
    private readonly ComputeDbContext _context;
    private readonly ISgxEnclaveService _enclaveService;
    private readonly IAttestationService _attestationService;
    private readonly ILogger<SecureComputationService> _logger;

    public SecureComputationService(
        ComputeDbContext context,
        ISgxEnclaveService enclaveService,
        IAttestationService attestationService,
        ILogger<SecureComputationService> logger)
    {
        _context = context;
        _enclaveService = enclaveService;
        _attestationService = attestationService;
        _logger = logger;
    }

    public async Task<SecureComputationResponse> PerformComputationAsync(SecureComputationRequest request)
    {
        // Find an available attested enclave
        var enclave = string.IsNullOrEmpty(request.EnclaveId)
            ? await _enclaveService.GetAvailableEnclaveAsync()
            : await _enclaveService.GetEnclaveAsync(Guid.Parse(request.EnclaveId));

        if (enclave == null)
        {
            throw new InvalidOperationException("No available SGX enclave found for secure computation");
        }

        // Verify enclave is attested
        var isAttested = await _attestationService.IsEnclaveAttestedAsync(Guid.Parse(enclave.Id));
        if (!isAttested)
        {
            throw new InvalidOperationException("Selected enclave is not properly attested");
        }

        // Create secure computation session
        var session = new SecureComputationSession
        {
            Id = Guid.NewGuid(),
            SessionToken = GenerateSessionToken(),
            EnclaveId = Guid.Parse(enclave.Id),
            UserId = GetCurrentUserId(), // This would come from authentication context
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(request.TimeoutDuration ?? TimeSpan.FromHours(1)),
            EncryptedData = request.EncryptedData,
            DataHash = request.DataHash
        };

        _context.SecureComputationSessions.Add(session);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Secure computation session created: {SessionId} on enclave: {EnclaveId}", 
            session.Id, enclave.Id);

        // Perform the actual secure computation
        var computationResult = await ExecuteSecureComputationAsync(session, request);

        // Update session with results
        session.Status = SessionStatus.Terminated;
        session.LastActivityAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new SecureComputationResponse
        {
            SessionId = session.Id.ToString(),
            EnclaveId = enclave.Id,
            EncryptedResult = computationResult.EncryptedResult,
            ResultHash = computationResult.ResultHash,
            StartedAt = session.CreatedAt,
            CompletedAt = session.LastActivityAt,
            AttestationProof = computationResult.AttestationProof,
            ResourceUsage = computationResult.ResourceUsage
        };
    }

    public async Task<SecureComputationSession?> GetSessionAsync(Guid sessionId)
    {
        return await _context.SecureComputationSessions
            .Include(s => s.Enclave)
            .FirstOrDefaultAsync(s => s.Id == sessionId);
    }

    public async Task<bool> TerminateSessionAsync(Guid sessionId)
    {
        var session = await _context.SecureComputationSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null) return false;

        session.Status = SessionStatus.Terminated;
        session.LastActivityAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Secure computation session terminated: {SessionId}", sessionId);
        return true;
    }

    public async Task<List<SecureComputationSession>> GetActiveSessionsAsync()
    {
        return await _context.SecureComputationSessions
            .Where(s => s.Status == SessionStatus.Active && s.ExpiresAt > DateTime.UtcNow)
            .Include(s => s.Enclave)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<SecureComputationSession>> GetSessionsForEnclaveAsync(Guid enclaveId)
    {
        return await _context.SecureComputationSessions
            .Where(s => s.EnclaveId == enclaveId)
            .Include(s => s.Enclave)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> ValidateSessionAsync(Guid sessionId)
    {
        var session = await _context.SecureComputationSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null) return false;

        return session.Status == SessionStatus.Active && 
               session.ExpiresAt > DateTime.UtcNow;
    }

    private async Task<SecureComputationResult> ExecuteSecureComputationAsync(
        SecureComputationSession session, 
        SecureComputationRequest request)
    {
        try
        {
            _logger.LogInformation("Executing secure computation in enclave: {EnclaveId}", session.EnclaveId);

            // In a real implementation, this would:
            // 1. Send the encrypted data to the SGX enclave
            // 2. Execute the specified algorithm inside the enclave
            // 3. Return encrypted results with attestation proof
            // 4. Measure resource usage during computation

            // Simulate secure computation
            var computationTime = GetComputationTime(request.Algorithm);
            await Task.Delay(computationTime);

            // Generate mock encrypted result
            var result = await GenerateMockComputationResultAsync(request);

            // Get attestation proof from enclave
            var attestationProof = await GenerateAttestationProofAsync(session.EnclaveId);

            return new SecureComputationResult
            {
                EncryptedResult = result.EncryptedData,
                ResultHash = result.DataHash,
                AttestationProof = attestationProof,
                ResourceUsage = new ComputeResourceUsage
                {
                    CpuCores = 1,
                    MemoryUsedMb = 256,
                    StorageUsedMb = 10,
                    Duration = TimeSpan.FromMilliseconds(computationTime),
                    Cost = CalculateComputationCost(computationTime, 1, 256)
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during secure computation: {SessionId}", session.Id);
            throw;
        }
    }

    private async Task<(string EncryptedData, string DataHash)> GenerateMockComputationResultAsync(SecureComputationRequest request)
    {
        // Simulate computation result based on algorithm
        var resultData = request.Algorithm.ToLower() switch
        {
            "sum" => "encrypted_sum_result_12345",
            "multiply" => "encrypted_multiply_result_67890",
            "hash" => "encrypted_hash_result_abcdef",
            "ml_inference" => "encrypted_ml_prediction_result",
            _ => "encrypted_generic_computation_result"
        };

        // Add timestamp to make result unique
        var timestampedResult = $"{resultData}_{DateTime.UtcNow:yyyyMMddHHmmss}";
        
        // Generate hash of the result
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(timestampedResult));
        var resultHash = Convert.ToHexString(hashBytes);

        await Task.Delay(100); // Simulate encryption time

        return (timestampedResult, resultHash);
    }

    private async Task<string> GenerateAttestationProofAsync(Guid enclaveId)
    {
        try
        {
            // Get the latest attestation for the enclave
            var attestation = await _attestationService.GetLatestAttestationAsync(enclaveId);
            
            if (attestation == null || !attestation.IsValid)
            {
                return "no_attestation_available";
            }

            // In a real implementation, this would generate a cryptographic proof
            // that links the computation to the attested enclave
            var proofData = new
            {
                EnclaveId = enclaveId,
                AttestationId = attestation.AttestationId,
                ComputationTimestamp = DateTime.UtcNow,
                ProofVersion = "1.0"
            };

            // Generate a mock proof
            var proofJson = System.Text.Json.JsonSerializer.Serialize(proofData);
            using var sha256 = SHA256.Create();
            var proofHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(proofJson));
            
            return Convert.ToBase64String(proofHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate attestation proof for enclave: {EnclaveId}", enclaveId);
            return "proof_generation_failed";
        }
    }

    private static int GetComputationTime(string algorithm)
    {
        // Simulate different computation times based on algorithm complexity
        return algorithm.ToLower() switch
        {
            "sum" => 1000,          // 1 second for simple operations
            "multiply" => 1500,     // 1.5 seconds for multiplication
            "hash" => 2000,         // 2 seconds for hashing
            "ml_inference" => 5000, // 5 seconds for ML inference
            "encryption" => 3000,   // 3 seconds for encryption
            _ => 2000               // 2 seconds default
        };
    }

    private static decimal CalculateComputationCost(int computationTimeMs, int cpuCores, long memoryMb)
    {
        // Simple cost calculation based on resources and time
        var timeHours = computationTimeMs / (1000.0 * 3600.0); // Convert to hours
        var cpuCost = (decimal)(cpuCores * timeHours * 0.10); // $0.10 per CPU hour
        var memoryCost = (decimal)(memoryMb / 1024.0 * timeHours * 0.02); // $0.02 per GB hour
        
        return cpuCost + memoryCost;
    }

    private static string GenerateSessionToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var tokenBytes = new byte[32];
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes);
    }

    private static string GetCurrentUserId()
    {
        // In a real implementation, this would get the user ID from the authentication context
        return "system_user";
    }

    private class SecureComputationResult
    {
        public string EncryptedResult { get; set; } = "";
        public string ResultHash { get; set; } = "";
        public string AttestationProof { get; set; } = "";
        public ComputeResourceUsage ResourceUsage { get; set; } = new();
    }
}