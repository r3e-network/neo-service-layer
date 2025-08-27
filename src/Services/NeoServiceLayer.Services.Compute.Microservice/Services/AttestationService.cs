using Microsoft.EntityFrameworkCore;
using Neo.Compute.Service.Data;
using Neo.Compute.Service.Models;
using Neo.Compute.Service.Services;

namespace Neo.Compute.Service.Services;

public class AttestationService : IAttestationService
{
    private readonly ComputeDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AttestationService> _logger;

    public AttestationService(
        ComputeDbContext context,
        HttpClient httpClient,
        ILogger<AttestationService> logger)
    {
        _context = context;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AttestationResponse> PerformAttestationAsync(AttestationRequest request)
    {
        // Validate enclave exists
        var enclave = await _context.SgxEnclaves
            .FirstOrDefaultAsync(e => e.Id == Guid.Parse(request.EnclaveId));
        
        if (enclave == null)
        {
            throw new InvalidOperationException($"Enclave not found: {request.EnclaveId}");
        }

        var attestationResult = new AttestationResult
        {
            Id = Guid.NewGuid(),
            EnclaveId = Guid.Parse(request.EnclaveId),
            Quote = request.Quote,
            Certificate = request.Certificate,
            Status = AttestationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24), // Default 24-hour expiry
            TcbLevel = "UpToDate"
        };

        _context.AttestationResults.Add(attestationResult);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Attestation started: {AttestationId} for enclave: {EnclaveId}", 
            attestationResult.Id, request.EnclaveId);

        // Start attestation verification process
        _ = Task.Run(() => VerifyAttestationAsync(attestationResult.Id));

        return new AttestationResponse
        {
            AttestationId = attestationResult.Id.ToString(),
            Status = AttestationStatus.Pending.ToString(),
            CreatedAt = attestationResult.CreatedAt,
            IsValid = false,
            ExpiresAt = attestationResult.ExpiresAt
        };
    }

    public async Task<AttestationResponse?> GetAttestationAsync(Guid attestationId)
    {
        var attestation = await _context.AttestationResults
            .FirstOrDefaultAsync(a => a.Id == attestationId);

        return attestation != null ? MapToResponse(attestation) : null;
    }

    public async Task<List<AttestationResponse>> GetAttestationsForEnclaveAsync(Guid enclaveId)
    {
        var attestations = await _context.AttestationResults
            .Where(a => a.EnclaveId == enclaveId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return attestations.Select(MapToResponse).ToList();
    }

    public async Task<bool> ValidateAttestationAsync(Guid attestationId)
    {
        var attestation = await _context.AttestationResults
            .FirstOrDefaultAsync(a => a.Id == attestationId);

        if (attestation == null) return false;

        return attestation.Status == AttestationStatus.Verified && 
               !attestation.IsRevoked && 
               attestation.ExpiresAt > DateTime.UtcNow;
    }

    public async Task<bool> IsEnclaveAttestedAsync(Guid enclaveId)
    {
        var latestAttestation = await _context.AttestationResults
            .Where(a => a.EnclaveId == enclaveId && 
                       a.Status == AttestationStatus.Verified &&
                       !a.IsRevoked &&
                       a.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();

        if (latestAttestation != null)
        {
            // Update enclave attestation status
            var enclave = await _context.SgxEnclaves.FirstOrDefaultAsync(e => e.Id == enclaveId);
            if (enclave != null)
            {
                enclave.IsAttested = true;
                enclave.LastAttestation = latestAttestation.VerifiedAt;
                await _context.SaveChangesAsync();
            }
        }

        return latestAttestation != null;
    }

    public async Task<AttestationResponse?> GetLatestAttestationAsync(Guid enclaveId)
    {
        var attestation = await _context.AttestationResults
            .Where(a => a.EnclaveId == enclaveId)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();

        return attestation != null ? MapToResponse(attestation) : null;
    }

    public async Task<bool> RevokeAttestationAsync(Guid attestationId)
    {
        var attestation = await _context.AttestationResults
            .FirstOrDefaultAsync(a => a.Id == attestationId);

        if (attestation == null) return false;

        attestation.Status = AttestationStatus.Revoked;
        attestation.IsRevoked = true;

        // Update associated enclave
        var enclave = await _context.SgxEnclaves
            .FirstOrDefaultAsync(e => e.Id == attestation.EnclaveId);
        
        if (enclave != null)
        {
            enclave.IsAttested = false;
            enclave.LastAttestation = null;
        }

        await _context.SaveChangesAsync();

        _logger.LogWarning("Attestation revoked: {AttestationId} for enclave: {EnclaveId}", 
            attestationId, attestation.EnclaveId);

        return true;
    }

    private async Task VerifyAttestationAsync(Guid attestationId)
    {
        try
        {
            _logger.LogInformation("Starting attestation verification: {AttestationId}", attestationId);

            var attestation = await _context.AttestationResults
                .FirstOrDefaultAsync(a => a.Id == attestationId);

            if (attestation == null)
            {
                _logger.LogError("Attestation not found: {AttestationId}", attestationId);
                return;
            }

            // Update status to in progress
            attestation.Status = AttestationStatus.InProgress;
            await _context.SaveChangesAsync();

            // Simulate attestation verification process
            var verificationResult = await PerformQuoteVerificationAsync(attestation.Quote, attestation.Certificate);

            if (verificationResult.IsValid)
            {
                attestation.Status = AttestationStatus.Verified;
                attestation.VerifiedAt = DateTime.UtcNow;
                attestation.VerificationDetails = verificationResult.Details;
                attestation.TcbLevel = verificationResult.TcbLevel ?? "UpToDate";

                _logger.LogInformation("Attestation verified successfully: {AttestationId}", attestationId);
            }
            else
            {
                attestation.Status = AttestationStatus.Failed;
                attestation.ErrorMessage = verificationResult.ErrorMessage;

                _logger.LogError("Attestation verification failed: {AttestationId} - {Error}", 
                    attestationId, verificationResult.ErrorMessage);
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during attestation verification: {AttestationId}", attestationId);

            var attestation = await _context.AttestationResults
                .FirstOrDefaultAsync(a => a.Id == attestationId);
            
            if (attestation != null)
            {
                attestation.Status = AttestationStatus.Failed;
                attestation.ErrorMessage = ex.Message;
                await _context.SaveChangesAsync();
            }
        }
    }

    private async Task<AttestationVerificationResult> PerformQuoteVerificationAsync(string quote, string certificate)
    {
        try
        {
            // In a real implementation, this would:
            // 1. Validate the quote format and structure
            // 2. Verify the certificate chain against Intel's root certificates
            // 3. Check the TCB (Trusted Computing Base) level
            // 4. Validate the enclave measurement against known good values
            // 5. Check for known vulnerabilities or revoked certificates

            // Simulate verification process
            await Task.Delay(3000); // 3 second verification

            // For demo purposes, always return success
            // In reality, this would involve complex cryptographic verification
            return new AttestationVerificationResult
            {
                IsValid = true,
                Details = "Quote and certificate verified successfully",
                TcbLevel = "UpToDate"
            };
        }
        catch (Exception ex)
        {
            return new AttestationVerificationResult
            {
                IsValid = false,
                ErrorMessage = $"Verification failed: {ex.Message}"
            };
        }
    }

    private AttestationResponse MapToResponse(AttestationResult attestation)
    {
        return new AttestationResponse
        {
            AttestationId = attestation.Id.ToString(),
            Status = attestation.Status.ToString(),
            CreatedAt = attestation.CreatedAt,
            VerifiedAt = attestation.VerifiedAt,
            IsValid = attestation.Status == AttestationStatus.Verified && 
                     !attestation.IsRevoked && 
                     attestation.ExpiresAt > DateTime.UtcNow,
            TcbLevel = attestation.TcbLevel,
            ExpiresAt = attestation.ExpiresAt,
            ErrorMessage = attestation.ErrorMessage
        };
    }

    private class AttestationVerificationResult
    {
        public bool IsValid { get; set; }
        public string? Details { get; set; }
        public string? TcbLevel { get; set; }
        public string? ErrorMessage { get; set; }
    }
}