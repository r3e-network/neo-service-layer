using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.ServiceFramework;

/// <summary>
/// Base class for services that support blockchain operations.
/// </summary>
public abstract class BlockchainServiceBase : ServiceBase, IBlockchainService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlockchainServiceBase"/> class.
    /// </summary>
    /// <param name="name">The name of the service.</param>
    /// <param name="description">The description of the service.</param>
    /// <param name="version">The version of the service.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="supportedBlockchains">The supported blockchain types.</param>
    protected BlockchainServiceBase(string name, string description, string version, ILogger logger, IEnumerable<BlockchainType> supportedBlockchains)
        : base(name, description, version, logger)
    {
        SupportedBlockchains = supportedBlockchains.ToList();
    }

    /// <inheritdoc/>
    public IEnumerable<BlockchainType> SupportedBlockchains { get; }

    /// <inheritdoc/>
    public bool SupportsBlockchain(BlockchainType blockchainType)
    {
        return SupportedBlockchains.Contains(blockchainType);
    }
}
