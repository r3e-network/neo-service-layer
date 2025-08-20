using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Tee.Host.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


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

    /// <summary>
    /// Gets the service name (alias for Name property).
    /// </summary>
    protected string ServiceName => Name;

    /// <inheritdoc/>
    public bool SupportsBlockchain(BlockchainType blockchainType)
    {
        return SupportedBlockchains.Contains(blockchainType);
    }

    /// <summary>
    /// Validates that the blockchain type is supported by this service.
    /// </summary>
    /// <param name="blockchainType">The blockchain type to validate.</param>
    protected void ValidateBlockchainType(BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported by {Name}");
        }
    }
}
