using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Tee.Host.Storage.PersistentStorage;
using NeoServiceLayer.Tee.Host.Storage.PersistentStorage.Compression;
using NeoServiceLayer.Tee.Host.Storage.PersistentStorage.Encryption;
using Xunit;

namespace NeoServiceLayer.Tee.Host.Tests.Storage.PersistentStorage
{
    public class EncryptionCompressionIntegrationTests : IDisposable
    {
        private readonly Mock<ILoggerFactory> _loggerFactoryMock;
        private readonly Mock<ILogger<AesEncryptionProvider>> _aesLoggerMock;
        private readonly Mock<ILogger<GZipCompressionProvider>> _gzipLoggerMock;
        private readonly Mock<ILogger<OcclumFileStorageProvider>> _storageLoggerMock;
        private readonly string _testDirectory;
        private readonly string _keyFile;
        private readonly string _storageDirectory;

        public EncryptionCompressionIntegrationTests()
        {
            _aesLoggerMock = new Mock<ILogger<AesEncryptionProvider>>();
            _gzipLoggerMock = new Mock<ILogger<GZipCompressionProvider>>();
            _storageLoggerMock = new Mock<ILogger<OcclumFileStorageProvider>>();
            
            _loggerFactoryMock = new Mock<ILoggerFactory>();
            _loggerFactoryMock.Setup(f => f.CreateLogger(It.Is<string>(s => s == typeof(AesEncryptionProvider).FullName)))
                .Returns(_aesLoggerMock.Object);
            _loggerFactoryMock.Setup(f => f.CreateLogger(It.Is<string>(s => s == typeof(GZipCompressionProvider).FullName)))
                .Returns(_gzipLoggerMock.Object);
            _loggerFactoryMock.Setup(f => f.CreateLogger(It.Is<string>(s => s == typeof(OcclumFileStorageProvider).FullName)))
                .Returns(_storageLoggerMock.Object);
            
            _testDirectory = Path.Combine(Path.GetTempPath(), $"encryption_compression_test_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
            _keyFile = Path.Combine(_testDirectory, "aes_key.bin");
            _storageDirectory = Path.Combine(_testDirectory, "storage");
        }

        public void Dispose()
        {
            // Clean up the test directory
            if (Directory.Exists(_testDirectory))
            {
                try
                {
                    Directory.Delete(_testDirectory, true);
                }
                catch
                {
                    // Ignore errors during cleanup
                }
            }
        }

        [Fact]
        public async Task EncryptAndCompress_ThenStore_ThenRetrieveAndDecompressAndDecrypt_RoundTrip()
        {
            // Arrange
            var encryptionOptions = new AesEncryptionOptions { KeyFile = _keyFile };
            var compressionOptions = new GZipCompressionOptions { CompressionLevel = 2 };
            var storageOptions = new OcclumFileStorageOptions { StorageDirectory = _storageDirectory };
            
            var encryptionProvider = new AesEncryptionProvider(_aesLoggerMock.Object, encryptionOptions);
            var compressionProvider = new GZipCompressionProvider(_gzipLoggerMock.Object, compressionOptions);
            var storageProvider = new OcclumFileStorageProvider(_storageLoggerMock.Object, storageOptions);
            
            await encryptionProvider.InitializeAsync();
            await compressionProvider.InitializeAsync();
            await storageProvider.InitializeAsync();
            
            string key = "test_key";
            byte[] originalData = Encoding.UTF8.GetBytes(new string('a', 10000)); // 10KB of repeating data

            // Act - Compress, Encrypt, and Store
            byte[] compressedData = await compressionProvider.CompressAsync(originalData);
            byte[] encryptedCompressedData = await encryptionProvider.EncryptAsync(compressedData);
            await storageProvider.WriteAsync(key, encryptedCompressedData);
            
            // Act - Retrieve, Decrypt, and Decompress
            byte[] retrievedEncryptedCompressedData = await storageProvider.ReadAsync(key);
            byte[] retrievedCompressedData = await encryptionProvider.DecryptAsync(retrievedEncryptedCompressedData);
            byte[] retrievedData = await compressionProvider.DecompressAsync(retrievedCompressedData);

            // Assert
            Assert.NotNull(retrievedEncryptedCompressedData);
            Assert.NotEqual(originalData, compressedData); // Compression should change the data
            Assert.NotEqual(compressedData, encryptedCompressedData); // Encryption should change the data
            Assert.Equal(encryptedCompressedData, retrievedEncryptedCompressedData); // Storage should preserve the data
            Assert.Equal(compressedData, retrievedCompressedData); // Decryption should restore the compressed data
            Assert.Equal(originalData, retrievedData); // Decompression should restore the original data
            
            // Verify compression actually reduced the size
            Assert.True(compressedData.Length < originalData.Length);
        }

        [Fact]
        public async Task EncryptAndCompress_WithDifferentProviders_RoundTrip()
        {
            // Arrange
            var aesOptions = new AesEncryptionOptions { KeyFile = Path.Combine(_testDirectory, "aes_key.bin") };
            var chaCha20Options = new ChaCha20EncryptionOptions { KeyFile = Path.Combine(_testDirectory, "chacha20_key.bin") };
            var gzipOptions = new GZipCompressionOptions { CompressionLevel = 2 };
            var brotliOptions = new BrotliCompressionOptions { CompressionLevel = 4 };
            
            var aesProvider = new AesEncryptionProvider(_aesLoggerMock.Object, aesOptions);
            var chaCha20Provider = new ChaCha20EncryptionProvider(new Mock<ILogger<ChaCha20EncryptionProvider>>().Object, chaCha20Options);
            var gzipProvider = new GZipCompressionProvider(_gzipLoggerMock.Object, gzipOptions);
            var brotliProvider = new BrotliCompressionProvider(new Mock<ILogger<BrotliCompressionProvider>>().Object, brotliOptions);
            
            await aesProvider.InitializeAsync();
            await chaCha20Provider.InitializeAsync();
            await gzipProvider.InitializeAsync();
            await brotliProvider.InitializeAsync();
            
            byte[] originalData = Encoding.UTF8.GetBytes(new string('a', 10000)); // 10KB of repeating data

            // Act - Test all combinations
            byte[] gzipCompressedData = await gzipProvider.CompressAsync(originalData);
            byte[] brotliCompressedData = await brotliProvider.CompressAsync(originalData);
            
            byte[] aesEncryptedGzipData = await aesProvider.EncryptAsync(gzipCompressedData);
            byte[] aesEncryptedBrotliData = await aesProvider.EncryptAsync(brotliCompressedData);
            byte[] chaCha20EncryptedGzipData = await chaCha20Provider.EncryptAsync(gzipCompressedData);
            byte[] chaCha20EncryptedBrotliData = await chaCha20Provider.EncryptAsync(brotliCompressedData);
            
            // Decrypt and decompress
            byte[] decryptedGzipData1 = await aesProvider.DecryptAsync(aesEncryptedGzipData);
            byte[] decryptedBrotliData1 = await aesProvider.DecryptAsync(aesEncryptedBrotliData);
            byte[] decryptedGzipData2 = await chaCha20Provider.DecryptAsync(chaCha20EncryptedGzipData);
            byte[] decryptedBrotliData2 = await chaCha20Provider.DecryptAsync(chaCha20EncryptedBrotliData);
            
            byte[] decompressedData1 = await gzipProvider.DecompressAsync(decryptedGzipData1);
            byte[] decompressedData2 = await brotliProvider.DecompressAsync(decryptedBrotliData1);
            byte[] decompressedData3 = await gzipProvider.DecompressAsync(decryptedGzipData2);
            byte[] decompressedData4 = await brotliProvider.DecompressAsync(decryptedBrotliData2);

            // Assert
            Assert.Equal(originalData, decompressedData1);
            Assert.Equal(originalData, decompressedData2);
            Assert.Equal(originalData, decompressedData3);
            Assert.Equal(originalData, decompressedData4);
            
            // Verify Brotli compresses better than GZip for this data
            Assert.True(brotliCompressedData.Length <= gzipCompressedData.Length);
        }

        [Fact]
        public async Task StorageProvider_WithEncryptionAndCompression_RoundTrip()
        {
            // Arrange
            var encryptionOptions = new AesEncryptionOptions { KeyFile = _keyFile };
            var compressionOptions = new GZipCompressionOptions { CompressionLevel = 2 };
            var storageOptions = new OcclumFileStorageOptions { StorageDirectory = _storageDirectory };
            
            var encryptionProvider = new AesEncryptionProvider(_aesLoggerMock.Object, encryptionOptions);
            var compressionProvider = new GZipCompressionProvider(_gzipLoggerMock.Object, compressionOptions);
            var storageProvider = new OcclumFileStorageProvider(_storageLoggerMock.Object, storageOptions);
            
            await encryptionProvider.InitializeAsync();
            await compressionProvider.InitializeAsync();
            await storageProvider.InitializeAsync();
            
            string key = "test_key";
            byte[] originalData = Encoding.UTF8.GetBytes(new string('a', 10000)); // 10KB of repeating data

            // Create a custom metadata with compression and encryption info
            var metadata = new StorageMetadata
            {
                Key = key,
                Size = originalData.Length,
                CreationTime = DateTime.UtcNow,
                LastModifiedTime = DateTime.UtcNow,
                LastAccessTime = DateTime.UtcNow,
                IsCompressed = true,
                IsEncrypted = true,
                CompressionAlgorithm = "GZip",
                EncryptionAlgorithm = "AES-GCM"
            };

            // Act - Compress, Encrypt, and Store
            byte[] compressedData = await compressionProvider.CompressAsync(originalData);
            byte[] encryptedCompressedData = await encryptionProvider.EncryptAsync(compressedData);
            
            // Store with metadata
            await storageProvider.WriteAsync(key, encryptedCompressedData);
            await storageProvider.UpdateMetadataAsync(key, metadata);
            
            // Retrieve metadata and data
            var retrievedMetadata = await storageProvider.GetMetadataAsync(key);
            byte[] retrievedEncryptedCompressedData = await storageProvider.ReadAsync(key);
            
            // Process based on metadata
            byte[] retrievedData;
            if (retrievedMetadata.IsEncrypted && retrievedMetadata.IsCompressed)
            {
                byte[] retrievedCompressedData = await encryptionProvider.DecryptAsync(retrievedEncryptedCompressedData);
                retrievedData = await compressionProvider.DecompressAsync(retrievedCompressedData);
            }
            else if (retrievedMetadata.IsEncrypted)
            {
                retrievedData = await encryptionProvider.DecryptAsync(retrievedEncryptedCompressedData);
            }
            else if (retrievedMetadata.IsCompressed)
            {
                retrievedData = await compressionProvider.DecompressAsync(retrievedEncryptedCompressedData);
            }
            else
            {
                retrievedData = retrievedEncryptedCompressedData;
            }

            // Assert
            Assert.Equal(originalData, retrievedData);
            Assert.Equal("GZip", retrievedMetadata.CompressionAlgorithm);
            Assert.Equal("AES-GCM", retrievedMetadata.EncryptionAlgorithm);
            Assert.True(retrievedMetadata.IsCompressed);
            Assert.True(retrievedMetadata.IsEncrypted);
        }
    }
}
