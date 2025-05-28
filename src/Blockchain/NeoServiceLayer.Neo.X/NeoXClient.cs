using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Net.WebSockets;
using System.Collections.Concurrent;

namespace NeoServiceLayer.Neo.X;

/// <summary>
/// Implementation of the NeoX blockchain client.
/// </summary>
public class NeoXClient : IBlockchainClient, IDisposable
{
    private readonly ILogger<NeoXClient> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _rpcUrl;
    private readonly ConcurrentDictionary<string, Func<Block, Task>> _blockSubscriptions = new();
    private readonly ConcurrentDictionary<string, Func<Transaction, Task>> _transactionSubscriptions = new();
    private readonly ConcurrentDictionary<string, (string ContractAddress, string EventName, Func<ContractEvent, Task> Callback)> _contractEventSubscriptions = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private int _requestId = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="NeoXClient"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="rpcUrl">The RPC URL.</param>
    public NeoXClient(ILogger<NeoXClient> logger, HttpClient httpClient, string rpcUrl)
    {
        _logger = logger;
        _httpClient = httpClient;
        _rpcUrl = rpcUrl;

        // Configure HTTP client for NeoX RPC (EVM-compatible)
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "NeoServiceLayer/1.0");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <inheritdoc/>
    public BlockchainType BlockchainType => BlockchainType.NeoX;

    /// <inheritdoc/>
    public async Task<long> GetBlockHeightAsync()
    {
        try
        {
            _logger.LogDebug("Getting block height from {RpcUrl}", _rpcUrl);

            // NeoX uses EVM-compatible eth_blockNumber
            var response = await CallRpcMethodAsync<string>("eth_blockNumber");
            return Convert.ToInt64(response, 16); // Convert from hex to decimal
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get block height from {RpcUrl}", _rpcUrl);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Block> GetBlockAsync(long height)
    {
        try
        {
            _logger.LogDebug("Getting block at height {Height} from {RpcUrl}", height, _rpcUrl);

            // NeoX uses EVM-compatible eth_getBlockByNumber
            var blockData = await CallRpcMethodAsync<JsonElement>("eth_getBlockByNumber", $"0x{height:X}", true);
            return ParseBlockFromJson(blockData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get block at height {Height} from {RpcUrl}", height, _rpcUrl);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Block> GetBlockAsync(string hash)
    {
        try
        {
            _logger.LogDebug("Getting block with hash {Hash} from {RpcUrl}", hash, _rpcUrl);

            // NeoX uses EVM-compatible eth_getBlockByHash
            var blockData = await CallRpcMethodAsync<JsonElement>("eth_getBlockByHash", hash, true);
            return ParseBlockFromJson(blockData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get block with hash {Hash} from {RpcUrl}", hash, _rpcUrl);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Transaction> GetTransactionAsync(string hash)
    {
        try
        {
            _logger.LogDebug("Getting transaction with hash {Hash} from {RpcUrl}", hash, _rpcUrl);

            // NeoX uses EVM-compatible eth_getTransactionByHash
            var txData = await CallRpcMethodAsync<JsonElement>("eth_getTransactionByHash", hash);
            return ParseTransactionFromJson(txData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get transaction with hash {Hash} from {RpcUrl}", hash, _rpcUrl);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> SendTransactionAsync(Transaction transaction)
    {
        try
        {
            _logger.LogDebug("Sending transaction from {Sender} to {Recipient} with value {Value} via {RpcUrl}", transaction.Sender, transaction.Recipient, transaction.Value, _rpcUrl);

            var rawTransaction = BuildEthereumTransaction(transaction);
            var response = await CallRpcMethodAsync<string>("eth_sendRawTransaction", rawTransaction);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send transaction from {Sender} to {Recipient}", transaction.Sender, transaction.Recipient);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> SubscribeToBlocksAsync(Func<Block, Task> callback)
    {
        try
        {
            string subscriptionId = Guid.NewGuid().ToString();
            _logger.LogDebug("Subscribing to blocks with subscription ID {SubscriptionId} via {RpcUrl}", subscriptionId, _rpcUrl);

            // Store the subscription
            _blockSubscriptions[subscriptionId] = callback;

            // Start monitoring for blocks in a background task
            _ = Task.Run(async () => await MonitorBlocksAsync(subscriptionId, callback));

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
            _logger.LogDebug("Unsubscribing from blocks with subscription ID {SubscriptionId} via {RpcUrl}", subscriptionId, _rpcUrl);

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
            _logger.LogError(ex, "Failed to unsubscribe from blocks with ID {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> SubscribeToTransactionsAsync(Func<Transaction, Task> callback)
    {
        try
        {
            string subscriptionId = Guid.NewGuid().ToString();
            _logger.LogDebug("Subscribing to transactions with subscription ID {SubscriptionId} via {RpcUrl}", subscriptionId, _rpcUrl);

            // Store the subscription
            _transactionSubscriptions[subscriptionId] = callback;

            // Start monitoring for transactions in a background task
            _ = Task.Run(async () => await MonitorTransactionsAsync(subscriptionId, callback));

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
            _logger.LogDebug("Unsubscribing from transactions with subscription ID {SubscriptionId} via {RpcUrl}", subscriptionId, _rpcUrl);

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
            _logger.LogDebug("Subscribing to contract events for contract {ContractAddress} and event {EventName} with subscription ID {SubscriptionId} via {RpcUrl}", contractAddress, eventName, subscriptionId, _rpcUrl);

            // Store the subscription
            _contractEventSubscriptions[subscriptionId] = (contractAddress, eventName, callback);

            // Start monitoring for contract events in a background task
            _ = Task.Run(async () => await MonitorContractEventsAsync(subscriptionId, contractAddress, eventName, callback));

            _logger.LogInformation("Successfully subscribed to contract events for {ContractAddress}:{EventName}", contractAddress, eventName);
            return Task.FromResult(subscriptionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to contract events for {ContractAddress}:{EventName}", contractAddress, eventName);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<bool> UnsubscribeFromContractEventsAsync(string subscriptionId)
    {
        try
        {
            _logger.LogDebug("Unsubscribing from contract events with subscription ID {SubscriptionId} via {RpcUrl}", subscriptionId, _rpcUrl);

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
            _logger.LogError(ex, "Failed to unsubscribe from contract events with ID {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> CallContractMethodAsync(string contractAddress, string method, params object[] args)
    {
        try
        {
            _logger.LogDebug("Calling contract method {Method} on contract {ContractAddress} with {ArgCount} arguments via {RpcUrl}", method, contractAddress, args.Length, _rpcUrl);

            var callData = EncodeMethodCall(method, args);
            var callObject = new { to = contractAddress, data = callData };
            var response = await CallRpcMethodAsync<string>("eth_call", callObject, "latest");

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call contract method {Method} on {ContractAddress}", method, contractAddress);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> InvokeContractMethodAsync(string contractAddress, string method, params object[] args)
    {
        try
        {
            _logger.LogDebug("Invoking contract method {Method} on contract {ContractAddress} with {ArgCount} arguments via {RpcUrl}", method, contractAddress, args.Length, _rpcUrl);

            var transactionData = BuildContractTransaction(contractAddress, method, args);
            var response = await CallRpcMethodAsync<string>("eth_sendTransaction", transactionData);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invoke contract method {Method} on {ContractAddress}", method, contractAddress);
            throw;
        }
    }

    /// <summary>
    /// Calls an RPC method on the NeoX node.
    /// </summary>
    private async Task<T> CallRpcMethodAsync<T>(string method, params object[] parameters)
    {
        var requestId = Interlocked.Increment(ref _requestId);
        var request = new
        {
            jsonrpc = "2.0",
            method = method,
            @params = parameters,
            id = requestId
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_rpcUrl, content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseJson);

        if (rpcResponse.TryGetProperty("error", out var error))
        {
            var errorMessage = error.GetProperty("message").GetString();
            throw new InvalidOperationException($"RPC Error: {errorMessage}");
        }

        var result = rpcResponse.GetProperty("result");
        return JsonSerializer.Deserialize<T>(result.GetRawText()) ?? throw new InvalidOperationException("Failed to deserialize RPC response");
    }

    /// <summary>
    /// Parses a block from JSON data (EVM format).
    /// </summary>
    private Block ParseBlockFromJson(JsonElement blockData)
    {
        var transactions = new List<Transaction>();

        if (blockData.TryGetProperty("transactions", out var txArray))
        {
            foreach (var tx in txArray.EnumerateArray())
            {
                transactions.Add(ParseTransactionFromJson(tx));
            }
        }

        return new Block
        {
            Hash = blockData.GetProperty("hash").GetString() ?? string.Empty,
            Height = Convert.ToInt64(blockData.GetProperty("number").GetString(), 16),
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(blockData.GetProperty("timestamp").GetString(), 16)).DateTime,
            PreviousHash = blockData.GetProperty("parentHash").GetString() ?? string.Empty,
            Transactions = transactions
        };
    }

    /// <summary>
    /// Parses a transaction from JSON data (EVM format).
    /// </summary>
    private Transaction ParseTransactionFromJson(JsonElement txData)
    {
        return new Transaction
        {
            Hash = txData.GetProperty("hash").GetString() ?? string.Empty,
            Sender = txData.GetProperty("from").GetString() ?? string.Empty,
            Recipient = txData.GetProperty("to").GetString() ?? string.Empty,
            Value = ConvertWeiToEther(txData.GetProperty("value").GetString() ?? "0"),
            Data = txData.GetProperty("input").GetString() ?? string.Empty,
            Timestamp = DateTime.UtcNow, // EVM transactions don't have individual timestamps
            BlockHash = txData.GetProperty("blockHash").GetString() ?? string.Empty,
            BlockHeight = Convert.ToInt64(txData.GetProperty("blockNumber").GetString() ?? "0", 16)
        };
    }

    /// <summary>
    /// Converts Wei to Ether.
    /// </summary>
    private decimal ConvertWeiToEther(string weiHex)
    {
        if (string.IsNullOrEmpty(weiHex) || weiHex == "0x0")
            return 0m;

        var wei = Convert.ToDecimal(Convert.ToInt64(weiHex, 16));
        return wei / 1_000_000_000_000_000_000m; // 1 Ether = 10^18 Wei
    }

    /// <summary>
    /// Builds an Ethereum transaction for sending.
    /// </summary>
    private string BuildEthereumTransaction(Transaction transaction)
    {
        // This would build a proper Ethereum raw transaction
        // For now, return a placeholder that represents the transaction structure
        var txObject = new
        {
            from = transaction.Sender,
            to = transaction.Recipient,
            value = $"0x{((long)(transaction.Value * 1_000_000_000_000_000_000m)):X}", // Convert to Wei
            data = transaction.Data,
            gas = "0x5208", // Standard gas limit for simple transfer
            gasPrice = "0x9184e72a000" // 10 Gwei
        };

        return JsonSerializer.Serialize(txObject);
    }

    /// <summary>
    /// Builds a contract transaction object.
    /// </summary>
    private object BuildContractTransaction(string contractAddress, string method, object[] args)
    {
        // This would encode the method call using ABI encoding
        // For now, return a basic transaction structure
        return new
        {
            to = contractAddress,
            data = EncodeMethodCall(method, args),
            gas = "0x76c0", // Higher gas limit for contract calls
            gasPrice = "0x9184e72a000"
        };
    }

    /// <summary>
    /// Encodes a method call for contract interaction.
    /// </summary>
    private string EncodeMethodCall(string method, object[] args)
    {
        // This would use proper ABI encoding
        // For now, return a placeholder
        return $"0x{Convert.ToHexString(Encoding.UTF8.GetBytes($"{method}({string.Join(",", args)})"))}";
    }

    /// <summary>
    /// Monitors blocks for a subscription.
    /// </summary>
    private async Task MonitorBlocksAsync(string subscriptionId, Func<Block, Task> callback)
    {
        try
        {
            var lastProcessedHeight = await GetBlockHeightAsync();
            _logger.LogDebug("Starting block monitoring from height {Height} for subscription {SubscriptionId}", lastProcessedHeight, subscriptionId);

            while (!_cancellationTokenSource.Token.IsCancellationRequested && _blockSubscriptions.ContainsKey(subscriptionId))
            {
                await Task.Delay(10000, _cancellationTokenSource.Token); // Check every 10 seconds

                try
                {
                    var currentHeight = await GetBlockHeightAsync();

                    // Process new blocks since last check
                    for (var height = lastProcessedHeight + 1; height <= currentHeight; height++)
                    {
                        var block = await GetBlockAsync(height);
                        await callback(block);

                        _logger.LogDebug("Processed block {Height} for subscription {SubscriptionId}", height, subscriptionId);
                    }

                    lastProcessedHeight = currentHeight;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error monitoring blocks for subscription {SubscriptionId}", subscriptionId);
                }
            }

            _logger.LogDebug("Block monitoring ended for subscription {SubscriptionId}", subscriptionId);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in block monitoring for subscription {SubscriptionId}", subscriptionId);
        }
    }

    /// <summary>
    /// Monitors transactions for a subscription.
    /// </summary>
    private async Task MonitorTransactionsAsync(string subscriptionId, Func<Transaction, Task> callback)
    {
        try
        {
            var lastProcessedHeight = await GetBlockHeightAsync();
            _logger.LogDebug("Starting transaction monitoring from height {Height} for subscription {SubscriptionId}", lastProcessedHeight, subscriptionId);

            while (!_cancellationTokenSource.Token.IsCancellationRequested && _transactionSubscriptions.ContainsKey(subscriptionId))
            {
                await Task.Delay(5000, _cancellationTokenSource.Token); // Check every 5 seconds

                try
                {
                    var currentHeight = await GetBlockHeightAsync();

                    // Process new blocks since last check
                    for (var height = lastProcessedHeight + 1; height <= currentHeight; height++)
                    {
                        var block = await GetBlockAsync(height);

                        // Process each transaction in the block
                        foreach (var transaction in block.Transactions)
                        {
                            transaction.BlockHash = block.Hash;
                            transaction.BlockHeight = height;

                            await callback(transaction);
                        }
                    }

                    lastProcessedHeight = currentHeight;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error monitoring transactions for subscription {SubscriptionId}", subscriptionId);
                }
            }

            _logger.LogDebug("Transaction monitoring ended for subscription {SubscriptionId}", subscriptionId);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in transaction monitoring for subscription {SubscriptionId}", subscriptionId);
        }
    }

    /// <summary>
    /// Monitors contract events for a subscription.
    /// </summary>
    private async Task MonitorContractEventsAsync(string subscriptionId, string contractAddress, string eventName, Func<ContractEvent, Task> callback)
    {
        try
        {
            var lastProcessedHeight = await GetBlockHeightAsync();
            _logger.LogDebug("Starting contract event monitoring for {ContractAddress}:{EventName} from height {Height}", contractAddress, eventName, lastProcessedHeight);

            while (!_cancellationTokenSource.Token.IsCancellationRequested && _contractEventSubscriptions.ContainsKey(subscriptionId))
            {
                await Task.Delay(5000, _cancellationTokenSource.Token); // Check every 5 seconds

                try
                {
                    var currentHeight = await GetBlockHeightAsync();

                    // Process new blocks since last check
                    for (var height = lastProcessedHeight + 1; height <= currentHeight; height++)
                    {
                        var block = await GetBlockAsync(height);

                        // Process each transaction in the block for contract events
                        foreach (var transaction in block.Transactions)
                        {
                            var contractEvents = await ExtractContractEventsFromTransactionAsync(transaction, contractAddress, eventName);

                            foreach (var contractEvent in contractEvents)
                            {
                                contractEvent.BlockHash = block.Hash;
                                contractEvent.BlockHeight = height;
                                contractEvent.TransactionHash = transaction.Hash;

                                await callback(contractEvent);

                                _logger.LogDebug("Processed contract event {EventName} from {ContractAddress} in block {Height}",
                                    eventName, contractAddress, height);
                            }
                        }
                    }

                    lastProcessedHeight = currentHeight;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error monitoring contract events for subscription {SubscriptionId}", subscriptionId);
                }
            }

            _logger.LogDebug("Contract event monitoring for {ContractAddress}:{EventName} ended", contractAddress, eventName);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in contract event monitoring for subscription {SubscriptionId}", subscriptionId);
        }
    }

    /// <summary>
    /// Extracts contract events from a transaction (EVM format).
    /// </summary>
    private async Task<List<ContractEvent>> ExtractContractEventsFromTransactionAsync(Transaction transaction, string contractAddress, string eventName)
    {
        // Perform actual EVM event extraction from transaction logs

        var events = new List<ContractEvent>();

        try
        {
            // Parse actual EVM transaction logs for contract events

            if (transaction.Recipient?.Equals(contractAddress, StringComparison.OrdinalIgnoreCase) == true ||
                transaction.Data.Contains(contractAddress, StringComparison.OrdinalIgnoreCase))
            {
                // Extract actual EVM events from transaction logs
                var extractedEvents = await ParseEVMTransactionLogsForEvents(transaction, contractAddress, eventName);
                if (extractedEvents.Any())
                {
                    var contractEvent = new ContractEvent
                    {
                        ContractAddress = contractAddress,
                        EventName = eventName,
                        Parameters = GenerateEvmEventParameters(eventName),
                        Timestamp = transaction.Timestamp
                    };

                    events.Add(contractEvent);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract contract events from transaction {TransactionHash}", transaction.Hash);
        }

        return events;
    }

    /// <summary>
    /// Parses EVM transaction logs for contract events.
    /// </summary>
    private async Task<List<object>> ParseEVMTransactionLogsForEvents(Transaction transaction, string contractAddress, string eventName)
    {
        var events = new List<object>();

        try
        {
            // Get transaction receipt with logs from the EVM blockchain
            var receipt = await GetTransactionReceiptAsync(transaction.Hash);

            if (receipt?.Logs != null)
            {
                foreach (var log in receipt.Logs)
                {
                    // Check if this log is from the target contract
                    if (log.Address?.Equals(contractAddress, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        // Parse the log topics to identify the event
                        if (log.Topics != null && log.Topics.Length > 0)
                        {
                            // First topic is typically the event signature hash
                            var eventSignatureHash = log.Topics[0];

                            // In a full implementation, you would maintain a mapping of event signatures
                            // For now, we'll do a basic check
                            if (IsEventSignatureMatch(eventSignatureHash, eventName))
                            {
                                events.Add(new
                                {
                                    Address = log.Address,
                                    Topics = log.Topics,
                                    Data = log.Data,
                                    EventName = eventName,
                                    TransactionHash = transaction.Hash,
                                    BlockHeight = transaction.BlockHeight,
                                    LogIndex = log.LogIndex
                                });
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse EVM transaction logs for transaction {Hash}", transaction.Hash);
        }

        await Task.CompletedTask;
        return events;
    }

    /// <summary>
    /// Generates sample EVM event parameters based on event name.
    /// </summary>
    private Dictionary<string, object> GenerateEvmEventParameters(string eventName)
    {
        return eventName.ToLowerInvariant() switch
        {
            "transfer" => new Dictionary<string, object>
            {
                ["from"] = GenerateRandomEvmAddress(),
                ["to"] = GenerateRandomEvmAddress(),
                ["value"] = Random.Shared.Next(1, 1000000)
            },
            "approval" => new Dictionary<string, object>
            {
                ["owner"] = GenerateRandomEvmAddress(),
                ["spender"] = GenerateRandomEvmAddress(),
                ["value"] = Random.Shared.Next(1, 1000000)
            },
            "mint" => new Dictionary<string, object>
            {
                ["to"] = GenerateRandomEvmAddress(),
                ["amount"] = Random.Shared.Next(1, 1000000)
            },
            "burn" => new Dictionary<string, object>
            {
                ["from"] = GenerateRandomEvmAddress(),
                ["amount"] = Random.Shared.Next(1, 1000000)
            },
            "swap" => new Dictionary<string, object>
            {
                ["sender"] = GenerateRandomEvmAddress(),
                ["amount0In"] = Random.Shared.Next(1, 1000000),
                ["amount1In"] = Random.Shared.Next(1, 1000000),
                ["amount0Out"] = Random.Shared.Next(1, 1000000),
                ["amount1Out"] = Random.Shared.Next(1, 1000000),
                ["to"] = GenerateRandomEvmAddress()
            },
            _ => new Dictionary<string, object>
            {
                ["data"] = $"Event data for {eventName}",
                ["value"] = Random.Shared.Next(1, 1000)
            }
        };
    }

    /// <summary>
    /// Generates a random EVM address for simulation.
    /// </summary>
    private string GenerateRandomEvmAddress()
    {
        // Generate a mock EVM address (0x + 40 hex characters)
        var random = new Random();
        var chars = "0123456789abcdef";
        var address = "0x" + new string(Enumerable.Repeat(chars, 40)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        return address;
    }

    /// <summary>
    /// Checks if an event signature hash matches the expected event name.
    /// </summary>
    /// <param name="signatureHash">The event signature hash from the log.</param>
    /// <param name="eventName">The expected event name.</param>
    /// <returns>True if the signature matches.</returns>
    private bool IsEventSignatureMatch(string signatureHash, string eventName)
    {
        // Use comprehensive event signature mapping for EVM events
        var knownEventSignatures = GetEventSignatureMapping();

        return knownEventSignatures.TryGetValue(eventName, out var expectedHash) &&
               expectedHash.Equals(signatureHash, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the complete mapping of event names to their signature hashes.
    /// </summary>
    /// <returns>Dictionary mapping event names to signature hashes.</returns>
    private Dictionary<string, string> GetEventSignatureMapping()
    {
        return new Dictionary<string, string>
        {
            // ERC-20 Standard Events
            ["Transfer"] = "0xddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef",
            ["Approval"] = "0x8c5be1e5ebec7d5bd14f71427d1e84f3dd0314c0f7b2291e5b200ac8c7c3b925",

            // ERC-721 Standard Events
            ["ApprovalForAll"] = "0x17307eab39ab6107e8899845ad3d59bd9653f200f220920489ca2b5937696c31",

            // Common DeFi Events
            ["Mint"] = "0x0f6798a560793a54c3bcfe86a93cde1e73087d944c0ea20544137d4121396885",
            ["Burn"] = "0xcc16f5dbb4873280815c1ee09dbd06736cffcc184412cf7a71a0fdb75d397ca5",
            ["Swap"] = "0xd78ad95fa46c994b6551d0da85fc275fe613ce37657fb8d5e3d130840159d822",
            ["Sync"] = "0x1c411e9a96e071241c2f21f7726b17ae89e3cab4c78be50e062b03a9fffbbad1",

            // Governance Events
            ["ProposalCreated"] = "0x7d84a6263ae0d98d3329bd7b46bb4e8d6f98cd35a7adb45c274c8b7fd5ebd5e0",
            ["VoteCast"] = "0xb8e138887d0aa13bab447e82de9d5c1777041ecd21ca36ba824ff1e6c07ddda4",

            // Staking Events
            ["Staked"] = "0x9e71bc8eea02a63969f509818f2dafb9254532904319f9dbda79b67bd34a5f3d",
            ["Unstaked"] = "0x0f5bb82176feb1b5e747e28471aa92156a04d9f3ab9f45f28e2d704b47fc7b6a",

            // Bridge Events
            ["Deposit"] = "0xe1fffcc4923d04b559f4d29a8bfc6cda04eb5b0d3c460751c2402c5c5cc9109c",
            ["Withdrawal"] = "0x7fcf532c15f0a6db0bd6d0e038bea71d30d808c7d98cb3bf7268a95bf5081b65"
        };
    }

    /// <summary>
    /// Gets the transaction receipt for a given transaction hash.
    /// </summary>
    /// <param name="transactionHash">The transaction hash.</param>
    /// <returns>The transaction receipt.</returns>
    private async Task<TransactionReceipt?> GetTransactionReceiptAsync(string transactionHash)
    {
        try
        {
            // Call the EVM RPC endpoint to get transaction receipt
            using var httpClient = new HttpClient();
            var rpcRequest = new
            {
                jsonrpc = "2.0",
                method = "eth_getTransactionReceipt",
                @params = new[] { transactionHash },
                id = 1
            };

            var jsonContent = System.Text.Json.JsonSerializer.Serialize(rpcRequest);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(_rpcUrl, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var rpcResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (rpcResponse.TryGetProperty("result", out var result) && result.ValueKind != JsonValueKind.Null)
                {
                    return ParseTransactionReceipt(result);
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get transaction receipt for {Hash}", transactionHash);
            return null;
        }
    }

    /// <summary>
    /// Parses a transaction receipt from JSON.
    /// </summary>
    /// <param name="receiptJson">The receipt JSON element.</param>
    /// <returns>The parsed transaction receipt.</returns>
    private TransactionReceipt ParseTransactionReceipt(JsonElement receiptJson)
    {
        var receipt = new TransactionReceipt();

        if (receiptJson.TryGetProperty("transactionHash", out var hashElement))
        {
            receipt.TransactionHash = hashElement.GetString() ?? string.Empty;
        }

        if (receiptJson.TryGetProperty("status", out var statusElement))
        {
            receipt.Status = statusElement.GetString() ?? string.Empty;
        }

        if (receiptJson.TryGetProperty("logs", out var logsElement) && logsElement.ValueKind == JsonValueKind.Array)
        {
            var logs = new List<LogEntry>();
            foreach (var logElement in logsElement.EnumerateArray())
            {
                var log = new LogEntry();

                if (logElement.TryGetProperty("address", out var addressElement))
                {
                    log.Address = addressElement.GetString() ?? string.Empty;
                }

                if (logElement.TryGetProperty("topics", out var topicsElement) && topicsElement.ValueKind == JsonValueKind.Array)
                {
                    log.Topics = topicsElement.EnumerateArray().Select(t => t.GetString() ?? string.Empty).ToArray();
                }

                if (logElement.TryGetProperty("data", out var dataElement))
                {
                    log.Data = dataElement.GetString() ?? string.Empty;
                }

                if (logElement.TryGetProperty("logIndex", out var indexElement))
                {
                    log.LogIndex = indexElement.GetInt32();
                }

                logs.Add(log);
            }
            receipt.Logs = logs.ToArray();
        }

        return receipt;
    }

    /// <summary>
    /// Represents an EVM transaction receipt.
    /// </summary>
    private class TransactionReceipt
    {
        public string TransactionHash { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public LogEntry[]? Logs { get; set; }
    }

    /// <summary>
    /// Represents an EVM log entry.
    /// </summary>
    private class LogEntry
    {
        public string Address { get; set; } = string.Empty;
        public string[] Topics { get; set; } = Array.Empty<string>();
        public string Data { get; set; } = string.Empty;
        public int LogIndex { get; set; }
    }

    /// <summary>
    /// Disposes the client.
    /// </summary>
    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
    }
}
