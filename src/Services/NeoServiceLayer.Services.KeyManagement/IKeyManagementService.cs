using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.KeyManagement;

/// <summary>
/// Interface for the Key Management service.
/// </summary>
public interface IKeyManagementService : IEnclaveService, IBlockchainService
{
    /// <summary>
    /// Creates a new key.
    /// </summary>
    /// <param name="keyId">The key ID.</param>
    /// <param name="keyType">The key type (e.g., "Secp256k1", "Ed25519", "RSA").</param>
    /// <param name="keyUsage">The key usage (e.g., "Sign,Verify", "Encrypt,Decrypt").</param>
    /// <param name="exportable">Whether the key is exportable.</param>
    /// <param name="description">The key description.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The key metadata.</returns>
    Task<KeyMetadata> CreateKeyAsync(string keyId, string keyType, string keyUsage, bool exportable, string description, BlockchainType blockchainType);

    /// <summary>
    /// Gets key metadata.
    /// </summary>
    /// <param name="keyId">The key ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The key metadata.</returns>
    Task<KeyMetadata> GetKeyMetadataAsync(string keyId, BlockchainType blockchainType);

    /// <summary>
    /// Lists keys.
    /// </summary>
    /// <param name="skip">The number of keys to skip.</param>
    /// <param name="take">The number of keys to take.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The list of key metadata.</returns>
    Task<IEnumerable<KeyMetadata>> ListKeysAsync(int skip, int take, BlockchainType blockchainType);

    /// <summary>
    /// Signs data using a key.
    /// </summary>
    /// <param name="keyId">The key ID.</param>
    /// <param name="dataHex">The data to sign, in hexadecimal format.</param>
    /// <param name="signingAlgorithm">The signing algorithm.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The signature in hexadecimal format.</returns>
    Task<string> SignDataAsync(string keyId, string dataHex, string signingAlgorithm, BlockchainType blockchainType);

    /// <summary>
    /// Verifies a signature.
    /// </summary>
    /// <param name="keyIdOrPublicKeyHex">The key ID or public key in hexadecimal format.</param>
    /// <param name="dataHex">The data that was signed, in hexadecimal format.</param>
    /// <param name="signatureHex">The signature to verify, in hexadecimal format.</param>
    /// <param name="signingAlgorithm">The signing algorithm.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the signature is valid, false otherwise.</returns>
    Task<bool> VerifySignatureAsync(string keyIdOrPublicKeyHex, string dataHex, string signatureHex, string signingAlgorithm, BlockchainType blockchainType);

    /// <summary>
    /// Encrypts data using a key.
    /// </summary>
    /// <param name="keyIdOrPublicKeyHex">The key ID or public key in hexadecimal format.</param>
    /// <param name="dataHex">The data to encrypt, in hexadecimal format.</param>
    /// <param name="encryptionAlgorithm">The encryption algorithm.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The encrypted data in hexadecimal format.</returns>
    Task<string> EncryptDataAsync(string keyIdOrPublicKeyHex, string dataHex, string encryptionAlgorithm, BlockchainType blockchainType);

    /// <summary>
    /// Decrypts data using a key.
    /// </summary>
    /// <param name="keyId">The key ID.</param>
    /// <param name="encryptedDataHex">The encrypted data, in hexadecimal format.</param>
    /// <param name="encryptionAlgorithm">The encryption algorithm.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The decrypted data in hexadecimal format.</returns>
    Task<string> DecryptDataAsync(string keyId, string encryptedDataHex, string encryptionAlgorithm, BlockchainType blockchainType);

    /// <summary>
    /// Deletes a key.
    /// </summary>
    /// <param name="keyId">The key ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the key was deleted, false otherwise.</returns>
    Task<bool> DeleteKeyAsync(string keyId, BlockchainType blockchainType);
}

/// <summary>
/// Key metadata.
/// </summary>
public class KeyMetadata
{
    /// <summary>
    /// Gets or sets the key ID.
    /// </summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the key type.
    /// </summary>
    public string KeyType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the key usage.
    /// </summary>
    public string KeyUsage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the key is exportable.
    /// </summary>
    public bool Exportable { get; set; }

    /// <summary>
    /// Gets or sets the key description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last used date.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Gets or sets the public key in hexadecimal format.
    /// </summary>
    public string? PublicKeyHex { get; set; }
}
