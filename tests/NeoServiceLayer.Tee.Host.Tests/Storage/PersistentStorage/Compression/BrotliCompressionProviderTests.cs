using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Tee.Host.Storage.PersistentStorage;
using NeoServiceLayer.Tee.Host.Storage.PersistentStorage.Compression;
using Xunit;

namespace NeoServiceLayer.Tee.Host.Tests.Storage.PersistentStorage.Compression
{
    public class BrotliCompressionProviderTests : IDisposable
    {
        private readonly Mock<ILogger<BrotliCompressionProvider>> _loggerMock;
        private readonly BrotliCompressionOptions _options;
        private readonly BrotliCompressionProvider _provider;

        public BrotliCompressionProviderTests()
        {
            _loggerMock = new Mock<ILogger<BrotliCompressionProvider>>();
            _options = new BrotliCompressionOptions
            {
                CompressionLevel = 4 // Default
            };
            _provider = new BrotliCompressionProvider(_loggerMock.Object, _options);
        }

        public void Dispose()
        {
            _provider.Dispose();
        }

        [Fact]
        public async Task InitializeAsync_Succeeds()
        {
            // Act & Assert
            await _provider.InitializeAsync(); // Should not throw
        }

        [Fact]
        public async Task CompressAsync_DecompressAsync_RoundTrip()
        {
            // Arrange
            await _provider.InitializeAsync();
            byte[] data = Encoding.UTF8.GetBytes("test_data");

            // Act
            byte[] compressedData = await _provider.CompressAsync(data);
            byte[] decompressedData = await _provider.DecompressAsync(compressedData);

            // Assert
            Assert.NotEqual(data, compressedData); // Compression should change the data
            Assert.Equal(data, decompressedData); // Decompression should restore the original data
        }

        [Fact]
        public async Task CompressAsync_WithRepeatingData_ReducesSize()
        {
            // Arrange
            await _provider.InitializeAsync();
            byte[] data = Encoding.UTF8.GetBytes(new string('a', 1000)); // Repeating data compresses well

            // Act
            byte[] compressedData = await _provider.CompressAsync(data);

            // Assert
            Assert.True(compressedData.Length < data.Length); // Compression should reduce size
        }

        [Fact]
        public async Task CompressAsync_WithRandomData_MayNotReduceSize()
        {
            // Arrange
            await _provider.InitializeAsync();
            byte[] data = new byte[1000];
            new Random().NextBytes(data); // Random data doesn't compress well

            // Act
            byte[] compressedData = await _provider.CompressAsync(data);

            // Assert
            // Note: Brotli might still reduce size slightly due to header compression,
            // but we don't make a strong assertion about the size reduction
            Assert.NotEqual(data, compressedData); // Compression should at least change the data
        }

        [Fact]
        public async Task GetProviderInfo_ReturnsCorrectInfo()
        {
            // Arrange
            await _provider.InitializeAsync();

            // Act
            var info = _provider.GetProviderInfo();

            // Assert
            Assert.Equal("Brotli", info.Name);
            Assert.Equal("Brotli", info.Algorithm);
            Assert.Equal(4, info.CompressionLevel); // Default
            Assert.True(info.SupportsStreaming);
            Assert.NotNull(info.AdditionalProperties);
            Assert.True(info.AdditionalProperties.ContainsKey("CompressionLevel"));
            Assert.Equal("4", info.AdditionalProperties["CompressionLevel"]);
        }

        [Fact]
        public async Task CompressAsync_WithDifferentCompressionLevels_ProducesDifferentResults()
        {
            // Arrange
            await _provider.InitializeAsync();
            byte[] data = Encoding.UTF8.GetBytes(new string('a', 1000)); // Repeating data

            var lowOptions = new BrotliCompressionOptions { CompressionLevel = 1 }; // Low compression
            var highOptions = new BrotliCompressionOptions { CompressionLevel = 9 }; // High compression
            
            var lowProvider = new BrotliCompressionProvider(_loggerMock.Object, lowOptions);
            var highProvider = new BrotliCompressionProvider(_loggerMock.Object, highOptions);
            
            await lowProvider.InitializeAsync();
            await highProvider.InitializeAsync();

            // Act
            byte[] lowCompressedData = await lowProvider.CompressAsync(data);
            byte[] highCompressedData = await highProvider.CompressAsync(data);

            // Assert
            Assert.NotEqual(lowCompressedData, highCompressedData); // Different compression levels should produce different results
            
            // Higher compression should produce smaller output than lower compression
            Assert.True(highCompressedData.Length <= lowCompressedData.Length);
            
            // Both should be able to decompress each other's data
            byte[] decompressedFromLow = await highProvider.DecompressAsync(lowCompressedData);
            byte[] decompressedFromHigh = await lowProvider.DecompressAsync(highCompressedData);
            
            Assert.Equal(data, decompressedFromLow);
            Assert.Equal(data, decompressedFromHigh);
        }

        [Fact]
        public async Task CompressAsync_LargeData_Succeeds()
        {
            // Arrange
            await _provider.InitializeAsync();
            byte[] data = Encoding.UTF8.GetBytes(new string('a', 1000000)); // 1MB of repeating data

            // Act
            byte[] compressedData = await _provider.CompressAsync(data);
            byte[] decompressedData = await _provider.DecompressAsync(compressedData);

            // Assert
            Assert.True(compressedData.Length < data.Length); // Compression should reduce size significantly
            Assert.Equal(data, decompressedData); // Decompression should restore the original data
        }
    }
}
