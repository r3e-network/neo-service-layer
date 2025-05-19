namespace NeoServiceLayer.Tee.Host.Storage.PersistentStorage.Compression
{
    /// <summary>
    /// Options for the GZip compression provider.
    /// </summary>
    public class GZipCompressionOptions
    {
        /// <summary>
        /// Gets or sets the compression level. 0 = NoCompression, 1 = Fastest, 2 = Optimal.
        /// </summary>
        public int CompressionLevel { get; set; } = 2; // Optimal
    }
}
