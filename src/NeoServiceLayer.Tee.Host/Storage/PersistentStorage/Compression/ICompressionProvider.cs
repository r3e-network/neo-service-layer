using System;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Host.Storage.PersistentStorage.Compression
{
    /// <summary>
    /// Interface for compression providers.
    /// </summary>
    public interface ICompressionProvider : IDisposable
    {
        /// <summary>
        /// Gets the name of the compression provider.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the description of the compression provider.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Initializes the compression provider.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task InitializeAsync();

        /// <summary>
        /// Compresses data.
        /// </summary>
        /// <param name="data">The data to compress.</param>
        /// <returns>The compressed data.</returns>
        Task<byte[]> CompressAsync(byte[] data);

        /// <summary>
        /// Decompresses data.
        /// </summary>
        /// <param name="compressedData">The compressed data to decompress.</param>
        /// <returns>The decompressed data.</returns>
        Task<byte[]> DecompressAsync(byte[] compressedData);

        /// <summary>
        /// Gets information about the compression provider.
        /// </summary>
        /// <returns>Information about the compression provider.</returns>
        CompressionProviderInfo GetProviderInfo();
    }
}
