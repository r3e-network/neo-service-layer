namespace NeoServiceLayer.Services.Backup.Models;

/// <summary>
/// Compression algorithm enumeration.
/// </summary>
public enum CompressionAlgorithm
{
    /// <summary>
    /// No compression.
    /// </summary>
    None,

    /// <summary>
    /// GZip compression.
    /// </summary>
    GZip,

    /// <summary>
    /// Deflate compression.
    /// </summary>
    Deflate,

    /// <summary>
    /// Brotli compression.
    /// </summary>
    Brotli,

    /// <summary>
    /// LZ4 compression.
    /// </summary>
    LZ4,

    /// <summary>
    /// LZMA compression.
    /// </summary>
    LZMA
}

/// <summary>
/// Compression level enumeration.
/// </summary>
public enum CompressionLevel
{
    /// <summary>
    /// No compression.
    /// </summary>
    NoCompression,

    /// <summary>
    /// Fastest compression.
    /// </summary>
    Fastest,

    /// <summary>
    /// Optimal compression.
    /// </summary>
    Optimal,

    /// <summary>
    /// Smallest size compression.
    /// </summary>
    SmallestSize
}

/// <summary>
/// Encryption algorithm enumeration.
/// </summary>
public enum EncryptionAlgorithm
{
    /// <summary>
    /// No encryption.
    /// </summary>
    None,

    /// <summary>
    /// AES-128 encryption.
    /// </summary>
    AES128,

    /// <summary>
    /// AES-256 encryption.
    /// </summary>
    AES256,

    /// <summary>
    /// ChaCha20 encryption.
    /// </summary>
    ChaCha20,

    /// <summary>
    /// RSA encryption.
    /// </summary>
    RSA
}

/// <summary>
/// Key derivation function enumeration.
/// </summary>
public enum KeyDerivationFunction
{
    /// <summary>
    /// PBKDF2 key derivation.
    /// </summary>
    PBKDF2,

    /// <summary>
    /// Scrypt key derivation.
    /// </summary>
    Scrypt,

    /// <summary>
    /// Argon2 key derivation.
    /// </summary>
    Argon2
}

/// <summary>
/// Compression settings.
/// </summary>
public class CompressionSettings
{
    /// <summary>
    /// Gets or sets whether compression is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the compression algorithm.
    /// </summary>
    public CompressionAlgorithm Algorithm { get; set; } = CompressionAlgorithm.GZip;

    /// <summary>
    /// Gets or sets the compression level.
    /// </summary>
    public CompressionLevel Level { get; set; } = CompressionLevel.Optimal;

    /// <summary>
    /// Gets or sets additional compression options.
    /// </summary>
    public Dictionary<string, object> Options { get; set; } = new();
}

/// <summary>
/// Encryption settings.
/// </summary>
public class EncryptionSettings
{
    /// <summary>
    /// Gets or sets whether encryption is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the encryption algorithm.
    /// </summary>
    public EncryptionAlgorithm Algorithm { get; set; } = EncryptionAlgorithm.AES256;

    /// <summary>
    /// Gets or sets the encryption key.
    /// </summary>
    public string? EncryptionKey { get; set; }

    /// <summary>
    /// Gets or sets the key derivation settings.
    /// </summary>
    public KeyDerivationSettings KeyDerivation { get; set; } = new();

    /// <summary>
    /// Gets or sets additional encryption options.
    /// </summary>
    public Dictionary<string, object> Options { get; set; } = new();
}

/// <summary>
/// Key derivation settings.
/// </summary>
public class KeyDerivationSettings
{
    /// <summary>
    /// Gets or sets the key derivation function.
    /// </summary>
    public KeyDerivationFunction Function { get; set; } = KeyDerivationFunction.PBKDF2;

    /// <summary>
    /// Gets or sets the number of iterations.
    /// </summary>
    public int Iterations { get; set; } = 100000;

    /// <summary>
    /// Gets or sets the salt.
    /// </summary>
    public byte[] Salt { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets additional derivation options.
    /// </summary>
    public Dictionary<string, object> Options { get; set; } = new();
}

/// <summary>
/// Retention policy.
/// </summary>
public class RetentionPolicy
{
    /// <summary>
    /// Gets or sets the retention period.
    /// </summary>
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// Gets or sets the maximum number of backups to keep.
    /// </summary>
    public int MaxBackupCount { get; set; } = 10;

    /// <summary>
    /// Gets or sets whether to auto-delete expired backups.
    /// </summary>
    public bool AutoDeleteExpired { get; set; } = true;

    /// <summary>
    /// Gets or sets additional retention options.
    /// </summary>
    public Dictionary<string, object> Options { get; set; } = new();
}
