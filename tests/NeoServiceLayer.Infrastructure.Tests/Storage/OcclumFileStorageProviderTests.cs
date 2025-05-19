using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Infrastructure.Storage;
using Xunit;

namespace NeoServiceLayer.Infrastructure.Tests.Storage
{
    /// <summary>
    /// Tests for the OcclumFileStorageProvider class.
    /// </summary>
    [Trait("Category", "Storage")]
    public class OcclumFileStorageProviderTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OcclumFileStorageProviderTests> _logger;
        private readonly IPersistentStorageProvider _storageProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumFileStorageProviderTests"/> class.
        /// </summary>
        public OcclumFileStorageProviderTests()
        {
            // Create service collection
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Add storage options
            services.Configure<StorageOptions>(options =>
            {
                options.Provider = "OcclumFileStorage";
                options.Encryption = new EncryptionOptions { Enabled = true, KeySize = 256, Algorithm = "AES-GCM" };
                options.Compression = new CompressionOptions { Enabled = true, Level = "Optimal", Algorithm = "Deflate" };
                options.Chunking = new ChunkingOptions { Enabled = true, ChunkSize = 1048576 };
                options.Transaction = new TransactionOptions { Enabled = true, IsolationLevel = "ReadCommitted" };
            });

            // Add storage provider
            services.AddSingleton<IPersistentStorageProvider, OcclumFileStorageProvider>();

            // Build service provider
            _serviceProvider = services.BuildServiceProvider();

            // Get logger
            _logger = _serviceProvider.GetRequiredService<ILogger<OcclumFileStorageProviderTests>>();

            // Get storage provider
            _storageProvider = _serviceProvider.GetRequiredService<IPersistentStorageProvider>();

            // Set simulation mode
            Environment.SetEnvironmentVariable("OCCLUM_SIMULATION", "1");
        }

        /// <summary>
        /// Tests that the storage provider can be initialized.
        /// </summary>
        [Fact]
        public async Task InitializeAsync_ShouldSucceed()
        {
            // Act
            await _storageProvider.InitializeAsync();

            // Assert
            // No exception means success
        }

        /// <summary>
        /// Tests that data can be stored and retrieved.
        /// </summary>
        [Fact]
        public async Task StoreAndRetrieveAsync_ShouldSucceed()
        {
            // Arrange
            await _storageProvider.InitializeAsync();
            string key = $"test-key-{Guid.NewGuid()}";
            byte[] data = Encoding.UTF8.GetBytes("Hello, world!");

            try
            {
                // Act
                await _storageProvider.StoreAsync(key, data);
                byte[] retrievedData = await _storageProvider.RetrieveAsync(key);

                // Assert
                Assert.NotNull(retrievedData);
                Assert.Equal(data, retrievedData);
            }
            finally
            {
                // Clean up
                await _storageProvider.DeleteAsync(key);
            }
        }

        /// <summary>
        /// Tests that data can be deleted.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_ShouldSucceed()
        {
            // Arrange
            await _storageProvider.InitializeAsync();
            string key = $"test-key-{Guid.NewGuid()}";
            byte[] data = Encoding.UTF8.GetBytes("Hello, world!");
            await _storageProvider.StoreAsync(key, data);

            // Act
            await _storageProvider.DeleteAsync(key);
            byte[] retrievedData = await _storageProvider.RetrieveAsync(key);

            // Assert
            Assert.Null(retrievedData);
        }

        /// <summary>
        /// Tests that existence of a key can be checked.
        /// </summary>
        [Fact]
        public async Task ExistsAsync_ShouldSucceed()
        {
            // Arrange
            await _storageProvider.InitializeAsync();
            string key = $"test-key-{Guid.NewGuid()}";
            byte[] data = Encoding.UTF8.GetBytes("Hello, world!");

            try
            {
                // Act
                bool existsBefore = await _storageProvider.ExistsAsync(key);
                await _storageProvider.StoreAsync(key, data);
                bool existsAfter = await _storageProvider.ExistsAsync(key);

                // Assert
                Assert.False(existsBefore);
                Assert.True(existsAfter);
            }
            finally
            {
                // Clean up
                await _storageProvider.DeleteAsync(key);
            }
        }

        /// <summary>
        /// Tests that all keys can be listed.
        /// </summary>
        [Fact]
        public async Task ListKeysAsync_ShouldSucceed()
        {
            // Arrange
            await _storageProvider.InitializeAsync();
            string key1 = $"test-key-1-{Guid.NewGuid()}";
            string key2 = $"test-key-2-{Guid.NewGuid()}";
            byte[] data = Encoding.UTF8.GetBytes("Hello, world!");

            try
            {
                // Act
                await _storageProvider.StoreAsync(key1, data);
                await _storageProvider.StoreAsync(key2, data);
                string[] keys = await _storageProvider.ListKeysAsync();

                // Assert
                Assert.NotNull(keys);
                Assert.Contains(key1, keys);
                Assert.Contains(key2, keys);
            }
            finally
            {
                // Clean up
                await _storageProvider.DeleteAsync(key1);
                await _storageProvider.DeleteAsync(key2);
            }
        }
    }
}
