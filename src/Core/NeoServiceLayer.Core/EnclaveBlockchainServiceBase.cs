using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Core;

/// <summary>
/// Base class for services that require both enclave and blockchain capabilities.
/// This implementation maintains clean separation of concerns without circular dependencies.
/// </summary>
public abstract class EnclaveBlockchainServiceBase : EnclaveServiceBase, IBlockchainService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnclaveBlockchainServiceBase"/> class.
    /// </summary>
    protected EnclaveBlockchainServiceBase(
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

        // Update metadata
        SetMetadata("SupportedBlockchains", SupportedBlockchains.Select(b => b.ToString()).ToList());
        SetMetadata("ServiceType", "EnclaveBlockchain");
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

    /// <summary>
    /// Validates both blockchain support and enclave initialization.
    /// </summary>
    /// <param name="blockchainType">The blockchain type to validate.</param>
    /// <exception cref="NotSupportedException">Thrown if the blockchain type is not supported.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the enclave is not initialized.</exception>
    protected void ValidateBlockchainAndEnclave(BlockchainType blockchainType)
    {
        ValidateBlockchainSupport(blockchainType);

        if (!IsEnclaveInitialized)
        {
            throw new InvalidOperationException($"Enclave must be initialized before blockchain operations for service {Name}");
        }
    }
}
