using System.Collections.Generic;

namespace NeoServiceLayer.Tee.Host.Storage.PersistentStorage.Compression
{
    /// <summary>
    /// Information about a compression provider.
    /// </summary>
    public class CompressionProviderInfo
    {
        /// <summary>
        /// Gets or sets the name of the compression provider.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the compression provider.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the algorithm used by the compression provider.
        /// </summary>
        public string Algorithm { get; set; }

        /// <summary>
        /// Gets or sets the compression level.
        /// </summary>
        public int CompressionLevel { get; set; }

        /// <summary>
        /// Gets or sets whether the compression provider supports streaming.
        /// </summary>
        public bool SupportsStreaming { get; set; }

        /// <summary>
        /// Gets or sets additional properties of the compression provider.
        /// </summary>
        public Dictionary<string, string> AdditionalProperties { get; set; } = new Dictionary<string, string>();
    }
}
