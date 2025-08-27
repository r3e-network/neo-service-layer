using Neo.SecretsManagement.Service.Models;

namespace Neo.SecretsManagement.Service.Services;

public interface IKeyManagementService
{
    Task<EncryptionKey> GenerateKeyAsync(GenerateKeyRequest request, string userId);
    Task<EncryptionKey?> GetKeyAsync(string keyId);
    Task<List<EncryptionKey>> ListKeysAsync(KeyStatus? status = null);
    Task<bool> RotateKeyAsync(Guid keyId, string userId);
    Task<bool> RevokeKeyAsync(Guid keyId, string userId);
    Task<bool> UpdateKeyStatusAsync(Guid keyId, KeyStatus status, string userId);
    Task<KeyStatistics> GetKeyStatisticsAsync();
    Task<List<EncryptionKey>> GetKeysRequiringRotationAsync();
    Task EnsureDefaultKeysAsync();
    Task<bool> ValidateKeyAsync(string keyId);
    Task<string> ExportKeyAsync(string keyId, string format);
    Task<EncryptionKey> ImportKeyAsync(string keyData, string format, string userId);
}