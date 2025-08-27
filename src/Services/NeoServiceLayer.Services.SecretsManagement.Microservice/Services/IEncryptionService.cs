namespace Neo.SecretsManagement.Service.Services;

public interface IEncryptionService
{
    Task<string> EncryptAsync(string plaintext, string keyId);
    Task<string> DecryptAsync(string ciphertext, string keyId);
    Task<string> EncryptWithDataKeyAsync(string plaintext, out string encryptedDataKey);
    Task<string> DecryptWithDataKeyAsync(string ciphertext, string encryptedDataKey);
    Task<bool> VerifyIntegrityAsync(string data, string keyId, string expectedHash);
    Task<string> GenerateSecureRandomAsync(int length);
    Task<string> HashAsync(string data, string? salt = null);
    Task<bool> VerifyHashAsync(string data, string hash, string? salt = null);
    Task<byte[]> GenerateKeyAsync(int keySize);
    Task<string> DeriveKeyAsync(string password, string salt, int keySize);
}