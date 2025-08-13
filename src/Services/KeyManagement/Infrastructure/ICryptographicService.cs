using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Services.KeyManagement.Infrastructure
{
    /// <summary>
    /// Interface for cryptographic operations
    /// </summary>
    public interface ICryptographicService
    {
        /// <summary>
        /// Generates a new cryptographic key
        /// </summary>
        Task<KeyGenerationResult> GenerateKeyAsync(
            string algorithm,
            int keySize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Signs data with a private key
        /// </summary>
        Task<string> SignDataAsync(
            string keyId,
            byte[] data,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifies a signature
        /// </summary>
        Task<bool> VerifySignatureAsync(
            string keyId,
            byte[] data,
            string signature,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Encrypts data with a public key
        /// </summary>
        Task<byte[]> EncryptAsync(
            string keyId,
            byte[] data,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Decrypts data with a private key
        /// </summary>
        Task<byte[]> DecryptAsync(
            string keyId,
            byte[] encryptedData,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of key generation
    /// </summary>
    public class KeyGenerationResult
    {
        public string PublicKey { get; set; } = string.Empty;
        public string? EncryptedPrivateKey { get; set; }
        public string Algorithm { get; set; } = string.Empty;
        public int KeySize { get; set; }
        public byte[]? PublicKeyBytes { get; set; }
        public byte[]? PrivateKeyBytes { get; set; }
    }
}