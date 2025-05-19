using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Shared.Interfaces;

namespace NeoServiceLayer.Tee.Host.Storage.PersistentStorage
{
    /// <summary>
    /// Utility class for storage operations.
    /// </summary>
    public class StorageUtility
    {
        private readonly ILogger<StorageUtility> _logger;
        private readonly IOcclumInterface _occlumInterface;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageUtility"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for logging information and errors.</param>
        /// <param name="occlumInterface">The Occlum interface to use for sealing and unsealing data.</param>
        public StorageUtility(ILogger<StorageUtility> logger, IOcclumInterface occlumInterface)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _occlumInterface = occlumInterface ?? throw new ArgumentNullException(nameof(occlumInterface));
        }

        /// <summary>
        /// Encrypts data using the enclave's sealing functionality.
        /// </summary>
        /// <param name="data">The data to encrypt.</param>
        /// <returns>The encrypted data.</returns>
        public byte[] Encrypt(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _logger.LogDebug("Encrypting data ({Size} bytes)", data.Length);

            try
            {
                // Use the enclave's sealing functionality to encrypt the data
                byte[] encryptedData = _occlumInterface.SealData(data);
                _logger.LogDebug("Data encrypted successfully ({EncryptedSize} bytes)", encryptedData.Length);
                return encryptedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to encrypt data");
                throw new StorageException("Failed to encrypt data", ex);
            }
        }

        /// <summary>
        /// Decrypts data using the enclave's unsealing functionality.
        /// </summary>
        /// <param name="encryptedData">The encrypted data to decrypt.</param>
        /// <returns>The decrypted data.</returns>
        public byte[] Decrypt(byte[] encryptedData)
        {
            if (encryptedData == null)
            {
                throw new ArgumentNullException(nameof(encryptedData));
            }

            _logger.LogDebug("Decrypting data ({Size} bytes)", encryptedData.Length);

            try
            {
                // Use the enclave's unsealing functionality to decrypt the data
                byte[] decryptedData = _occlumInterface.UnsealData(encryptedData);
                _logger.LogDebug("Data decrypted successfully ({DecryptedSize} bytes)", decryptedData.Length);
                return decryptedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt data");
                throw new StorageException("Failed to decrypt data", ex);
            }
        }

        /// <summary>
        /// Compresses data using GZip compression.
        /// </summary>
        /// <param name="data">The data to compress.</param>
        /// <returns>The compressed data.</returns>
        public byte[] Compress(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _logger.LogDebug("Compressing data ({Size} bytes)", data.Length);

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal))
                    {
                        gzipStream.Write(data, 0, data.Length);
                    }

                    byte[] compressedData = memoryStream.ToArray();
                    _logger.LogDebug("Data compressed successfully ({CompressedSize} bytes)", compressedData.Length);
                    return compressedData;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compress data");
                throw new StorageException("Failed to compress data", ex);
            }
        }

        /// <summary>
        /// Decompresses data using GZip decompression.
        /// </summary>
        /// <param name="compressedData">The compressed data to decompress.</param>
        /// <returns>The decompressed data.</returns>
        public byte[] Decompress(byte[] compressedData)
        {
            if (compressedData == null)
            {
                throw new ArgumentNullException(nameof(compressedData));
            }

            _logger.LogDebug("Decompressing data ({Size} bytes)", compressedData.Length);

            try
            {
                using (var compressedStream = new MemoryStream(compressedData))
                using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                using (var resultStream = new MemoryStream())
                {
                    gzipStream.CopyTo(resultStream);
                    byte[] decompressedData = resultStream.ToArray();
                    _logger.LogDebug("Data decompressed successfully ({DecompressedSize} bytes)", decompressedData.Length);
                    return decompressedData;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decompress data");
                throw new StorageException("Failed to decompress data", ex);
            }
        }

        /// <summary>
        /// Computes a hash for data.
        /// </summary>
        /// <param name="data">The data to hash.</param>
        /// <returns>The hash as a string.</returns>
        public string ComputeHash(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _logger.LogDebug("Computing hash for data ({Size} bytes)", data.Length);

            try
            {
                using (var sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(data);
                    string hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                    _logger.LogDebug("Hash computed successfully: {Hash}", hash);
                    return hash;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compute hash for data");
                throw new StorageException("Failed to compute hash for data", ex);
            }
        }

        /// <summary>
        /// Encrypts and compresses data.
        /// </summary>
        /// <param name="data">The data to encrypt and compress.</param>
        /// <returns>The encrypted and compressed data.</returns>
        public byte[] EncryptAndCompress(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _logger.LogDebug("Encrypting and compressing data ({Size} bytes)", data.Length);

            try
            {
                // Compress the data first
                byte[] compressedData = Compress(data);

                // Then encrypt the compressed data
                byte[] encryptedData = Encrypt(compressedData);

                _logger.LogDebug("Data encrypted and compressed successfully ({FinalSize} bytes)", encryptedData.Length);
                return encryptedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to encrypt and compress data");
                throw new StorageException("Failed to encrypt and compress data", ex);
            }
        }

        /// <summary>
        /// Decrypts and decompresses data.
        /// </summary>
        /// <param name="encryptedCompressedData">The encrypted and compressed data to decrypt and decompress.</param>
        /// <returns>The decrypted and decompressed data.</returns>
        public byte[] DecryptAndDecompress(byte[] encryptedCompressedData)
        {
            if (encryptedCompressedData == null)
            {
                throw new ArgumentNullException(nameof(encryptedCompressedData));
            }

            _logger.LogDebug("Decrypting and decompressing data ({Size} bytes)", encryptedCompressedData.Length);

            try
            {
                // Decrypt the data first
                byte[] decryptedData = Decrypt(encryptedCompressedData);

                // Then decompress the decrypted data
                byte[] decompressedData = Decompress(decryptedData);

                _logger.LogDebug("Data decrypted and decompressed successfully ({FinalSize} bytes)", decompressedData.Length);
                return decompressedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt and decompress data");
                throw new StorageException("Failed to decrypt and decompress data", ex);
            }
        }
    }
}
