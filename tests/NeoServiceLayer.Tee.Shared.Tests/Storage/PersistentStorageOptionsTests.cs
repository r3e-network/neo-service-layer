using System;
using System.IO;
using NeoServiceLayer.Tee.Shared.Storage;
using Xunit;

namespace NeoServiceLayer.Tee.Shared.Tests.Storage
{
    public class PersistentStorageOptionsTests
    {
        [Fact]
        public void PersistentStorageOptions_DefaultConstructor_InitializesWithDefaultValues()
        {
            // Act
            var options = new PersistentStorageOptions();

            // Assert
            Assert.Equal(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "storage"), options.StoragePath);
            Assert.True(options.EnableEncryption);
            Assert.Null(options.EncryptionKey);
            Assert.True(options.EnableCompression);
            Assert.Equal(6, options.CompressionLevel);
            Assert.Equal(4 * 1024 * 1024, options.MaxChunkSize);
            Assert.True(options.CreateIfNotExists);
            Assert.True(options.EnableAutoFlush);
            Assert.Equal(5000, options.AutoFlushIntervalMs);
            Assert.True(options.EnableCaching);
            Assert.Equal(100 * 1024 * 1024, options.CacheSizeBytes);
            Assert.True(options.EnableLogging);
            Assert.Equal("Information", options.LogLevel);
        }

        [Fact]
        public void PersistentStorageOptions_CustomValues_AreApplied()
        {
            // Arrange
            string storagePath = Path.Combine(Path.GetTempPath(), "custom_storage");
            byte[] encryptionKey = new byte[] { 1, 2, 3, 4, 5 };
            bool enableEncryption = false;
            bool enableCompression = false;
            int compressionLevel = 3;
            int maxChunkSize = 1024;
            bool createIfNotExists = false;
            bool enableAutoFlush = false;
            int autoFlushIntervalMs = 10000;
            bool enableCaching = false;
            long cacheSizeBytes = 50 * 1024 * 1024;
            bool enableLogging = false;
            string logLevel = "Debug";

            // Act
            var options = new PersistentStorageOptions
            {
                StoragePath = storagePath,
                EncryptionKey = encryptionKey,
                EnableEncryption = enableEncryption,
                EnableCompression = enableCompression,
                CompressionLevel = compressionLevel,
                MaxChunkSize = maxChunkSize,
                CreateIfNotExists = createIfNotExists,
                EnableAutoFlush = enableAutoFlush,
                AutoFlushIntervalMs = autoFlushIntervalMs,
                EnableCaching = enableCaching,
                CacheSizeBytes = cacheSizeBytes,
                EnableLogging = enableLogging,
                LogLevel = logLevel
            };

            // Assert
            Assert.Equal(storagePath, options.StoragePath);
            Assert.Equal(encryptionKey, options.EncryptionKey);
            Assert.Equal(enableEncryption, options.EnableEncryption);
            Assert.Equal(enableCompression, options.EnableCompression);
            Assert.Equal(compressionLevel, options.CompressionLevel);
            Assert.Equal(maxChunkSize, options.MaxChunkSize);
            Assert.Equal(createIfNotExists, options.CreateIfNotExists);
            Assert.Equal(enableAutoFlush, options.EnableAutoFlush);
            Assert.Equal(autoFlushIntervalMs, options.AutoFlushIntervalMs);
            Assert.Equal(enableCaching, options.EnableCaching);
            Assert.Equal(cacheSizeBytes, options.CacheSizeBytes);
            Assert.Equal(enableLogging, options.EnableLogging);
            Assert.Equal(logLevel, options.LogLevel);
        }
    }
}
