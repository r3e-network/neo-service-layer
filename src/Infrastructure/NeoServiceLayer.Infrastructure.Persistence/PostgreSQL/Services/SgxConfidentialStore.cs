using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Contexts;

namespace NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Services
{
    /// <summary>
    /// PostgreSQL-backed confidential data storage for SGX services
    /// Provides sealed data management with attestation support
    /// </summary>
    public interface ISgxConfidentialStore
    {
        Task<string> SealDataAsync(string key, byte[] data, string serviceName, string policyType = "MRSIGNER", int? expirationHours = null);
        Task<byte[]?> UnsealDataAsync(string key, string serviceName);
        Task<bool> DeleteSealedDataAsync(string key, string serviceName);
        Task<IEnumerable<SealedDataInfo>> ListSealedDataAsync(string serviceName);
        Task<bool> StoreAttestationAsync(string attestationId, byte[] quote, byte[] report, string mrenclave, string mrsigner);
        Task<EnclaveAttestation?> GetAttestationAsync(string attestationId);
        Task<bool> VerifyAttestationAsync(string attestationId, string status, string? result = null);
        Task CleanupExpiredDataAsync();
        Task<SealingPolicy?> GetPolicyAsync(string policyType);
        Task<SealingPolicy> CreatePolicyAsync(string name, string policyType, int expirationHours, bool requireAttestation);
    }

    public class SealedDataInfo
    {
        public string Key { get; set; } = string.Empty;
        public string StorageId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int AccessCount { get; set; }
        public bool IsExpired { get; set; }
        public string PolicyType { get; set; } = string.Empty;
    }

    public class SgxConfidentialStore : ISgxConfidentialStore
    {
        private readonly NeoServiceDbContext _context;
        private readonly ILogger<SgxConfidentialStore> _logger;

        public SgxConfidentialStore(
            NeoServiceDbContext context,
            ILogger<SgxConfidentialStore> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> SealDataAsync(
            string key, 
            byte[] data, 
            string serviceName, 
            string policyType = "MRSIGNER",
            int? expirationHours = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be empty", nameof(key));
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be empty", nameof(data));
            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentException("Service name cannot be empty", nameof(serviceName));

            try
            {
                // Get or create policy
                var policy = await GetOrCreatePolicyAsync(policyType, expirationHours ?? 24);
                
                // Generate storage ID
                var storageId = GenerateStorageId(key, serviceName);
                
                // Check if data already exists
                var existing = await _context.SealedDataItems
                    .FirstOrDefaultAsync(s => s.Key == key && s.ServiceName == serviceName);

                if (existing != null)
                {
                    // Update existing sealed data
                    existing.SealedData = data;
                    existing.OriginalSize = data.Length;
                    existing.SealedSize = data.Length;
                    existing.PolicyType = policyType;
                    existing.ExpiresAt = DateTime.UtcNow.AddHours(policy.ExpirationHours);
                    existing.Fingerprint = ComputeFingerprint(data);
                    existing.LastAccessed = DateTime.UtcNow;
                    existing.AccessCount++;
                    
                    _context.SealedDataItems.Update(existing);
                }
                else
                {
                    // Create new sealed data item
                    var sealedItem = new SealedDataItem
                    {
                        Key = key,
                        ServiceName = serviceName,
                        StorageId = storageId,
                        SealedData = data,
                        OriginalSize = data.Length,
                        SealedSize = data.Length,
                        PolicyType = policyType,
                        Policy = policy,
                        ExpiresAt = DateTime.UtcNow.AddHours(policy.ExpirationHours),
                        Fingerprint = ComputeFingerprint(data),
                        Metadata = JsonSerializer.Serialize(new
                        {
                            serviceName,
                            sealedAt = DateTime.UtcNow,
                            policyType
                        })
                    };

                    await _context.SealedDataItems.AddAsync(sealedItem);
                }

                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Sealed data for key {Key} in service {Service} with policy {Policy}",
                    key, serviceName, policyType);
                
                return storageId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seal data for key {Key} in service {Service}", 
                    key, serviceName);
                throw;
            }
        }

        public async Task<byte[]?> UnsealDataAsync(string key, string serviceName)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be empty", nameof(key));
            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentException("Service name cannot be empty", nameof(serviceName));

            try
            {
                var sealedItem = await _context.SealedDataItems
                    .Include(s => s.Policy)
                    .FirstOrDefaultAsync(s => 
                        s.Key == key && 
                        s.ServiceName == serviceName &&
                        !s.IsExpired);

                if (sealedItem == null)
                {
                    _logger.LogWarning("Sealed data not found for key {Key} in service {Service}", 
                        key, serviceName);
                    return null;
                }

                // Check if unsealing is allowed by policy
                if (sealedItem.Policy != null && !sealedItem.Policy.AllowUnseal)
                {
                    _logger.LogWarning("Unsealing not allowed by policy for key {Key}", key);
                    return null;
                }

                // Update access information
                sealedItem.LastAccessed = DateTime.UtcNow;
                sealedItem.AccessCount++;
                
                _context.SealedDataItems.Update(sealedItem);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Unsealed data for key {Key} in service {Service}", 
                    key, serviceName);
                
                return sealedItem.SealedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unseal data for key {Key} in service {Service}", 
                    key, serviceName);
                throw;
            }
        }

        public async Task<bool> DeleteSealedDataAsync(string key, string serviceName)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be empty", nameof(key));
            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentException("Service name cannot be empty", nameof(serviceName));

            try
            {
                var sealedItem = await _context.SealedDataItems
                    .Include(s => s.Attestations)
                    .FirstOrDefaultAsync(s => 
                        s.Key == key && 
                        s.ServiceName == serviceName);

                if (sealedItem == null)
                {
                    _logger.LogWarning("Sealed data not found for deletion: key {Key} in service {Service}", 
                        key, serviceName);
                    return false;
                }

                // Remove associated attestations
                _context.EnclaveAttestations.RemoveRange(sealedItem.Attestations);
                
                // Remove sealed data item
                _context.SealedDataItems.Remove(sealedItem);
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Deleted sealed data for key {Key} in service {Service}", 
                    key, serviceName);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete sealed data for key {Key} in service {Service}", 
                    key, serviceName);
                throw;
            }
        }

        public async Task<IEnumerable<SealedDataInfo>> ListSealedDataAsync(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentException("Service name cannot be empty", nameof(serviceName));

            try
            {
                var items = await _context.SealedDataItems
                    .Where(s => s.ServiceName == serviceName)
                    .Select(s => new SealedDataInfo
                    {
                        Key = s.Key,
                        StorageId = s.StorageId,
                        CreatedAt = s.CreatedAt,
                        ExpiresAt = s.ExpiresAt,
                        AccessCount = s.AccessCount,
                        IsExpired = s.IsExpired,
                        PolicyType = s.PolicyType
                    })
                    .ToListAsync();

                _logger.LogInformation("Listed {Count} sealed data items for service {Service}", 
                    items.Count, serviceName);
                
                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list sealed data for service {Service}", serviceName);
                throw;
            }
        }

        public async Task<bool> StoreAttestationAsync(
            string attestationId, 
            byte[] quote, 
            byte[] report, 
            string mrenclave, 
            string mrsigner)
        {
            if (string.IsNullOrWhiteSpace(attestationId))
                throw new ArgumentException("Attestation ID cannot be empty", nameof(attestationId));
            if (quote == null || quote.Length == 0)
                throw new ArgumentException("Quote cannot be empty", nameof(quote));
            if (report == null || report.Length == 0)
                throw new ArgumentException("Report cannot be empty", nameof(report));

            try
            {
                var attestation = new EnclaveAttestation
                {
                    AttestationId = attestationId,
                    Quote = quote,
                    Report = report,
                    MRENCLAVE = mrenclave ?? string.Empty,
                    MRSIGNER = mrsigner ?? string.Empty,
                    Status = "Pending",
                    ExpiresAt = DateTime.UtcNow.AddHours(24)
                };

                await _context.EnclaveAttestations.AddAsync(attestation);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Stored attestation {AttestationId}", attestationId);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store attestation {AttestationId}", attestationId);
                throw;
            }
        }

        public async Task<EnclaveAttestation?> GetAttestationAsync(string attestationId)
        {
            if (string.IsNullOrWhiteSpace(attestationId))
                throw new ArgumentException("Attestation ID cannot be empty", nameof(attestationId));

            try
            {
                var attestation = await _context.EnclaveAttestations
                    .FirstOrDefaultAsync(a => a.AttestationId == attestationId);

                if (attestation != null)
                {
                    _logger.LogInformation("Retrieved attestation {AttestationId}", attestationId);
                }
                
                return attestation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get attestation {AttestationId}", attestationId);
                throw;
            }
        }

        public async Task<bool> VerifyAttestationAsync(
            string attestationId, 
            string status, 
            string? result = null)
        {
            if (string.IsNullOrWhiteSpace(attestationId))
                throw new ArgumentException("Attestation ID cannot be empty", nameof(attestationId));
            if (string.IsNullOrWhiteSpace(status))
                throw new ArgumentException("Status cannot be empty", nameof(status));

            try
            {
                var attestation = await _context.EnclaveAttestations
                    .FirstOrDefaultAsync(a => a.AttestationId == attestationId);

                if (attestation == null)
                {
                    _logger.LogWarning("Attestation {AttestationId} not found for verification", attestationId);
                    return false;
                }

                attestation.Status = status;
                attestation.VerifiedAt = DateTime.UtcNow;
                attestation.VerificationResult = result;
                
                _context.EnclaveAttestations.Update(attestation);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Verified attestation {AttestationId} with status {Status}", 
                    attestationId, status);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify attestation {AttestationId}", attestationId);
                throw;
            }
        }

        public async Task CleanupExpiredDataAsync()
        {
            try
            {
                // Remove expired sealed data items
                var expiredItems = await _context.SealedDataItems
                    .Include(s => s.Attestations)
                    .Where(s => s.ExpiresAt < DateTime.UtcNow)
                    .ToListAsync();

                if (expiredItems.Any())
                {
                    // Remove associated attestations
                    var attestations = expiredItems.SelectMany(s => s.Attestations);
                    _context.EnclaveAttestations.RemoveRange(attestations);
                    
                    // Remove sealed data items
                    _context.SealedDataItems.RemoveRange(expiredItems);
                    
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Cleaned up {Count} expired sealed data items", expiredItems.Count);
                }

                // Remove expired attestations not linked to sealed data
                var expiredAttestations = await _context.EnclaveAttestations
                    .Where(a => a.ExpiresAt < DateTime.UtcNow && a.SealedDataItemId == null)
                    .ToListAsync();

                if (expiredAttestations.Any())
                {
                    _context.EnclaveAttestations.RemoveRange(expiredAttestations);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Cleaned up {Count} expired attestations", expiredAttestations.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup expired data");
                throw;
            }
        }

        public async Task<SealingPolicy?> GetPolicyAsync(string policyType)
        {
            if (string.IsNullOrWhiteSpace(policyType))
                throw new ArgumentException("Policy type cannot be empty", nameof(policyType));

            try
            {
                return await _context.SealingPolicies
                    .FirstOrDefaultAsync(p => 
                        p.PolicyType == policyType && 
                        p.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get policy {PolicyType}", policyType);
                throw;
            }
        }

        public async Task<SealingPolicy> CreatePolicyAsync(
            string name, 
            string policyType, 
            int expirationHours, 
            bool requireAttestation)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));
            if (string.IsNullOrWhiteSpace(policyType))
                throw new ArgumentException("Policy type cannot be empty", nameof(policyType));
            if (expirationHours <= 0)
                throw new ArgumentException("Expiration hours must be positive", nameof(expirationHours));

            try
            {
                var policy = new SealingPolicy
                {
                    Name = name,
                    PolicyType = policyType,
                    ExpirationHours = expirationHours,
                    RequireAttestation = requireAttestation,
                    Description = $"Policy for {policyType} with {expirationHours}h expiration",
                    PolicyRules = JsonSerializer.Serialize(new
                    {
                        policyType,
                        expirationHours,
                        requireAttestation,
                        createdAt = DateTime.UtcNow
                    })
                };

                await _context.SealingPolicies.AddAsync(policy);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Created policy {Name} of type {PolicyType}", name, policyType);
                
                return policy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create policy {Name}", name);
                throw;
            }
        }

        private async Task<SealingPolicy> GetOrCreatePolicyAsync(string policyType, int expirationHours)
        {
            var policy = await GetPolicyAsync(policyType);
            
            if (policy == null)
            {
                policy = await CreatePolicyAsync(
                    $"Default_{policyType}",
                    policyType,
                    expirationHours,
                    requireAttestation: policyType == "MRENCLAVE");
            }
            
            return policy;
        }

        private string GenerateStorageId(string key, string serviceName)
        {
            var input = $"{serviceName}_{key}_{DateTime.UtcNow.Ticks}";
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hash).Replace("/", "_").Replace("+", "-").Substring(0, 32);
        }

        private string ComputeFingerprint(byte[] data)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(data);
            return Convert.ToBase64String(hash);
        }
    }
}