namespace NeoServiceLayer.Core.Models
{
    /// <summary>
    /// Options for storage.
    /// </summary>
    public class StorageOptions
    {
        /// <summary>
        /// Gets or sets the storage provider.
        /// </summary>
        public string Provider { get; set; } = "OcclumFileStorage";

        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the encryption options.
        /// </summary>
        public EncryptionOptions Encryption { get; set; } = new EncryptionOptions();

        /// <summary>
        /// Gets or sets the compression options.
        /// </summary>
        public CompressionOptions Compression { get; set; } = new CompressionOptions();

        /// <summary>
        /// Gets or sets the chunking options.
        /// </summary>
        public ChunkingOptions Chunking { get; set; } = new ChunkingOptions();

        /// <summary>
        /// Gets or sets the transaction options.
        /// </summary>
        public TransactionOptions Transaction { get; set; } = new TransactionOptions();
    }

    /// <summary>
    /// Options for encryption.
    /// </summary>
    public class EncryptionOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether encryption is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the key size in bits.
        /// </summary>
        public int KeySize { get; set; } = 256;

        /// <summary>
        /// Gets or sets the encryption algorithm.
        /// </summary>
        public string Algorithm { get; set; } = "AES-GCM";
    }

    /// <summary>
    /// Options for compression.
    /// </summary>
    public class CompressionOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether compression is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the compression level.
        /// </summary>
        public string Level { get; set; } = "Optimal";

        /// <summary>
        /// Gets or sets the compression algorithm.
        /// </summary>
        public string Algorithm { get; set; } = "Deflate";
    }

    /// <summary>
    /// Options for chunking.
    /// </summary>
    public class ChunkingOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether chunking is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the chunk size in bytes.
        /// </summary>
        public int ChunkSize { get; set; } = 1048576; // 1 MB
    }

    /// <summary>
    /// Options for transactions.
    /// </summary>
    public class TransactionOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether transactions are enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the isolation level.
        /// </summary>
        public string IsolationLevel { get; set; } = "ReadCommitted";
    }
}
