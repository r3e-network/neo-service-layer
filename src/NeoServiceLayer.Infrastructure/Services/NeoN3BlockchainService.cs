using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;
using Task = System.Threading.Tasks.Task;

namespace NeoServiceLayer.Infrastructure.Services
{
    /// <summary>
    /// Implementation of the Neo N3 blockchain service.
    /// </summary>
    public class NeoN3BlockchainService : INeoN3BlockchainService
    {
        private readonly ILogger<NeoN3BlockchainService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _rpcUrl;
        private readonly string _walletPath;
        private readonly string _walletPassword;
        private readonly JsonSerializerOptions _jsonOptions;
        private int _requestId = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="NeoN3BlockchainService"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="rpcUrl">The RPC URL of the Neo N3 node.</param>
        /// <param name="walletPath">The path to the wallet file.</param>
        /// <param name="walletPassword">The wallet password.</param>
        public NeoN3BlockchainService(
            ILogger<NeoN3BlockchainService> logger,
            string rpcUrl,
            string walletPath,
            string walletPassword)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _rpcUrl = rpcUrl ?? throw new ArgumentNullException(nameof(rpcUrl));
            _walletPath = walletPath ?? throw new ArgumentNullException(nameof(walletPath));
            _walletPassword = walletPassword ?? throw new ArgumentNullException(nameof(walletPassword));

            _httpClient = new HttpClient();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            _logger.LogInformation("Neo N3 blockchain service initialized with RPC URL: {RpcUrl}", _rpcUrl);
        }

        /// <inheritdoc/>
        public async Task<string> GetBlockchainHeightAsync()
        {
            _logger.LogInformation("Getting blockchain height");

            try
            {
                var response = await SendRpcRequestAsync("getblockcount", Array.Empty<object>());
                var height = response.GetProperty("result").GetInt32();

                _logger.LogInformation("Blockchain height: {Height}", height);

                return height.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blockchain height");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<BlockchainTransaction> GetTransactionAsync(string txHash)
        {
            _logger.LogInformation("Getting transaction {TxHash}", txHash);

            if (string.IsNullOrEmpty(txHash))
            {
                throw new ArgumentException("Transaction hash is required", nameof(txHash));
            }

            try
            {
                var response = await SendRpcRequestAsync("getrawtransaction", new object[] { txHash, true });
                var txJson = response.GetProperty("result");

                var tx = new BlockchainTransaction
                {
                    Hash = txJson.GetProperty("hash").GetString(),
                    Size = (uint)txJson.GetProperty("size").GetInt32(),
                    Version = (byte)txJson.GetProperty("version").GetInt32(),
                    Nonce = (uint)txJson.GetProperty("nonce").GetInt32(),
                    Sender = txJson.GetProperty("sender").GetString(),
                    SystemFee = txJson.GetProperty("sysfee").GetString(),
                    NetworkFee = txJson.GetProperty("netfee").GetString(),
                    Script = txJson.GetProperty("script").GetString(),
                    BlockIndex = txJson.TryGetProperty("blockindex", out var blockIndex) ? (uint)blockIndex.GetInt32() : 0,
                    BlockTime = txJson.TryGetProperty("blocktime", out var blockTime) ? (ulong)blockTime.GetInt64() : 0
                };

                _logger.LogInformation("Transaction {TxHash} retrieved successfully", txHash);

                return tx;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction {TxHash}", txHash);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string> InvokeContractAsync(string scriptHash, string operation, params object[] args)
        {
            _logger.LogInformation("Invoking contract {ScriptHash}.{Operation}", scriptHash, operation);

            if (string.IsNullOrEmpty(scriptHash))
            {
                throw new ArgumentException("Script hash is required", nameof(scriptHash));
            }

            if (string.IsNullOrEmpty(operation))
            {
                throw new ArgumentException("Operation is required", nameof(operation));
            }

            try
            {
                // First, build the script
                var buildScriptResponse = await SendRpcRequestAsync("invokefunction", BuildInvokeFunctionParams(scriptHash, operation, args));
                var script = buildScriptResponse.GetProperty("result").GetProperty("script").GetString();

                // Then, invoke the script
                var invokeResponse = await SendRpcRequestAsync("sendrawtransaction", new object[] { script });
                var txHash = invokeResponse.GetProperty("result").GetProperty("hash").GetString();

                _logger.LogInformation("Contract {ScriptHash}.{Operation} invoked successfully with transaction hash {TxHash}", scriptHash, operation, txHash);

                return txHash;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invoking contract {ScriptHash}.{Operation}", scriptHash, operation);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ContractInvocationResult> TestInvokeContractAsync(string scriptHash, string operation, params object[] args)
        {
            _logger.LogInformation("Test invoking contract {ScriptHash}.{Operation}", scriptHash, operation);

            if (string.IsNullOrEmpty(scriptHash))
            {
                throw new ArgumentException("Script hash is required", nameof(scriptHash));
            }

            if (string.IsNullOrEmpty(operation))
            {
                throw new ArgumentException("Operation is required", nameof(operation));
            }

            try
            {
                var response = await SendRpcRequestAsync("invokefunction", BuildInvokeFunctionParams(scriptHash, operation, args));
                var resultJson = response.GetProperty("result");

                var result = new ContractInvocationResult
                {
                    State = resultJson.GetProperty("state").GetString(),
                    GasConsumed = resultJson.GetProperty("gasconsumed").ToString(),
                    Stack = new object[]{}
                };

                if (resultJson.TryGetProperty("stack", out var stackJson))
                {
                    var stackItems = new List<object>();
                    foreach (var item in stackJson.EnumerateArray())
                    {
                        stackItems.Add(new
                        {
                            Type = item.GetProperty("type").GetString(),
                            Value = item.GetProperty("value").GetString()
                        });
                    }
                    result.Stack = stackItems.ToArray();
                }

                if (resultJson.TryGetProperty("exception", out var exceptionJson) && !exceptionJson.ValueKind.Equals(JsonValueKind.Null))
                {
                    result.Exception = exceptionJson.GetString();
                }

                _logger.LogInformation("Contract {ScriptHash}.{Operation} test invoked successfully with state {State}", scriptHash, operation, result.State);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error test invoking contract {ScriptHash}.{Operation}", scriptHash, operation);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<BlockchainEvent[]> GetContractEventsAsync(string scriptHash, int fromBlock = 0, int count = 100)
        {
            _logger.LogInformation("Getting events for contract {ScriptHash} from block {FromBlock} with count {Count}", scriptHash, fromBlock, count);

            if (string.IsNullOrEmpty(scriptHash))
            {
                throw new ArgumentException("Script hash is required", nameof(scriptHash));
            }

            try
            {
                var response = await SendRpcRequestAsync("getapplicationlog", new object[] { scriptHash, fromBlock, count });
                var eventsJson = response.GetProperty("result").GetProperty("executions");

                var events = new List<BlockchainEvent>();

                foreach (var execution in eventsJson.EnumerateArray())
                {
                    var txHash = execution.GetProperty("txid").GetString();
                    var notifications = execution.GetProperty("notifications");

                    foreach (var notification in notifications.EnumerateArray())
                    {
                        var contract = notification.GetProperty("contract").GetString();
                        if (contract.Equals(scriptHash, StringComparison.OrdinalIgnoreCase))
                        {
                            var eventName = notification.GetProperty("eventname").GetString();
                            var state = notification.GetProperty("state").GetProperty("value");

                            var stateValues = new List<object>();
                            foreach (var item in state.EnumerateArray())
                            {
                                stateValues.Add(item.GetProperty("value").GetString());
                            }

                            events.Add(new BlockchainEvent
                            {
                                TxHash = txHash,
                                BlockIndex = (uint)fromBlock,
                                EventName = eventName,
                                State = stateValues.ToArray()
                            });
                        }
                    }
                }

                _logger.LogInformation("Retrieved {EventCount} events for contract {ScriptHash}", events.Count, scriptHash);

                return events.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting events for contract {ScriptHash}", scriptHash);
                throw;
            }
        }

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

        private object[] BuildInvokeFunctionParams(string scriptHash, string operation, object[] args)
        {
            var parameters = new List<object> { scriptHash, operation };

            if (args != null && args.Length > 0)
            {
                var formattedArgs = new List<object>();
                foreach (var arg in args)
                {
                    formattedArgs.Add(FormatArgument(arg));
                }
                parameters.Add(formattedArgs.ToArray());
            }
            else
            {
                parameters.Add(Array.Empty<object>());
            }

            return parameters.ToArray();
        }

        private object FormatArgument(object arg)
        {
            if (arg == null)
            {
                return new { type = "Null", value = "" };
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
    }
}
