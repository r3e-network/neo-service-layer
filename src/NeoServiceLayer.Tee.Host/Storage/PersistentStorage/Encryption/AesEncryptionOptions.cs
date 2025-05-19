namespace NeoServiceLayer.Tee.Host.Storage.PersistentStorage.Encryption
{
    /// <summary>
    /// Options for the AES encryption provider.
    /// </summary>
    public class AesEncryptionOptions
    {
        /// <summary>
        /// Gets or sets the encryption key. If not provided, a key will be generated or loaded from the key file.
        /// </summary>
        public byte[] Key { get; set; }

        /// <summary>
        /// Gets or sets the path to the file containing the encryption key. If the file does not exist, a key will be generated and saved to this file.
        /// </summary>
        public string KeyFile { get; set; }

        /// <summary>
        /// Gets or sets the key size in bits. Default is 256 bits.
        /// </summary>
        public int KeySizeBits { get; set; } = 256;

        /// <summary>
        /// Gets or sets whether to automatically rotate the key after a certain period.
        /// </summary>
        public bool AutoRotateKey { get; set; } = false;

        /// <summary>
        /// Gets or sets the key rotation period in days. Only used if AutoRotateKey is true.
        /// </summary>
        public int KeyRotationPeriodDays { get; set; } = 90;
    }
}
