namespace NeoServiceLayer.Tee.Host.Storage.PersistentStorage.Compression
{
    /// <summary>
    /// Options for the Brotli compression provider.
    /// </summary>
    public class BrotliCompressionOptions
    {
        /// <summary>
        /// Gets or sets the compression level. 0 (no compression) to 11 (maximum compression).
        /// </summary>
        public int CompressionLevel { get; set; } = 4; // Default
    }
}
