namespace NeoServiceLayer.Core;

/// <summary>
/// Blockchain type enumeration.
/// </summary>
public enum BlockchainType
{
    /// <summary>
    /// Neo N3 blockchain.
    /// </summary>
    NeoN3,

    /// <summary>
    /// Neo X (EVM-compatible) blockchain.
    /// </summary>
    NeoX,

    /// <summary>
    /// Ethereum blockchain.
    /// </summary>
    Ethereum,

    /// <summary>
    /// Test blockchain for development.
    /// </summary>
    Test,

    /// <summary>
    /// Bitcoin blockchain.
    /// </summary>
    Bitcoin
}