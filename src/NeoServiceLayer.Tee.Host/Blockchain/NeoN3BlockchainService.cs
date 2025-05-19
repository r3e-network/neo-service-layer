using System;
using System.Threading.Tasks;
using System.Text.Json;
using System.Net.Http;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Host.Blockchain.Components;
using NeoServiceLayer.Tee.Shared.Blockchain;

namespace NeoServiceLayer.Tee.Host.Blockchain
{
    /// <summary>
    /// Implementation of the blockchain service for Neo N3.
    /// </summary>
    public class NeoN3BlockchainService : IBlockchainService, INeoN3BlockchainService, IDisposable
    {
        private readonly ILogger<NeoN3BlockchainService> _logger;
        private readonly BlockchainQueryService _queryService;
        private readonly BlockchainContractService _contractService;
        private readonly BlockchainTransactionService _transactionService;
        private bool _initialized;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the NeoN3BlockchainService class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="rpcUrl">The RPC URL.</param>
        /// <param name="walletPath">The wallet path.</param>
        /// <param name="walletPassword">The wallet password.</param>
        /// <param name="network">The network name.</param>
        public NeoN3BlockchainService(
            ILogger<NeoN3BlockchainService> logger,
            string rpcUrl,
            string walletPath = null,
            string walletPassword = null,
            string network = "mainnet")
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize services
            _queryService = new BlockchainQueryService(logger, rpcUrl, network);
            _contractService = new BlockchainContractService(logger, rpcUrl, network);

            if (!string.IsNullOrEmpty(walletPath) && !string.IsNullOrEmpty(walletPassword))
            {
                _transactionService = new BlockchainTransactionService(logger, rpcUrl, walletPath, walletPassword, network);
            }

            _initialized = false;
            _disposed = false;
        }

        /// <inheritdoc/>
        public async Task<bool> InitializeAsync()
        {
            CheckDisposed();

            try
            {
                if (_initialized)
                {
                    return true;
                }

                // Initialize all services
                var queryInitialized = await (_queryService as BlockchainServiceBase).GetType()
                    .GetMethod("InitializeAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .Invoke(_queryService, null) as Task<bool>;

                var contractInitialized = await (_contractService as BlockchainServiceBase).GetType()
                    .GetMethod("InitializeAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .Invoke(_contractService, null) as Task<bool>;

                bool transactionInitialized = true;
                if (_transactionService != null)
                {
                    transactionInitialized = await (_transactionService as BlockchainServiceBase).GetType()
                        .GetMethod("InitializeAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        .Invoke(_transactionService, null) as Task<bool>;
                }

                _initialized = queryInitialized.Result && contractInitialized.Result && transactionInitialized;

                if (_initialized)
                {
                    _logger.LogInformation("Neo N3 blockchain service initialized successfully");
                }
                else
                {
                    _logger.LogError("Failed to initialize Neo N3 blockchain service");
                }

                return _initialized;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Neo N3 blockchain service");
                return false;
            }
        }

        /// <inheritdoc/>
        public Task<ulong> GetBlockchainHeightAsync()
        {
            CheckDisposed();
            CheckInitialized();
            return _queryService.GetBlockchainHeightAsync();
        }

        /// <inheritdoc/>
        public Task<BlockchainBlock> GetBlockByHeightAsync(ulong height)
        {
            CheckDisposed();
            CheckInitialized();
            return _queryService.GetBlockByHeightAsync(height);
        }

        /// <inheritdoc/>
        public Task<BlockchainBlock> GetBlockByHashAsync(string hash)
        {
            CheckDisposed();
            CheckInitialized();
            return _queryService.GetBlockByHashAsync(hash);
        }

        /// <inheritdoc/>
        public Task<BlockchainTransaction> GetTransactionAsync(string hash)
        {
            CheckDisposed();
            CheckInitialized();
            return _queryService.GetTransactionAsync(hash);
        }

        /// <inheritdoc/>
        public Task<decimal> GetBalanceAsync(string address, string assetId = null)
        {
            CheckDisposed();
            CheckInitialized();
            return _contractService.GetBalanceAsync(address, assetId);
        }

        /// <inheritdoc/>
        public Task<BlockchainEvent[]> GetContractEventsAsync(string contractHash, ulong fromBlock = 0, int count = 100)
        {
            CheckDisposed();
            CheckInitialized();
            return _contractService.GetContractEventsAsync(contractHash, fromBlock, count);
        }

        /// <inheritdoc/>
        public Task<BlockchainEvent[]> GetContractEventsByNameAsync(string contractHash, string eventName, ulong fromBlock = 0, int count = 100)
        {
            CheckDisposed();
            CheckInitialized();
            return _contractService.GetContractEventsByNameAsync(contractHash, eventName, fromBlock, count);
        }

        /// <inheritdoc/>
        public Task<string> InvokeContractAsync(string contractHash, string operation, params object[] args)
        {
            CheckDisposed();
            CheckInitialized();

            if (_transactionService == null)
            {
                throw new InvalidOperationException("Transaction service is not initialized. Wallet path and password are required for this operation.");
            }

            return _transactionService.InvokeContractAsync(contractHash, operation, args);
        }

        /// <inheritdoc/>
        public Task<ContractInvocationResult> TestInvokeContractAsync(string contractHash, string operation, params object[] args)
        {
            CheckDisposed();
            CheckInitialized();

            if (_transactionService == null)
            {
                throw new InvalidOperationException("Transaction service is not initialized. Wallet path and password are required for this operation.");
            }

            return _transactionService.TestInvokeContractAsync(contractHash, operation, args);
        }

        /// <inheritdoc/>
        public Task<string> SendCallbackTransactionAsync(string contractHash, string method, string functionId, string result)
        {
            CheckDisposed();
            CheckInitialized();

            if (_transactionService == null)
            {
                throw new InvalidOperationException("Transaction service is not initialized. Wallet path and password are required for this operation.");
            }

            return _transactionService.SendCallbackTransactionAsync(contractHash, method, functionId, result);
        }

        /// <inheritdoc/>
        public async Task<bool> UnsubscribeFromEventsAsync(string subscriptionId)
        {
            CheckDisposed();
            CheckInitialized();

            if (string.IsNullOrEmpty(subscriptionId))
            {
                throw new ArgumentException("Subscription ID cannot be null or empty", nameof(subscriptionId));
            }

            try
            {
                // Remove the subscription
                var removed = _subscriptions.TryRemove(subscriptionId, out var subscription);
                if (removed)
                {
                    _logger.LogInformation("Unsubscribed from Neo N3 contract {ContractHash} events", subscription.ContractHash);
                }
                else
                {
                    _logger.LogWarning("Subscription {SubscriptionId} not found", subscriptionId);
                }

                return removed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsubscribing from events with subscription ID {SubscriptionId}", subscriptionId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<BlockchainSubscription>> GetActiveSubscriptionsAsync()
        {
            CheckDisposed();
            CheckInitialized();

            try
            {
                return _subscriptions.Values.Where(s => s.IsActive).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active subscriptions");
                throw;
            }
        }

        /// <inheritdoc/>
        public BlockchainType GetBlockchainType()
        {
            return BlockchainType.NeoN3;
        }

        /// <inheritdoc/>
        public string GetBlockchainNetwork()
        {
            return _network;
        }

        /// <inheritdoc/>
        public string GetBlockchainRpcUrl()
        {
            return _rpcUrl;
        }

        /// <inheritdoc/>
        public async Task<string> GetBlockchainVersionAsync()
        {
            CheckDisposed();
            CheckInitialized();

            try
            {
                var response = await SendRpcRequestAsync("getversion", Array.Empty<object>());
                var version = response.GetProperty("result").GetProperty("protocol").GetProperty("network").GetString();

                return version;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Neo N3 blockchain version");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<int> GetBlockchainPeerCountAsync()
        {
            CheckDisposed();
            CheckInitialized();

            try
            {
                var response = await SendRpcRequestAsync("getpeers", Array.Empty<object>());
                var connected = response.GetProperty("result").GetProperty("connected").GetArrayLength();

                return connected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Neo N3 blockchain peer count");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<BlockchainSyncState> GetBlockchainSyncStateAsync()
        {
            CheckDisposed();
            CheckInitialized();

            try
            {
                // Get the current height
                var height = await GetBlockchainHeightAsync();

                // Get the highest block from peers
                var response = await SendRpcRequestAsync("getpeers", Array.Empty<object>());
                var connectedPeers = response.GetProperty("result").GetProperty("connected");

                ulong highestPeerHeight = 0;
                foreach (var peer in connectedPeers.EnumerateArray())
                {
                    var peerHeight = (ulong)peer.GetProperty("height").GetInt64();
                    if (peerHeight > highestPeerHeight)
                    {
                        highestPeerHeight = peerHeight;
                    }
                }

                // If no peers, return unknown
                if (highestPeerHeight == 0)
                {
                    return BlockchainSyncState.Unknown;
                }

                // If the current height is within 1 block of the highest peer, consider it synced
                if (height >= highestPeerHeight - 1)
                {
                    return BlockchainSyncState.Synced;
                }

                // Otherwise, it's syncing
                return BlockchainSyncState.Syncing;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Neo N3 blockchain sync state");
                return BlockchainSyncState.Unknown;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> IsConnectedAsync()
        {
            try
            {
                var response = await SendRpcRequestAsync("getblockcount", Array.Empty<object>());
                return response.TryGetProperty("result", out _);
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> IsSyncedAsync()
        {
            CheckDisposed();
            CheckInitialized();

            try
            {
                var syncState = await GetBlockchainSyncStateAsync();
                return syncState == BlockchainSyncState.Synced;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Sends an RPC request to the Neo N3 blockchain.
        /// </summary>
        /// <param name="method">The RPC method.</param>
        /// <param name="parameters">The RPC parameters.</param>
        /// <returns>The RPC response.</returns>
        private async Task<JsonElement> SendRpcRequestAsync(string method, object[] parameters)
        {
            var requestId = Interlocked.Increment(ref _requestId);
            var request = new
            {
                jsonrpc = "2.0",
                id = requestId,
                method,
                @params = parameters
            };

            var content = new StringContent(JsonSerializer.Serialize(request, _jsonOptions), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_rpcUrl, content);

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(responseContent).RootElement;
        }

        /// <summary>
        /// Builds the parameters for invoking a function.
        /// </summary>
        /// <param name="contractHash">The contract hash.</param>
        /// <param name="operation">The operation to invoke.</param>
        /// <param name="args">The arguments to pass to the operation.</param>
        /// <returns>The parameters for invoking a function.</returns>
        private object[] BuildInvokeFunctionParams(string contractHash, string operation, object[] args)
        {
            var parameters = new List<object> { contractHash, operation };

            // Add arguments
            var formattedArgs = new List<object>();
            foreach (var arg in args)
            {
                formattedArgs.Add(FormatArgument(arg));
            }
            parameters.Add(formattedArgs.ToArray());

            return parameters.ToArray();
        }

        /// <summary>
        /// Formats an argument for invoking a function.
        /// </summary>
        /// <param name="arg">The argument to format.</param>
        /// <returns>The formatted argument.</returns>
        private object FormatArgument(object arg)
        {
            if (arg == null)
            {
                return new { type = "Null" };
            }

            if (arg is string str)
            {
                return new { type = "String", value = str };
            }

            if (arg is int || arg is long || arg is uint || arg is ulong)
            {
                return new { type = "Integer", value = arg.ToString() };
            }

            if (arg is bool b)
            {
                return new { type = "Boolean", value = b };
            }

            if (arg is byte[] bytes)
            {
                return new { type = "ByteArray", value = Convert.ToBase64String(bytes) };
            }

            // Default to string representation
            return new { type = "String", value = arg.ToString() };
        }

        /// <summary>
        /// Checks if the service is initialized.
        /// </summary>
        private void CheckInitialized()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Neo N3 blockchain service is not initialized");
            }
        }

        /// <summary>
        /// Checks if the service is disposed.
        /// </summary>
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(NeoN3BlockchainService));
            }
        }



        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the service.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    (_queryService as IDisposable)?.Dispose();
                    (_contractService as IDisposable)?.Dispose();
                    (_transactionService as IDisposable)?.Dispose();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizes the service.
        /// </summary>
        ~NeoN3BlockchainService()
        {
            Dispose(false);
        }
    }
}
