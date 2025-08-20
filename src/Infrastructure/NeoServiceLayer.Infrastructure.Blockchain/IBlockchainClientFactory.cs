using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using NeoServiceLayer.Core;


namespace NeoServiceLayer.Infrastructure.Blockchain;

/// <summary>
/// Interface for blockchain client factories.
/// </summary>
public interface IBlockchainClientFactory
{
    /// <summary>
    /// Creates a blockchain client for the specified blockchain type.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The blockchain client.</returns>
    IBlockchainClient CreateClient(BlockchainType blockchainType);

    /// <summary>
    /// Gets all supported blockchain types.
    /// </summary>
    /// <returns>All supported blockchain types.</returns>
    IEnumerable<BlockchainType> GetSupportedBlockchainTypes();
}
