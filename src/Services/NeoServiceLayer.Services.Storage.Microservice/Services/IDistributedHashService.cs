using Neo.Storage.Service.Models;

namespace Neo.Storage.Service.Services;

public interface IDistributedHashService
{
    Task<string> CalculateHashAsync(Stream content);
    Task<string> CalculateHashAsync(byte[] content);
    Task<bool> VerifyHashAsync(Stream content, string expectedHash);
    Task<bool> VerifyReplicaHashAsync(Guid replicaId);
    Task<string> GenerateEtagAsync(string hash, DateTime lastModified);
    Task<string> GenerateVersionIdAsync();
    Task<bool> CompareHashesAsync(string hash1, string hash2);
}