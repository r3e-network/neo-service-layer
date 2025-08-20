using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NeoServiceLayer.Demos
{
    /// <summary>
    /// Production-ready security demonstration with proper key derivation and encryption.
    /// </summary>
    public class SecurityDemo
    {
        /// <summary>
        /// Demonstrates secure PBKDF2 key derivation with proper parameters.
        /// Uses production-grade security standards.
        /// </summary>
        public static byte[] DeriveKey(string password, byte[] salt, int iterations = 100000)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));
            
            if (salt == null || salt.Length < 16)
                throw new ArgumentException("Salt must be at least 16 bytes", nameof(salt));
            
            if (iterations < 10000)
                throw new ArgumentException("Iterations must be at least 10000 for security", nameof(iterations));
            
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(32); // 256-bit key
        }
        
        /// <summary>
        /// Demonstrates secure AES-GCM encryption for production use.
        /// </summary>
        public static (byte[] ciphertext, byte[] nonce, byte[] tag) EncryptAesGcm(byte[] plaintext, byte[] key)
        {
            if (plaintext == null || plaintext.Length == 0)
                throw new ArgumentException("Plaintext cannot be null or empty", nameof(plaintext));
            
            if (key == null || key.Length != 32)
                throw new ArgumentException("Key must be 32 bytes (256-bit)", nameof(key));
            
            var nonce = new byte[12]; // 96-bit nonce for GCM
            var tag = new byte[16];   // 128-bit authentication tag
            var ciphertext = new byte[plaintext.Length];
            
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(nonce);
            
            using var aes = new AesGcm(key);
            aes.Encrypt(nonce, plaintext, ciphertext, tag);
            
            return (ciphertext, nonce, tag);
        }
        
        /// <summary>
        /// Demonstrates secure AES-GCM decryption for production use.
        /// </summary>
        public static byte[] DecryptAesGcm(byte[] ciphertext, byte[] key, byte[] nonce, byte[] tag)
        {
            if (ciphertext == null || ciphertext.Length == 0)
                throw new ArgumentException("Ciphertext cannot be null or empty", nameof(ciphertext));
            
            if (key == null || key.Length != 32)
                throw new ArgumentException("Key must be 32 bytes (256-bit)", nameof(key));
            
            if (nonce == null || nonce.Length != 12)
                throw new ArgumentException("Nonce must be 12 bytes", nameof(nonce));
            
            if (tag == null || tag.Length != 16)
                throw new ArgumentException("Tag must be 16 bytes", nameof(tag));
            
            var plaintext = new byte[ciphertext.Length];
            
            using var aes = new AesGcm(key);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
            
            return plaintext;
        }
        
        /// <summary>
        /// Generates cryptographically secure random salt for key derivation.
        /// </summary>
        public static byte[] GenerateSalt(int length = 32)
        {
            if (length < 16)
                throw new ArgumentException("Salt length must be at least 16 bytes", nameof(length));
            
            var salt = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);
            return salt;
        }
    }
}







