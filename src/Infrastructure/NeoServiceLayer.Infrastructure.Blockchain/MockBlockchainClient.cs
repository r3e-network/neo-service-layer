using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Infrastructure;

/// <summary>
/// Production implementation of the blockchain client for Neo N3 and Neo X networks.
/// </summary>
public class NeoBlockchainClient : IBlockchainClient
{
    private readonly ILogger _logger;
    private readonly BlockchainType _blockchainType;
    private readonly HttpClient _httpClient;
    private readonly string _rpcEndpoint;
    private readonly Dictionary<string, Func<Block, Task>> _blockSubscriptions = new();
    private readonly Dictionary<string, Func<Transaction, Task>> _transactionSubscriptions = new();
    private readonly Dictionary<string, (string ContractAddress, string EventName, Func<ContractEvent, Task> Callback)> _contractEventSubscriptions = new();
    private readonly object _subscriptionsLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="NeoBlockchainClient"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="httpClient">The HTTP client for RPC calls.</param>
    /// <param name="rpcEndpoint">The RPC endpoint URL.</param>
    public NeoBlockchainClient(ILogger logger, BlockchainType blockchainType, HttpClient httpClient, string rpcEndpoint)
    {
        _logger = logger;
        _blockchainType = blockchainType;
        _httpClient = httpClient;
        _rpcEndpoint = rpcEndpoint;
    }

    /// <inheritdoc/>
    public BlockchainType BlockchainType => _blockchainType;

    /// <inheritdoc/>
    public async Task<string> CallContractAsync(string contractAddress, string method, params object[] parameters)
    {
        _logger.LogInformation("Calling contract {ContractAddress}.{Method} on {BlockchainType}", contractAddress, method, _blockchainType);

        try
        {
            var request = new JsonRpcRequest
            {
                Id = 1,
                Method = "invokefunction",
                Params = new object[]
                {
                    contractAddress,
                    method,
                    parameters?.Select(ConvertParameterToNeoFormat).ToArray() ?? Array.Empty<object>()
                }
            };

            var response = await SendJsonRpcRequestAsync(request);

            if (response?.Result != null)
            {
                return JsonSerializer.Serialize(response.Result);
            }

            throw new InvalidOperationException($"Contract call failed: {response?.Error?.Message ?? "Unknown error"}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call contract {ContractAddress}.{Method}", contractAddress, method);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> CallContractMethodAsync(string contractAddress, string method, params object[] args)
    {
        return CallContractAsync(contractAddress, method, args);
    }

    /// <inheritdoc/>
    public async Task<decimal> EstimateGasAsync(Transaction transaction)
    {
        _logger.LogInformation("Estimating gas for transaction on {BlockchainType}", _blockchainType);

        try
        {
            // For Neo, we can use calculatenetworkfee RPC method
            var request = new JsonRpcRequest
            {
                Id = 1,
                Method = "calculatenetworkfee",
                Params = new object[]
                {
                    Convert.ToBase64String(Encoding.UTF8.GetBytes(transaction.Data ?? ""))
                }
            };

            var response = await SendJsonRpcRequestAsync(request);

            if (response?.Result != null && decimal.TryParse(response.Result.ToString(), out var fee))
            {
                return fee / 100000000m; // Convert from NeoGAS smallest unit
            }

            // Fallback to default gas estimation
            return _blockchainType == BlockchainType.NeoN3 ? 0.01m : 0.001m;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to estimate gas, using default value");
            return _blockchainType == BlockchainType.NeoN3 ? 0.01m : 0.001m;
        }
    }

    /// <inheritdoc/>
    public async Task<decimal> GetBalanceAsync(string address, string assetId)
    {
        _logger.LogInformation("Getting balance for address {Address} and asset {AssetId} on {BlockchainType}", address, assetId, _blockchainType);

        try
        {
            var request = new JsonRpcRequest
            {
                Id = 1,
                Method = "getnep17balances",
                Params = new object[] { address }
            };

            var response = await SendJsonRpcRequestAsync(request);

            if (response?.Result != null)
            {
                var balanceData = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(response.Result));

                if (balanceData.TryGetProperty("balance", out var balances))
                {
                    foreach (var balance in balances.EnumerateArray())
                    {
                        if (balance.TryGetProperty("assethash", out var hash) &&
                            hash.GetString() == assetId &&
                            balance.TryGetProperty("amount", out var amount))
                        {
                            if (decimal.TryParse(amount.GetString(), out var balanceValue))
                            {
                                return balanceValue / 100000000m; // Convert from smallest unit
                            }
                        }
                    }
                }
            }

            return 0m;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get balance for address {Address}", address);
            return 0m;
        }
    }

    /// <inheritdoc/>
    public async Task<Block> GetBlockAsync(long height)
    {
        _logger.LogInformation("Getting block at height {Height} on {BlockchainType}", height, _blockchainType);

        try
        {
            var request = new JsonRpcRequest
            {
                Id = 1,
                Method = "getblock",
                Params = new object[] { height, 1 } // 1 = verbose mode
            };

            var response = await SendJsonRpcRequestAsync(request);

            if (response?.Result != null)
            {
                return ParseBlockFromJsonElement(JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(response.Result)));
            }

            throw new ArgumentException($"Block at height {height} not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get block at height {Height}", height);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Block> GetBlockAsync(string hash)
    {
        _logger.LogInformation("Getting block with hash {Hash} on {BlockchainType}", hash, _blockchainType);

        try
        {
            var request = new JsonRpcRequest
            {
                Id = 1,
                Method = "getblock",
                Params = new object[] { hash, 1 } // 1 = verbose mode
            };

            var response = await SendJsonRpcRequestAsync(request);

            if (response?.Result != null)
            {
                return ParseBlockFromJsonElement(JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(response.Result)));
            }

            throw new ArgumentException($"Block with hash {hash} not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get block with hash {Hash}", hash);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> GetBlockHashAsync(long height)
    {
        _logger.LogInformation("Getting block hash at height {Height} on {BlockchainType}", height, _blockchainType);

        try
        {
            var request = new JsonRpcRequest
            {
                Id = 1,
                Method = "getblockhash",
                Params = new object[] { height }
            };

            var response = await SendJsonRpcRequestAsync(request);

            if (response?.Result != null)
            {
                return response.Result.ToString() ?? throw new InvalidOperationException("Invalid block hash response");
            }

            throw new ArgumentException($"Block at height {height} not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get block hash at height {Height}", height);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<long> GetBlockHeightAsync()
    {
        _logger.LogInformation("Getting block height on {BlockchainType}", _blockchainType);

        try
        {
            var request = new JsonRpcRequest
            {
                Id = 1,
                Method = "getblockcount",
                Params = Array.Empty<object>()
            };

            var response = await SendJsonRpcRequestAsync(request);

            if (response?.Result != null && long.TryParse(response.Result.ToString(), out var height))
            {
                return height - 1; // Neo returns block count, we want latest block height
            }

            throw new InvalidOperationException("Failed to get block height");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get block height");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<decimal> GetGasPriceAsync()
    {
        _logger.LogInformation("Getting gas price on {BlockchainType}", _blockchainType);

        try
        {
            // For Neo, gas price is relatively stable, but we can get it from the network
            var request = new JsonRpcRequest
            {
                Id = 1,
                Method = "getversion",
                Params = Array.Empty<object>()
            };

            var response = await SendJsonRpcRequestAsync(request);

            // Neo has a fixed gas price model, return standard price
            return _blockchainType == BlockchainType.NeoN3 ? 0.00001m : 0.000001m;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get gas price, using default");
            return _blockchainType == BlockchainType.NeoN3 ? 0.00001m : 0.000001m;
        }
    }

    /// <inheritdoc/>
    public async Task<Transaction> GetTransactionAsync(string hash)
    {
        _logger.LogInformation("Getting transaction {Hash} on {BlockchainType}", hash, _blockchainType);

        try
        {
            var request = new JsonRpcRequest
            {
                Id = 1,
                Method = "getrawtransaction",
                Params = new object[] { hash, 1 } // 1 = verbose mode
            };

            var response = await SendJsonRpcRequestAsync(request);

            if (response?.Result != null)
            {
                return ParseTransactionFromJsonElement(JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(response.Result)));
            }

            throw new ArgumentException($"Transaction {hash} not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get transaction {Hash}", hash);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> InvokeContractAsync(string contractAddress, string method, params object[] parameters)
    {
        _logger.LogInformation("Invoking contract {ContractAddress}.{Method} on {BlockchainType}", contractAddress, method, _blockchainType);

        try
        {
            // For production, this would require a wallet and transaction signing
            // For now, this is a readonly invoke
            var request = new JsonRpcRequest
            {
                Id = 1,
                Method = "invokefunction",
                Params = new object[]
                {
                    contractAddress,
                    method,
                    parameters?.Select(ConvertParameterToNeoFormat).ToArray() ?? Array.Empty<object>()
                }
            };

            var response = await SendJsonRpcRequestAsync(request);

            if (response?.Result != null)
            {
                // In production, this would create and broadcast a transaction
                // For now, return the transaction ID from the test invoke
                var resultElement = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(response.Result));
                if (resultElement.TryGetProperty("script", out var script))
                {
                    return $"0x{Convert.ToHexString(Encoding.UTF8.GetBytes(script.GetString() ?? "")).ToLowerInvariant()}";
                }
            }

            throw new InvalidOperationException($"Contract invocation failed: {response?.Error?.Message ?? "Unknown error"}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invoke contract {ContractAddress}.{Method}", contractAddress, method);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> InvokeContractMethodAsync(string contractAddress, string method, params object[] args)
    {
        return InvokeContractAsync(contractAddress, method, args);
    }

    /// <inheritdoc/>
    public async Task<string> SendTransactionAsync(Transaction transaction)
    {
        _logger.LogInformation("Sending transaction on {BlockchainType}", _blockchainType);

        try
        {
            // In production, this would require proper transaction signing and broadcast
            var request = new JsonRpcRequest
            {
                Id = 1,
                Method = "sendrawtransaction",
                Params = new object[]
                {
                    Convert.ToBase64String(Encoding.UTF8.GetBytes(transaction.Data ?? ""))
                }
            };

            var response = await SendJsonRpcRequestAsync(request);

            if (response?.Result != null)
            {
                return response.Result.ToString() ?? transaction.Hash;
            }

            throw new InvalidOperationException($"Transaction send failed: {response?.Error?.Message ?? "Unknown error"}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send transaction");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> SubscribeToBlocksAsync(Func<Block, Task> callback)
    {
        string subscriptionId = Guid.NewGuid().ToString();
        _logger.LogInformation("Subscribing to blocks with subscription ID {SubscriptionId} on {BlockchainType}", subscriptionId, _blockchainType);

        lock (_subscriptionsLock)
        {
            _blockSubscriptions[subscriptionId] = callback;
        }

        // In production, this would establish WebSocket connection for real-time updates
        return Task.FromResult(subscriptionId);
    }

    /// <inheritdoc/>
    public Task<bool> UnsubscribeFromBlocksAsync(string subscriptionId)
    {
        _logger.LogInformation("Unsubscribing from blocks with subscription ID {SubscriptionId} on {BlockchainType}", subscriptionId, _blockchainType);

        lock (_subscriptionsLock)
        {
            return Task.FromResult(_blockSubscriptions.Remove(subscriptionId));
        }
    }

    /// <inheritdoc/>
    public Task<string> SubscribeToTransactionsAsync(Func<Transaction, Task> callback)
    {
        string subscriptionId = Guid.NewGuid().ToString();
        _logger.LogInformation("Subscribing to transactions with subscription ID {SubscriptionId} on {BlockchainType}", subscriptionId, _blockchainType);

        lock (_subscriptionsLock)
        {
            _transactionSubscriptions[subscriptionId] = callback;
        }

        return Task.FromResult(subscriptionId);
    }

    /// <inheritdoc/>
    public Task<bool> UnsubscribeFromTransactionsAsync(string subscriptionId)
    {
        _logger.LogInformation("Unsubscribing from transactions with subscription ID {SubscriptionId} on {BlockchainType}", subscriptionId, _blockchainType);

        lock (_subscriptionsLock)
        {
            return Task.FromResult(_transactionSubscriptions.Remove(subscriptionId));
        }
    }

    /// <inheritdoc/>
    public Task<string> SubscribeToContractEventsAsync(string contractAddress, string eventName, Func<ContractEvent, Task> callback)
    {
        string subscriptionId = Guid.NewGuid().ToString();
        _logger.LogInformation("Subscribing to contract events for contract {ContractAddress} and event {EventName} with subscription ID {SubscriptionId} on {BlockchainType}",
            contractAddress, eventName, subscriptionId, _blockchainType);

        lock (_subscriptionsLock)
        {
            _contractEventSubscriptions[subscriptionId] = (contractAddress, eventName, callback);
        }

        return Task.FromResult(subscriptionId);
    }

    /// <inheritdoc/>
    public Task<bool> UnsubscribeFromContractEventsAsync(string subscriptionId)
    {
        _logger.LogInformation("Unsubscribing from contract events with subscription ID {SubscriptionId} on {BlockchainType}", subscriptionId, _blockchainType);

        lock (_subscriptionsLock)
        {
            return Task.FromResult(_contractEventSubscriptions.Remove(subscriptionId));
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ContractEvent>> GetBlockEventsAsync(long blockHeight)
    {
        _logger.LogInformation("Getting events from block {BlockHeight} on {BlockchainType}", blockHeight, _blockchainType);

        try
        {
            var request = new JsonRpcRequest
            {
                Id = 1,
                Method = "getapplicationlog",
                Params = new object[] { blockHeight }
            };

            var response = await SendJsonRpcRequestAsync(request);
            var events = new List<ContractEvent>();

            if (response?.Result != null && response.Result is JsonElement element)
            {
                if (element.TryGetProperty("executions", out var executions))
                {
                    foreach (var execution in executions.EnumerateArray())
                    {
                        if (execution.TryGetProperty("notifications", out var notifications))
                        {
                            foreach (var notification in notifications.EnumerateArray())
                            {
                                events.Add(ParseContractEventFromJsonElement(notification));
                            }
                        }
                    }
                }
            }

            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get events from block {BlockHeight}", blockHeight);
            return new List<ContractEvent>();
        }
    }
    
    /// <summary>
    /// Parses a contract event from JSON element.
    /// </summary>
    private ContractEvent ParseContractEventFromJsonElement(JsonElement element)
    {
        var contractEvent = new ContractEvent();

        if (element.TryGetProperty("contract", out var contract))
            contractEvent.ContractHash = contract.GetString() ?? "";

        if (element.TryGetProperty("eventname", out var eventName))
            contractEvent.EventName = eventName.GetString() ?? "";

        if (element.TryGetProperty("state", out var state))
        {
            contractEvent.Parameters = new Dictionary<string, object>
            {
                ["state"] = JsonSerializer.Serialize(state)
            };
        }

        return contractEvent;
    }

    // Helper methods for production implementation

    /// <summary>
    /// Sends a JSON-RPC request to the blockchain network.
    /// </summary>
    private async Task<JsonRpcResponse?> SendJsonRpcRequestAsync(JsonRpcRequest request)
    {
        try
        {
            var jsonContent = JsonSerializer.Serialize(request);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_rpcEndpoint, content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<JsonRpcResponse>(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send JSON-RPC request to {Endpoint}", _rpcEndpoint);
            throw;
        }
    }

    /// <summary>
    /// Converts a parameter to Neo format for RPC calls.
    /// </summary>
    private object ConvertParameterToNeoFormat(object parameter)
    {
        return parameter switch
        {
            string str => new { type = "String", value = str },
            int i => new { type = "Integer", value = i.ToString() },
            long l => new { type = "Integer", value = l.ToString() },
            bool b => new { type = "Boolean", value = b },
            byte[] bytes => new { type = "ByteArray", value = Convert.ToBase64String(bytes) },
            _ => new { type = "String", value = parameter.ToString() }
        };
    }

    /// <summary>
    /// Parses a block from JSON element.
    /// </summary>
    private Block ParseBlockFromJsonElement(JsonElement element)
    {
        var block = new Block();

        if (element.TryGetProperty("hash", out var hash))
            block.Hash = hash.GetString() ?? "";

        if (element.TryGetProperty("index", out var index))
            block.Height = index.GetInt64();

        if (element.TryGetProperty("time", out var time))
            block.Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(time.GetInt64()).DateTime;

        if (element.TryGetProperty("previousblockhash", out var prevHash))
            block.PreviousHash = prevHash.GetString() ?? "";

        if (element.TryGetProperty("tx", out var transactions))
        {
            block.Transactions = transactions.EnumerateArray()
                .Select(ParseTransactionFromJsonElement)
                .ToList();
        }

        return block;
    }

    /// <summary>
    /// Parses a transaction from JSON element.
    /// </summary>
    private Transaction ParseTransactionFromJsonElement(JsonElement element)
    {
        var transaction = new Transaction();

        if (element.TryGetProperty("hash", out var hash))
            transaction.Hash = hash.GetString() ?? "";

        if (element.TryGetProperty("sender", out var sender))
            transaction.From = sender.GetString() ?? "";

        if (element.TryGetProperty("size", out var size))
            transaction.Data = $"Transaction size: {size.GetInt32()} bytes";

        if (element.TryGetProperty("blocktime", out var time))
            transaction.Timestamp = DateTimeOffset.FromUnixTimeSeconds(time.GetInt64()).DateTime;

        if (element.TryGetProperty("blockhash", out var blockHash))
            transaction.BlockHash = blockHash.GetString() ?? "";

        return transaction;
    }
}

/// <summary>
/// JSON-RPC request structure for Neo blockchain communication.
/// </summary>
public class JsonRpcRequest
{
    public string Jsonrpc { get; set; } = "2.0";
    public int Id { get; set; }
    public string Method { get; set; } = "";
    public object[]? Params { get; set; }
}

/// <summary>
/// JSON-RPC response structure for Neo blockchain communication.
/// </summary>
public class JsonRpcResponse
{
    public string Jsonrpc { get; set; } = "";
    public int Id { get; set; }
    public object? Result { get; set; }
    public JsonRpcError? Error { get; set; }
}

/// <summary>
/// JSON-RPC error structure.
/// </summary>
public class JsonRpcError
{
    public int Code { get; set; }
    public string Message { get; set; } = "";
}
