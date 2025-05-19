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
    public class GZipCompressionProviderTests : IDisposable
    {
        private readonly Mock<ILogger<GZipCompressionProvider>> _loggerMock;
        private readonly GZipCompressionOptions _options;
        private readonly GZipCompressionProvider _provider;

        public GZipCompressionProviderTests()
        {
            _loggerMock = new Mock<ILogger<GZipCompressionProvider>>();
            _options = new GZipCompressionOptions
            {
                CompressionLevel = 2 // Optimal
            };
            _provider = new GZipCompressionProvider(_loggerMock.Object, _options);
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
            // Note: GZip might still reduce size slightly due to header compression,
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
            Assert.Equal("GZip", info.Name);
            Assert.Equal("GZip", info.Algorithm);
            Assert.Equal(2, info.CompressionLevel); // Optimal
            Assert.True(info.SupportsStreaming);
            Assert.NotNull(info.AdditionalProperties);
            Assert.True(info.AdditionalProperties.ContainsKey("CompressionLevel"));
            Assert.Equal("Optimal", info.AdditionalProperties["CompressionLevel"]);
        }

        [Fact]
        public async Task CompressAsync_WithDifferentCompressionLevels_ProducesDifferentResults()
        {
            // Arrange
            await _provider.InitializeAsync();
            byte[] data = Encoding.UTF8.GetBytes(new string('a', 1000)); // Repeating data

            var fastOptions = new GZipCompressionOptions { CompressionLevel = 1 }; // Fastest
            var optimalOptions = new GZipCompressionOptions { CompressionLevel = 2 }; // Optimal
            
            var fastProvider = new GZipCompressionProvider(_loggerMock.Object, fastOptions);
            var optimalProvider = new GZipCompressionProvider(_loggerMock.Object, optimalOptions);
            
            await fastProvider.InitializeAsync();
            await optimalProvider.InitializeAsync();

            // Act
            byte[] fastCompressedData = await fastProvider.CompressAsync(data);
            byte[] optimalCompressedData = await optimalProvider.CompressAsync(data);

            // Assert
            Assert.NotEqual(fastCompressedData, optimalCompressedData); // Different compression levels should produce different results
            
            // Optimal compression should produce smaller output than fastest compression
            Assert.True(optimalCompressedData.Length <= fastCompressedData.Length);
            
            // Both should be able to decompress each other's data
            byte[] decompressedFromFast = await optimalProvider.DecompressAsync(fastCompressedData);
            byte[] decompressedFromOptimal = await fastProvider.DecompressAsync(optimalCompressedData);
            
            Assert.Equal(data, decompressedFromFast);
            Assert.Equal(data, decompressedFromOptimal);
        }
    }
}
