using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Threading;
using System;


namespace NeoServiceLayer.Core;

/// <summary>
/// Base class for services that support blockchain operations.
/// This is a clean implementation without circular dependencies.
/// </summary>
public abstract class BlockchainServiceBase : ServiceBase, IBlockchainService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlockchainServiceBase"/> class.
    /// </summary>
    protected BlockchainServiceBase(
        string name,
        string description,
        string version,
        ILogger logger,
        IEnumerable<BlockchainType> supportedBlockchains)
        : base(name, description, version, logger)
    {
        SupportedBlockchains = (supportedBlockchains ?? throw new ArgumentNullException(nameof(supportedBlockchains))).ToList();

        if (!SupportedBlockchains.Any())
        {
            throw new ArgumentException("At least one supported blockchain must be specified", nameof(supportedBlockchains));
        }

        // Add blockchain capability
        AddCapability<IBlockchainService>();

        // Set metadata
        SetMetadata("SupportedBlockchains", SupportedBlockchains.Select(b => b.ToString()).ToList());
    }

    /// <inheritdoc/>
    public IEnumerable<BlockchainType> SupportedBlockchains { get; }

    /// <inheritdoc/>
    public bool SupportsBlockchain(BlockchainType blockchainType)
    {
        return SupportedBlockchains.Contains(blockchainType);
    }

    /// <summary>
    /// Validates that the specified blockchain type is supported by this service.
    /// </summary>
    /// <param name="blockchainType">The blockchain type to validate.</param>
    /// <exception cref="NotSupportedException">Thrown if the blockchain type is not supported.</exception>
    protected void ValidateBlockchainSupport(BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported by service {Name}");
        }
    }
}
