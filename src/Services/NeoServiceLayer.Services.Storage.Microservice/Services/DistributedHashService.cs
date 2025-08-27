using Microsoft.EntityFrameworkCore;
using Neo.Storage.Service.Data;
using Neo.Storage.Service.Services;
using System.Security.Cryptography;
using System.Text;

namespace Neo.Storage.Service.Services;

public class DistributedHashService : IDistributedHashService
{
    private readonly StorageDbContext _context;
    private readonly ILogger<DistributedHashService> _logger;

    public DistributedHashService(
        StorageDbContext context,
        ILogger<DistributedHashService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> CalculateHashAsync(Stream content)
    {
        try
        {
            using var sha256 = SHA256.Create();
            content.Position = 0; // Ensure we read from the beginning
            var hashBytes = await Task.Run(() => sha256.ComputeHash(content));
            content.Position = 0; // Reset position for subsequent reads
            
            var hash = Convert.ToHexString(hashBytes);
            _logger.LogDebug("Calculated hash for stream: {Hash}", hash);
            
            return hash;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate hash for stream");
            throw;
        }
    }

    public async Task<string> CalculateHashAsync(byte[] content)
    {
        try
        {
            using var sha256 = SHA256.Create();
            var hashBytes = await Task.Run(() => sha256.ComputeHash(content));
            
            var hash = Convert.ToHexString(hashBytes);
            _logger.LogDebug("Calculated hash for byte array of length {Length}: {Hash}", content.Length, hash);
            
            return hash;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate hash for byte array");
            throw;
        }
    }

    public async Task<bool> VerifyHashAsync(Stream content, string expectedHash)
    {
        try
        {
            var calculatedHash = await CalculateHashAsync(content);
            var isValid = string.Equals(calculatedHash, expectedHash, StringComparison.OrdinalIgnoreCase);
            
            _logger.LogDebug("Hash verification: expected={ExpectedHash}, calculated={CalculatedHash}, valid={IsValid}", 
                expectedHash, calculatedHash, isValid);
            
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify hash for stream");
            return false;
        }
    }

    public async Task<bool> VerifyReplicaHashAsync(Guid replicaId)
    {
        try
        {
            var replica = await _context.StorageReplicas
                .Include(r => r.Object)
                .Include(r => r.Node)
                .FirstOrDefaultAsync(r => r.Id == replicaId);

            if (replica == null)
            {
                _logger.LogWarning("Replica not found for verification: {ReplicaId}", replicaId);
                return false;
            }

            if (replica.Node.Status != Models.NodeStatus.Active)
            {
                _logger.LogWarning("Cannot verify replica on inactive node: {NodeId}", replica.Node.Id);
                return false;
            }

            // In a real implementation, this would read the actual file from the storage node
            // and calculate its hash. For now, we'll simulate the verification.
            var isValid = await SimulateReplicaHashVerificationAsync(replica);

            if (!isValid)
            {
                _logger.LogError("Hash verification failed for replica {ReplicaId} on node {NodeId}", 
                    replicaId, replica.Node.Id);
                
                // Mark replica as corrupted
                replica.Status = Models.ReplicaStatus.Failed;
                await _context.SaveChangesAsync();
            }
            else
            {
                // Update last verified timestamp
                replica.LastVerified = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify replica hash: {ReplicaId}", replicaId);
            return false;
        }
    }

    public async Task<string> GenerateEtagAsync(string hash, DateTime lastModified)
    {
        try
        {
            // Generate ETag using hash and last modified time
            var etagData = $"{hash}-{lastModified.Ticks}";
            using var md5 = MD5.Create();
            var etagBytes = await Task.Run(() => md5.ComputeHash(Encoding.UTF8.GetBytes(etagData)));
            
            var etag = $"\"{Convert.ToHexString(etagBytes)}\"";
            _logger.LogDebug("Generated ETag: {ETag} for hash {Hash}", etag, hash);
            
            return etag;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate ETag");
            throw;
        }
    }

    public async Task<string> GenerateVersionIdAsync()
    {
        try
        {
            // Generate a unique version ID using timestamp and random data
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var randomBytes = new byte[8];
            
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            
            var versionId = $"{timestamp:X}-{Convert.ToHexString(randomBytes)}";
            _logger.LogDebug("Generated version ID: {VersionId}", versionId);
            
            await Task.CompletedTask; // Maintain async signature
            return versionId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate version ID");
            throw;
        }
    }

    public async Task<bool> CompareHashesAsync(string hash1, string hash2)
    {
        await Task.CompletedTask; // Maintain async signature
        
        if (string.IsNullOrEmpty(hash1) || string.IsNullOrEmpty(hash2))
        {
            return false;
        }

        return string.Equals(hash1, hash2, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> SimulateReplicaHashVerificationAsync(Models.StorageReplica replica)
    {
        try
        {
            // In a real implementation, this would:
            // 1. Make HTTP request to the storage node
            // 2. Request the file hash or download the file
            // 3. Calculate the hash and compare with expected
            // 4. Handle network errors and node failures

            // For simulation, we'll assume 95% of replicas are valid
            await Task.Delay(100); // Simulate network delay

            var random = new Random();
            var isValid = random.NextDouble() > 0.05; // 95% success rate

            // Update replica hash if verification succeeded
            if (isValid && string.IsNullOrEmpty(replica.Hash))
            {
                replica.Hash = replica.Object.Hash;
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify replica hash for replica {ReplicaId}", replica.Id);
            return false;
        }
    }
}