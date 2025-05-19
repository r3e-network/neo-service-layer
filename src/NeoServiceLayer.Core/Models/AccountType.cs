namespace NeoServiceLayer.Core.Models
{
    /// <summary>
    /// Represents the type of a TEE account.
    /// </summary>
    public enum AccountType
    {
        /// <summary>
        /// A wallet account.
        /// </summary>
        Wallet = 0,

        /// <summary>
        /// An identity account.
        /// </summary>
        Identity = 1,

        /// <summary>
        /// A service account.
        /// </summary>
        Service = 2,

        /// <summary>
        /// A contract account.
        /// </summary>
        Contract = 3,

        /// <summary>
        /// An ECDSA account.
        /// </summary>
        ECDSA = 4,

        /// <summary>
        /// An ED25519 account.
        /// </summary>
        ED25519 = 5,

        /// <summary>
        /// An RSA account.
        /// </summary>
        RSA = 6,

        /// <summary>
        /// An AES account.
        /// </summary>
        AES = 7,

        /// <summary>
        /// An HMAC account.
        /// </summary>
        HMAC = 8
    }
}
