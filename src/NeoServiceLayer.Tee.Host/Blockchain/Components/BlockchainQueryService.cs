using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Shared.Blockchain;

namespace NeoServiceLayer.Tee.Host.Blockchain.Components
{
    /// <summary>
    /// Service for querying blockchain data.
    /// </summary>
    public class BlockchainQueryService : BlockchainServiceBase
    {
        /// <summary>
        /// Initializes a new instance of the BlockchainQueryService class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="rpcUrl">The RPC URL.</param>
        /// <param name="network">The network name.</param>
        public BlockchainQueryService(ILogger logger, string rpcUrl, string network = "mainnet")
            : base(logger, rpcUrl, network)
        {
        }

        /// <summary>
        /// Gets the blockchain height.
        /// </summary>
        /// <returns>The blockchain height.</returns>
        public async Task<ulong> GetBlockchainHeightAsync()
        {
            CheckDisposed();
            CheckInitialized();

            try
            {
                var response = await SendRpcRequestAsync("getblockcount", Array.Empty<object>());
                var height = response.GetProperty("result").GetInt64();

                _logger.LogDebug("Neo N3 blockchain height: {Height}", height);

                return (ulong)height;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Neo N3 blockchain height");
                throw;
            }
        }

        /// <summary>
        /// Gets a block by height.
        /// </summary>
        /// <param name="height">The block height.</param>
        /// <returns>The block.</returns>
        public async Task<BlockchainBlock> GetBlockByHeightAsync(ulong height)
        {
            CheckDisposed();
            CheckInitialized();

            try
            {
                var response = await SendRpcRequestAsync("getblock", new object[] { height, true });
                var blockJson = response.GetProperty("result");

                var block = new BlockchainBlock
                {
                    Hash = blockJson.GetProperty("hash").GetString(),
                    Height = (ulong)blockJson.GetProperty("index").GetInt64(),
                    Timestamp = (ulong)blockJson.GetProperty("time").GetInt64(),
                    Size = (uint)blockJson.GetProperty("size").GetInt32(),
                    Version = (uint)blockJson.GetProperty("version").GetInt32(),
                    MerkleRoot = blockJson.GetProperty("merkleroot").GetString(),
                    PreviousHash = blockJson.GetProperty("previousblockhash").GetString(),
                    NextHash = blockJson.TryGetProperty("nextblockhash", out var nextHash) ? nextHash.GetString() : null,
                    TransactionCount = (uint)blockJson.GetProperty("tx").GetArrayLength()
                };

                // Extract transaction hashes
                var txHashes = new List<string>();
                foreach (var tx in blockJson.GetProperty("tx").EnumerateArray())
                {
                    txHashes.Add(tx.GetProperty("hash").GetString());
                }
                block.TransactionHashes = txHashes.ToArray();

                _logger.LogDebug("Retrieved Neo N3 block at height {Height}", height);

                return block;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Neo N3 block at height {Height}", height);
                throw;
            }
        }

        /// <summary>
        /// Gets a block by hash.
        /// </summary>
        /// <param name="hash">The block hash.</param>
        /// <returns>The block.</returns>
        public async Task<BlockchainBlock> GetBlockByHashAsync(string hash)
        {
            CheckDisposed();
            CheckInitialized();

            if (string.IsNullOrEmpty(hash))
            {
                throw new ArgumentException("Block hash cannot be null or empty", nameof(hash));
            }

            try
            {
                var response = await SendRpcRequestAsync("getblock", new object[] { hash, true });
                var blockJson = response.GetProperty("result");

                var block = new BlockchainBlock
                {
                    Hash = blockJson.GetProperty("hash").GetString(),
                    Height = (ulong)blockJson.GetProperty("index").GetInt64(),
                    Timestamp = (ulong)blockJson.GetProperty("time").GetInt64(),
                    Size = (uint)blockJson.GetProperty("size").GetInt32(),
                    Version = (uint)blockJson.GetProperty("version").GetInt32(),
                    MerkleRoot = blockJson.GetProperty("merkleroot").GetString(),
                    PreviousHash = blockJson.GetProperty("previousblockhash").GetString(),
                    NextHash = blockJson.TryGetProperty("nextblockhash", out var nextHash) ? nextHash.GetString() : null,
                    TransactionCount = (uint)blockJson.GetProperty("tx").GetArrayLength()
                };

                // Extract transaction hashes
                var txHashes = new List<string>();
                foreach (var tx in blockJson.GetProperty("tx").EnumerateArray())
                {
                    txHashes.Add(tx.GetProperty("hash").GetString());
                }
                block.TransactionHashes = txHashes.ToArray();

                _logger.LogDebug("Retrieved Neo N3 block with hash {Hash}", hash);

                return block;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Neo N3 block with hash {Hash}", hash);
                throw;
            }
        }

        /// <summary>
        /// Gets a transaction by hash.
        /// </summary>
        /// <param name="hash">The transaction hash.</param>
        /// <returns>The transaction.</returns>
        public async Task<BlockchainTransaction> GetTransactionAsync(string hash)
        {
            CheckDisposed();
            CheckInitialized();

            if (string.IsNullOrEmpty(hash))
            {
                throw new ArgumentException("Transaction hash cannot be null or empty", nameof(hash));
            }

            try
            {
                var response = await SendRpcRequestAsync("getrawtransaction", new object[] { hash, true });
                var txJson = response.GetProperty("result");

                var tx = new BlockchainTransaction
                {
                    Hash = txJson.GetProperty("hash").GetString(),
                    BlockHash = txJson.TryGetProperty("blockhash", out var blockHash) ? blockHash.GetString() : null,
                    BlockHeight = txJson.TryGetProperty("blockindex", out var blockIndex) ? (ulong)blockIndex.GetInt64() : 0,
                    Timestamp = txJson.TryGetProperty("blocktime", out var blockTime) ? (ulong)blockTime.GetInt64() : 0,
                    Sender = txJson.TryGetProperty("sender", out var sender) ? sender.GetString() : null,
                    Size = (uint)txJson.GetProperty("size").GetInt32(),
                    Version = (uint)txJson.GetProperty("version").GetInt32(),
                    Nonce = (ulong)txJson.GetProperty("nonce").GetInt64(),
                    Script = txJson.GetProperty("script").GetString(),
                    SystemFee = txJson.GetProperty("sysfee").GetString(),
                    NetworkFee = txJson.GetProperty("netfee").GetString()
                };

                // Extract signers
                var signers = new List<string>();
                if (txJson.TryGetProperty("signers", out var signersJson))
                {
                    foreach (var signer in signersJson.EnumerateArray())
                    {
                        signers.Add(signer.GetProperty("account").GetString());
                    }
                }
                tx.Signers = signers.ToArray();

                // Extract attributes
                var attributes = new List<TransactionAttribute>();
                if (txJson.TryGetProperty("attributes", out var attributesJson))
                {
                    foreach (var attribute in attributesJson.EnumerateArray())
                    {
                        attributes.Add(new TransactionAttribute
                        {
                            Type = attribute.GetProperty("type").GetString(),
                            Value = attribute.GetProperty("value").GetString()
                        });
                    }
                }
                tx.Attributes = attributes.ToArray();

                // Extract witnesses
                var witnesses = new List<TransactionWitness>();
                if (txJson.TryGetProperty("witnesses", out var witnessesJson))
                {
                    foreach (var witness in witnessesJson.EnumerateArray())
                    {
                        witnesses.Add(new TransactionWitness
                        {
                            InvocationScript = witness.GetProperty("invocation").GetString(),
                            VerificationScript = witness.GetProperty("verification").GetString()
                        });
                    }
                }
                tx.Witnesses = witnesses.ToArray();

                _logger.LogDebug("Retrieved Neo N3 transaction with hash {Hash}", hash);

                return tx;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Neo N3 transaction with hash {Hash}", hash);
                throw;
            }
        }
    }
}
