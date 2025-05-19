using System;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Shared.Blockchain;

namespace NeoServiceLayer.Tee.Host.Blockchain
{
    /// <summary>
    /// Factory for creating blockchain services.
    /// </summary>
    public class BlockchainServiceFactory
    {
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes a new instance of the BlockchainServiceFactory class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        public BlockchainServiceFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>
        /// Creates a blockchain service.
        /// </summary>
        /// <param name="blockchainType">The blockchain type.</param>
        /// <param name="rpcUrl">The RPC URL.</param>
        /// <param name="network">The network name.</param>
        /// <returns>The blockchain service.</returns>
        public IBlockchainService CreateBlockchainService(BlockchainType blockchainType, string rpcUrl, string network = "mainnet")
        {
            if (string.IsNullOrEmpty(rpcUrl))
            {
                throw new ArgumentException("RPC URL cannot be null or empty", nameof(rpcUrl));
            }

            if (string.IsNullOrEmpty(network))
            {
                throw new ArgumentException("Network cannot be null or empty", nameof(network));
            }

            switch (blockchainType)
            {
                case BlockchainType.NeoN3:
                    return new NeoN3BlockchainService(_loggerFactory.CreateLogger<NeoN3BlockchainService>(), rpcUrl, network);

                case BlockchainType.Ethereum:
                case BlockchainType.BinanceSmartChain:
                case BlockchainType.Polygon:
                    throw new NotImplementedException($"Blockchain type {blockchainType} is not implemented yet");

                default:
                    throw new ArgumentException($"Unsupported blockchain type: {blockchainType}", nameof(blockchainType));
            }
        }

        /// <summary>
        /// Creates a blockchain event listener.
        /// </summary>
        /// <param name="blockchainService">The blockchain service.</param>
        /// <param name="storageManager">The storage manager.</param>
        /// <returns>The blockchain event listener.</returns>
        public IBlockchainEventListener CreateBlockchainEventListener(IBlockchainService blockchainService, Storage.IStorageManager storageManager)
        {
            if (blockchainService == null)
            {
                throw new ArgumentNullException(nameof(blockchainService));
            }

            if (storageManager == null)
            {
                throw new ArgumentNullException(nameof(storageManager));
            }

            return new BlockchainEventListener(
                _loggerFactory.CreateLogger<BlockchainEventListener>(),
                blockchainService,
                storageManager);
        }
    }
}
