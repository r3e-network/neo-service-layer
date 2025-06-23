using System.Collections.Concurrent;
using System.Numerics;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Newtonsoft.Json.Linq;
// Type aliases to resolve ambiguity
using CoreBlock = NeoServiceLayer.Core.Block;
using CoreTransaction = NeoServiceLayer.Core.Transaction;
using NetherBlock = Nethereum.RPC.Eth.DTOs.BlockWithTransactions;
using NetherTransaction = Nethereum.RPC.Eth.DTOs.Transaction;

namespace NeoServiceLayer.Neo.X;

/// <summary>
/// Implementation of the Neo X (EVM-compatible) blockchain client using Nethereum.
/// </summary>
public class NeoXClient : IBlockchainClient, IDisposable
{
    private readonly ILogger<NeoXClient> _logger;
    private readonly Web3 _web3;
    private readonly string _rpcUrl;
    private readonly ConcurrentDictionary<string, Func<CoreBlock, Task>> _blockSubscriptions = new();
    private readonly ConcurrentDictionary<string, Func<CoreTransaction, Task>> _transactionSubscriptions = new();
    private readonly ConcurrentDictionary<string, (string ContractAddress, string EventName, Func<ContractEvent, Task> Callback)> _contractEventSubscriptions = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Timer _subscriptionTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="NeoXClient"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="rpcUrl">The RPC URL.</param>
    public NeoXClient(ILogger<NeoXClient> logger, HttpClient httpClient, string rpcUrl)
    {
        _logger = logger;
        _rpcUrl = rpcUrl;

        // Initialize Web3 with the provided HTTP client
        _web3 = new Web3(rpcUrl);

        // Configure HTTP client timeout
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        // Initialize subscription monitoring timer (check every 5 seconds)
        _subscriptionTimer = new Timer(ProcessSubscriptions, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

        _logger.LogInformation("Neo X client initialized with RPC URL: {RpcUrl}", rpcUrl);
    }

    /// <inheritdoc/>
    public BlockchainType BlockchainType => BlockchainType.NeoX;

    /// <inheritdoc/>
    public async Task<long> GetBlockHeightAsync()
    {
        try
        {
            _logger.LogDebug("Getting block height from {RpcUrl}", _rpcUrl);

            var blockNumber = await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            return (long)blockNumber.Value;
        }
        catch (RpcResponseException ex)
        {
            _logger.LogError(ex, "RPC error response while getting block height from {RpcUrl}", _rpcUrl);
            throw new InvalidOperationException($"RPC Error: {ex.RpcError.Message}", ex);
        }
        catch (RpcClientUnknownException ex)
        {
            // Check if this wraps an HttpRequestException and re-throw it
            if (ex.InnerException is HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP request error while getting block height from {RpcUrl}", _rpcUrl);
                throw httpEx;
            }
            _logger.LogError(ex, "RPC client error while getting block height from {RpcUrl}", _rpcUrl);
            throw new InvalidOperationException($"RPC Error: {ex.Message}", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request error while getting block height from {RpcUrl}", _rpcUrl);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get block height from {RpcUrl}", _rpcUrl);
            // Check if this is an RPC error by looking for specific patterns in the message
            if (ex.Message.Contains("Method not found", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("RPC", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"RPC Error: {ex.Message}", ex);
            }
            throw new InvalidOperationException($"Failed to get block height: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<CoreBlock> GetBlockAsync(long height)
    {
        try
        {
            _logger.LogDebug("Getting block at height {Height} from {RpcUrl}", height, _rpcUrl);

            var blockNumber = new HexBigInteger(height);
            var block = await _web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(blockNumber);

            if (block == null)
            {
                throw new InvalidOperationException($"Block at height {height} not found");
            }

            return await ConvertToBlockAsync(block);
        }
        catch (RpcResponseException ex)
        {
            _logger.LogError(ex, "RPC error response while getting block at height {Height} from {RpcUrl}", height, _rpcUrl);
            throw new InvalidOperationException($"RPC Error: {ex.RpcError.Message}", ex);
        }
        catch (RpcClientUnknownException ex)
        {
            // Check if this wraps an HttpRequestException and re-throw it
            if (ex.InnerException is HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP request error while getting block at height {Height} from {RpcUrl}", height, _rpcUrl);
                throw httpEx;
            }
            _logger.LogError(ex, "RPC client error while getting block at height {Height} from {RpcUrl}", height, _rpcUrl);
            throw new InvalidOperationException($"RPC Error: {ex.Message}", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request error while getting block at height {Height} from {RpcUrl}", height, _rpcUrl);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get block at height {Height} from {RpcUrl}", height, _rpcUrl);
            // Check if this is an RPC error by looking for specific patterns in the message
            if (ex.Message.Contains("Method not found", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("RPC", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"RPC Error: {ex.Message}", ex);
            }
            throw new InvalidOperationException($"Failed to get block at height {height}: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<CoreBlock> GetBlockAsync(string hash)
    {
        try
        {
            _logger.LogDebug("Getting block with hash {Hash} from {RpcUrl}", hash, _rpcUrl);

            var block = await _web3.Eth.Blocks.GetBlockWithTransactionsByHash.SendRequestAsync(hash);

            if (block == null)
            {
                throw new InvalidOperationException($"Block with hash {hash} not found");
            }

            return await ConvertToBlockAsync(block);
        }
        catch (RpcResponseException ex)
        {
            _logger.LogError(ex, "RPC error response while getting block with hash {Hash} from {RpcUrl}", hash, _rpcUrl);
            throw new InvalidOperationException($"RPC Error: {ex.RpcError.Message}", ex);
        }
        catch (RpcClientUnknownException ex)
        {
            // Check if this wraps an HttpRequestException and re-throw it
            if (ex.InnerException is HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP request error while getting block with hash {Hash} from {RpcUrl}", hash, _rpcUrl);
                throw httpEx;
            }
            _logger.LogError(ex, "RPC client error while getting block with hash {Hash} from {RpcUrl}", hash, _rpcUrl);
            throw new InvalidOperationException($"RPC Error: {ex.Message}", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request error while getting block with hash {Hash} from {RpcUrl}", hash, _rpcUrl);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get block with hash {Hash} from {RpcUrl}", hash, _rpcUrl);
            // Check if this is an RPC error by looking for specific patterns in the message
            if (ex.Message.Contains("Method not found", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("RPC", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"RPC Error: {ex.Message}", ex);
            }
            throw new InvalidOperationException($"Failed to get block with hash {hash}: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<CoreTransaction> GetTransactionAsync(string hash)
    {
        try
        {
            _logger.LogDebug("Getting transaction with hash {Hash} from {RpcUrl}", hash, _rpcUrl);

            var transaction = await _web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(hash);

            if (transaction == null)
            {
                throw new InvalidOperationException($"Transaction with hash {hash} not found");
            }

            var receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(hash);

            return ConvertToTransaction(transaction, receipt);
        }
        catch (RpcResponseException ex)
        {
            _logger.LogError(ex, "RPC error response while getting transaction with hash {Hash} from {RpcUrl}", hash, _rpcUrl);
            throw new InvalidOperationException($"RPC Error: {ex.RpcError.Message}", ex);
        }
        catch (RpcClientUnknownException ex)
        {
            // Check if this wraps an HttpRequestException and re-throw it
            if (ex.InnerException is HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP request error while getting transaction with hash {Hash} from {RpcUrl}", hash, _rpcUrl);
                throw httpEx;
            }
            _logger.LogError(ex, "RPC client error while getting transaction with hash {Hash} from {RpcUrl}", hash, _rpcUrl);
            throw new InvalidOperationException($"RPC Error: {ex.Message}", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request error while getting transaction with hash {Hash} from {RpcUrl}", hash, _rpcUrl);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get transaction with hash {Hash} from {RpcUrl}", hash, _rpcUrl);
            // Check if this is an RPC error by looking for specific patterns in the message
            if (ex.Message.Contains("Method not found", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("RPC", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"RPC Error: {ex.Message}", ex);
            }
            throw new InvalidOperationException($"Failed to get transaction with hash {hash}: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<string> SendTransactionAsync(CoreTransaction transaction)
    {
        try
        {
            _logger.LogDebug("Sending transaction from {Sender} to {Recipient} with value {Value} via {RpcUrl}",
                transaction.Sender, transaction.Recipient, transaction.Value, _rpcUrl);

            // Create transaction input
            var transactionInput = new TransactionInput
            {
                From = transaction.Sender,
                To = transaction.Recipient,
                Value = new HexBigInteger(Web3.Convert.ToWei(transaction.Value)),
                Data = transaction.Data
            };

            // Send the transaction
            var txHash = await _web3.Eth.Transactions.SendTransaction.SendRequestAsync(transactionInput);

            _logger.LogInformation("Transaction sent successfully with hash: {TxHash}", txHash);
            return txHash;
        }
        catch (RpcResponseException ex)
        {
            _logger.LogError(ex, "RPC error response while sending transaction from {Sender} to {Recipient}",
                transaction.Sender, transaction.Recipient);
            throw new InvalidOperationException($"RPC Error: {ex.RpcError.Message}", ex);
        }
        catch (RpcClientUnknownException ex)
        {
            // Check if this wraps an HttpRequestException and re-throw it
            if (ex.InnerException is HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP request error while sending transaction from {Sender} to {Recipient}",
                    transaction.Sender, transaction.Recipient);
                throw httpEx;
            }
            _logger.LogError(ex, "RPC client error while sending transaction from {Sender} to {Recipient}",
                transaction.Sender, transaction.Recipient);
            throw new InvalidOperationException($"RPC Error: {ex.Message}", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request error while sending transaction from {Sender} to {Recipient}",
                transaction.Sender, transaction.Recipient);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send transaction from {Sender} to {Recipient}",
                transaction.Sender, transaction.Recipient);
            // Check if this is an RPC error by looking for specific patterns in the message
            if (ex.Message.Contains("Method not found", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("RPC", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"RPC Error: {ex.Message}", ex);
            }
            throw new InvalidOperationException($"Failed to send transaction: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public Task<string> SubscribeToBlocksAsync(Func<CoreBlock, Task> callback)
    {
        try
        {
            string subscriptionId = Guid.NewGuid().ToString();
            _logger.LogDebug("Subscribing to blocks with subscription ID {SubscriptionId} via {RpcUrl}",
                subscriptionId, _rpcUrl);

            _blockSubscriptions[subscriptionId] = callback;

            _logger.LogInformation("Successfully subscribed to blocks with ID {SubscriptionId}", subscriptionId);
            return Task.FromResult(subscriptionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to blocks");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<bool> UnsubscribeFromBlocksAsync(string subscriptionId)
    {
        try
        {
            _logger.LogDebug("Unsubscribing from blocks with subscription ID {SubscriptionId} via {RpcUrl}",
                subscriptionId, _rpcUrl);

            var removed = _blockSubscriptions.TryRemove(subscriptionId, out _);

            if (removed)
            {
                _logger.LogInformation("Successfully unsubscribed from blocks with ID {SubscriptionId}", subscriptionId);
            }
            else
            {
                _logger.LogWarning("Block subscription {SubscriptionId} not found for unsubscription", subscriptionId);
            }

            return Task.FromResult(removed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe from blocks with subscription ID {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> SubscribeToTransactionsAsync(Func<CoreTransaction, Task> callback)
    {
        try
        {
            string subscriptionId = Guid.NewGuid().ToString();
            _logger.LogDebug("Subscribing to transactions with subscription ID {SubscriptionId} via {RpcUrl}",
                subscriptionId, _rpcUrl);

            _transactionSubscriptions[subscriptionId] = callback;

            _logger.LogInformation("Successfully subscribed to transactions with ID {SubscriptionId}", subscriptionId);
            return Task.FromResult(subscriptionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to transactions");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<bool> UnsubscribeFromTransactionsAsync(string subscriptionId)
    {
        try
        {
            _logger.LogDebug("Unsubscribing from transactions with subscription ID {SubscriptionId} via {RpcUrl}",
                subscriptionId, _rpcUrl);

            var removed = _transactionSubscriptions.TryRemove(subscriptionId, out _);

            if (removed)
            {
                _logger.LogInformation("Successfully unsubscribed from transactions with ID {SubscriptionId}", subscriptionId);
            }
            else
            {
                _logger.LogWarning("Transaction subscription {SubscriptionId} not found for unsubscription", subscriptionId);
            }

            return Task.FromResult(removed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe from transactions with ID {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> SubscribeToContractEventsAsync(string contractAddress, string eventName, Func<ContractEvent, Task> callback)
    {
        try
        {
            string subscriptionId = Guid.NewGuid().ToString();
            _logger.LogDebug("Subscribing to contract events for contract {ContractAddress} and event {EventName} with subscription ID {SubscriptionId} via {RpcUrl}",
                contractAddress, eventName, subscriptionId, _rpcUrl);

            _contractEventSubscriptions[subscriptionId] = (contractAddress, eventName, callback);

            _logger.LogInformation("Successfully subscribed to contract events for {ContractAddress}:{EventName}",
                contractAddress, eventName);
            return Task.FromResult(subscriptionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to contract events for {ContractAddress}:{EventName}",
                contractAddress, eventName);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<bool> UnsubscribeFromContractEventsAsync(string subscriptionId)
    {
        try
        {
            _logger.LogDebug("Unsubscribing from contract events with subscription ID {SubscriptionId} via {RpcUrl}",
                subscriptionId, _rpcUrl);

            var removed = _contractEventSubscriptions.TryRemove(subscriptionId, out _);

            if (removed)
            {
                _logger.LogInformation("Successfully unsubscribed from contract events with ID {SubscriptionId}", subscriptionId);
            }
            else
            {
                _logger.LogWarning("Contract event subscription {SubscriptionId} not found for unsubscription", subscriptionId);
            }

            return Task.FromResult(removed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe from contract events with subscription ID {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> CallContractMethodAsync(string contractAddress, string method, params object[] args)
    {
        try
        {
            _logger.LogDebug("Calling contract method {Method} on contract {ContractAddress} with {ArgCount} arguments via {RpcUrl}",
                method, contractAddress, args.Length, _rpcUrl);

            // Create a contract instance
            var contract = _web3.Eth.GetContract("[]", contractAddress); // Empty ABI for generic calls

            // Build function call data
            var functionCallData = BuildFunctionCallData(method, args);

            // Make the call
            var result = await _web3.Eth.Transactions.Call.SendRequestAsync(new CallInput
            {
                To = contractAddress,
                Data = functionCallData
            });

            _logger.LogDebug("Contract method call successful for {Method} on {ContractAddress}", method, contractAddress);
            return result;
        }
        catch (RpcResponseException ex)
        {
            _logger.LogError(ex, "RPC error response while calling contract method {Method} on {ContractAddress}", method, contractAddress);
            throw new InvalidOperationException($"RPC Error: {ex.RpcError.Message}", ex);
        }
        catch (RpcClientUnknownException ex)
        {
            // Check if this wraps an HttpRequestException and re-throw it
            if (ex.InnerException is HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP request error while calling contract method {Method} on {ContractAddress}", method, contractAddress);
                throw httpEx;
            }
            _logger.LogError(ex, "RPC client error while calling contract method {Method} on {ContractAddress}", method, contractAddress);
            throw new InvalidOperationException($"RPC Error: {ex.Message}", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request error while calling contract method {Method} on {ContractAddress}", method, contractAddress);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call contract method {Method} on {ContractAddress}", method, contractAddress);
            // Check if this is an RPC error by looking for specific patterns in the message
            if (ex.Message.Contains("Method not found", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("RPC", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"RPC Error: {ex.Message}", ex);
            }
            throw new InvalidOperationException($"Failed to call contract method {method}: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<string> InvokeContractMethodAsync(string contractAddress, string method, params object[] args)
    {
        try
        {
            _logger.LogDebug("Invoking contract method {Method} on contract {ContractAddress} with {ArgCount} arguments via {RpcUrl}",
                method, contractAddress, args.Length, _rpcUrl);

            // Build function call data
            var functionCallData = BuildFunctionCallData(method, args);

            // Create transaction input
            var transactionInput = new TransactionInput
            {
                To = contractAddress,
                Data = functionCallData,
                Gas = new HexBigInteger(200000) // Default gas limit
            };

            // Send the transaction
            var txHash = await _web3.Eth.Transactions.SendTransaction.SendRequestAsync(transactionInput);

            _logger.LogInformation("Contract method invocation sent successfully with hash: {TxHash}", txHash);
            return txHash;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invoke contract method {Method} on {ContractAddress}", method, contractAddress);
            throw;
        }
    }

    /// <summary>
    /// Converts a Nethereum block to our Block model.
    /// </summary>
    private async Task<CoreBlock> ConvertToBlockAsync(NetherBlock block)
    {
        var transactions = new List<CoreTransaction>();

        foreach (var tx in block.Transactions)
        {
            var receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(tx.TransactionHash);
            transactions.Add(ConvertToTransaction(tx, receipt));
        }

        return new CoreBlock
        {
            Hash = block.BlockHash,
            Height = (long)block.Number.Value,
            Timestamp = DateTimeOffset.FromUnixTimeSeconds((long)block.Timestamp.Value).DateTime,
            PreviousHash = block.ParentHash,
            Transactions = transactions
        };
    }

    /// <summary>
    /// Converts a Nethereum transaction to our Transaction model.
    /// </summary>
    private CoreTransaction ConvertToTransaction(NetherTransaction tx, TransactionReceipt? receipt)
    {
        return new CoreTransaction
        {
            Hash = tx.TransactionHash,
            Sender = tx.From,
            Recipient = tx.To ?? string.Empty,
            Value = Web3.Convert.FromWei(tx.Value?.Value ?? 0),
            Data = tx.Input ?? string.Empty,
            Timestamp = DateTime.UtcNow, // EVM transactions don't have individual timestamps
            BlockHash = receipt?.BlockHash ?? string.Empty,
            BlockHeight = (long)(receipt?.BlockNumber?.Value ?? 0)
        };
    }

    /// <summary>
    /// Builds function call data for contract method calls.
    /// </summary>
    private string BuildFunctionCallData(string method, object[] args)
    {
        // This is a simplified implementation
        // In a production environment, you would use proper ABI encoding
        var methodSignature = $"{method}({string.Join(",", args.Select(a => a.GetType().Name))})";
        var methodBytes = System.Text.Encoding.UTF8.GetBytes(methodSignature);
        var methodHash = Nethereum.Util.Sha3Keccack.Current.CalculateHash(methodBytes)[..4];

        // For now, return just the method hash
        // In production, you would properly encode the arguments
        return "0x" + Convert.ToHexString(methodHash);
    }

    /// <summary>
    /// Processes subscriptions by checking for new blocks and events.
    /// </summary>
    private async void ProcessSubscriptions(object? state)
    {
        if (_cancellationTokenSource.Token.IsCancellationRequested)
            return;

        try
        {
            await ProcessBlockSubscriptions();
            await ProcessTransactionSubscriptions();
            await ProcessContractEventSubscriptions();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing subscriptions");
        }
    }

    /// <summary>
    /// Processes block subscriptions.
    /// </summary>
    private async Task ProcessBlockSubscriptions()
    {
        if (!_blockSubscriptions.Any())
            return;

        try
        {
            var currentHeight = await GetBlockHeightAsync();
            var block = await GetBlockAsync(currentHeight);

            foreach (var subscription in _blockSubscriptions.Values)
            {
                try
                {
                    await subscription(block);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in block subscription callback");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing block subscriptions");
        }
    }

    /// <summary>
    /// Processes transaction subscriptions.
    /// </summary>
    private async Task ProcessTransactionSubscriptions()
    {
        if (!_transactionSubscriptions.Any())
            return;

        try
        {
            var currentHeight = await GetBlockHeightAsync();
            var block = await GetBlockAsync(currentHeight);

            foreach (var transaction in block.Transactions)
            {
                foreach (var subscription in _transactionSubscriptions.Values)
                {
                    try
                    {
                        await subscription(transaction);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in transaction subscription callback");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transaction subscriptions");
        }
    }

    /// <summary>
    /// Processes contract event subscriptions.
    /// </summary>
    private async Task ProcessContractEventSubscriptions()
    {
        if (!_contractEventSubscriptions.Any())
            return;

        try
        {
            var currentHeight = await GetBlockHeightAsync();
            var block = await GetBlockAsync(currentHeight);

            foreach (var (subscriptionId, (contractAddress, eventName, callback)) in _contractEventSubscriptions)
            {
                try
                {
                    var events = await ExtractContractEventsFromBlock(block, contractAddress, eventName);

                    foreach (var contractEvent in events)
                    {
                        await callback(contractEvent);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in contract event subscription callback for {SubscriptionId}", subscriptionId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing contract event subscriptions");
        }
    }

    /// <summary>
    /// Extracts contract events from a block.
    /// </summary>
    private async Task<List<ContractEvent>> ExtractContractEventsFromBlock(CoreBlock block, string contractAddress, string eventName)
    {
        var events = new List<ContractEvent>();

        foreach (var transaction in block.Transactions)
        {
            try
            {
                var receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transaction.Hash);

                if (receipt?.Logs != null)
                {
                    var logs = receipt.Logs as JArray;
                    if (logs != null)
                    {
                        foreach (JToken logToken in logs)
                        {
                            var filterLog = logToken.ToObject<FilterLog>();
                            if (filterLog != null)
                            {
                                var logAddress = filterLog.Address ?? string.Empty;
                                var logData = filterLog.Data ?? string.Empty;
                                var logTopics = filterLog.Topics ?? Array.Empty<object>();

                                if (string.Equals(logAddress, contractAddress, StringComparison.OrdinalIgnoreCase))
                                {
                                    events.Add(new ContractEvent
                                    {
                                        ContractAddress = logAddress,
                                        EventName = eventName,
                                        EventData = logData,
                                        Parameters = new Dictionary<string, object>
                                        {
                                            ["topics"] = logTopics,
                                            ["data"] = logData
                                        },
                                        TransactionHash = transaction.Hash,
                                        BlockHash = block.Hash,
                                        BlockHeight = block.Height,
                                        Timestamp = transaction.Timestamp
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting events from transaction {TxHash}", transaction.Hash);
            }
        }

        return events;
    }

    /// <summary>
    /// Disposes the client and releases resources.
    /// </summary>
    public void Dispose()
    {
        try
        {
            _logger.LogInformation("Disposing Neo X client");

            _cancellationTokenSource.Cancel();
            _subscriptionTimer?.Dispose();
            _cancellationTokenSource.Dispose();

            _blockSubscriptions.Clear();
            _transactionSubscriptions.Clear();
            _contractEventSubscriptions.Clear();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing Neo X client");
        }
    }
}

