using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Shared.Blockchain;

namespace NeoServiceLayer.Tee.Host.Blockchain.Components
{
    /// <summary>
    /// Service for sending blockchain transactions.
    /// </summary>
    public class BlockchainTransactionService : BlockchainContractService
    {
        private readonly string _walletPath;
        private readonly string _walletPassword;

        /// <summary>
        /// Initializes a new instance of the BlockchainTransactionService class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="rpcUrl">The RPC URL.</param>
        /// <param name="walletPath">The wallet path.</param>
        /// <param name="walletPassword">The wallet password.</param>
        /// <param name="network">The network name.</param>
        public BlockchainTransactionService(
            ILogger logger,
            string rpcUrl,
            string walletPath,
            string walletPassword,
            string network = "mainnet")
            : base(logger, rpcUrl, network)
        {
            _walletPath = walletPath ?? throw new ArgumentNullException(nameof(walletPath));
            _walletPassword = walletPassword ?? throw new ArgumentNullException(nameof(walletPassword));
        }

        /// <summary>
        /// Invokes a contract.
        /// </summary>
        /// <param name="contractHash">The contract hash.</param>
        /// <param name="operation">The operation.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>The transaction hash.</returns>
        public async Task<string> InvokeContractAsync(string contractHash, string operation, params object[] args)
        {
            CheckDisposed();
            CheckInitialized();

            if (string.IsNullOrEmpty(contractHash))
            {
                throw new ArgumentException("Contract hash cannot be null or empty", nameof(contractHash));
            }

            if (string.IsNullOrEmpty(operation))
            {
                throw new ArgumentException("Operation cannot be null or empty", nameof(operation));
            }

            try
            {
                // First, build the script
                var buildScriptResponse = await SendRpcRequestAsync("invokefunction", BuildInvokeFunctionParams(contractHash, operation, args));
                var script = buildScriptResponse.GetProperty("result").GetProperty("script").GetString();

                // Then, invoke the script
                var invokeResponse = await SendRpcRequestAsync("sendrawtransaction", new object[] { script });
                var txHash = invokeResponse.GetProperty("result").GetProperty("hash").GetString();

                _logger.LogDebug("Invoked Neo N3 contract {ContractHash}.{Operation} with transaction hash {TxHash}", contractHash, operation, txHash);

                return txHash;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invoking Neo N3 contract {ContractHash}.{Operation}", contractHash, operation);
                throw;
            }
        }

        /// <summary>
        /// Tests invoking a contract.
        /// </summary>
        /// <param name="contractHash">The contract hash.</param>
        /// <param name="operation">The operation.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>The invocation result.</returns>
        public async Task<ContractInvocationResult> TestInvokeContractAsync(string contractHash, string operation, params object[] args)
        {
            CheckDisposed();
            CheckInitialized();

            if (string.IsNullOrEmpty(contractHash))
            {
                throw new ArgumentException("Contract hash cannot be null or empty", nameof(contractHash));
            }

            if (string.IsNullOrEmpty(operation))
            {
                throw new ArgumentException("Operation cannot be null or empty", nameof(operation));
            }

            try
            {
                var response = await SendRpcRequestAsync("invokefunction", BuildInvokeFunctionParams(contractHash, operation, args));
                var resultJson = response.GetProperty("result");

                var result = new ContractInvocationResult
                {
                    State = resultJson.GetProperty("state").GetString(),
                    GasConsumed = resultJson.GetProperty("gasconsumed").GetString(),
                    Exception = resultJson.TryGetProperty("exception", out var exception) ? exception.GetString() : null
                };

                // Extract stack
                var stack = new List<object>();
                foreach (var item in resultJson.GetProperty("stack").EnumerateArray())
                {
                    stack.Add(new
                    {
                        Type = item.GetProperty("type").GetString(),
                        Value = item.GetProperty("value").GetString()
                    });
                }
                result.Stack = stack.ToArray();

                // Extract notifications
                var notifications = new List<object>();
                if (resultJson.TryGetProperty("notifications", out var notificationsJson))
                {
                    foreach (var notification in notificationsJson.EnumerateArray())
                    {
                        notifications.Add(new
                        {
                            Contract = notification.GetProperty("contract").GetString(),
                            EventName = notification.GetProperty("eventname").GetString(),
                            State = notification.GetProperty("state").GetRawText()
                        });
                    }
                }
                result.Notifications = notifications.ToArray();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error test invoking Neo N3 contract {ContractHash}.{Operation}", contractHash, operation);
                throw;
            }
        }

        /// <summary>
        /// Sends a callback transaction to a contract.
        /// </summary>
        /// <param name="contractHash">The contract hash.</param>
        /// <param name="method">The method.</param>
        /// <param name="functionId">The function ID.</param>
        /// <param name="result">The result.</param>
        /// <returns>The transaction hash.</returns>
        public async Task<string> SendCallbackTransactionAsync(string contractHash, string method, string functionId, string result)
        {
            CheckDisposed();
            CheckInitialized();

            if (string.IsNullOrEmpty(contractHash))
            {
                throw new ArgumentException("Contract hash cannot be null or empty", nameof(contractHash));
            }

            if (string.IsNullOrEmpty(method))
            {
                throw new ArgumentException("Method cannot be null or empty", nameof(method));
            }

            if (string.IsNullOrEmpty(functionId))
            {
                throw new ArgumentException("Function ID cannot be null or empty", nameof(functionId));
            }

            try
            {
                _logger.LogInformation("Sending callback to contract {ContractHash}.{Method} with result for function {FunctionId}",
                    contractHash, method, functionId);

                // Invoke the contract method
                var txHash = await InvokeContractAsync(contractHash, method, functionId, result);

                _logger.LogInformation("Callback sent to contract {ContractHash}.{Method} with transaction hash {TxHash}",
                    contractHash, method, txHash);

                return txHash;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending callback to contract {ContractHash}.{Method}", contractHash, method);
                throw;
            }
        }
    }
}
