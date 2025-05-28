using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Tee.Host.Services;

namespace NeoServiceLayer.ServiceFramework;

/// <summary>
/// Base class for services that require both enclave and blockchain operations.
/// </summary>
public abstract class EnclaveBlockchainServiceBase : EnclaveServiceBase, IBlockchainService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnclaveBlockchainServiceBase"/> class.
    /// </summary>
    /// <param name="name">The name of the service.</param>
    /// <param name="description">The description of the service.</param>
    /// <param name="version">The version of the service.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="supportedBlockchains">The supported blockchain types.</param>
    /// <param name="enclaveManager">The enclave manager (optional).</param>
    protected EnclaveBlockchainServiceBase(string name, string description, string version, ILogger logger, IEnumerable<BlockchainType> supportedBlockchains, IEnclaveManager? enclaveManager = null)
        : base(name, description, version, logger, enclaveManager)
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
