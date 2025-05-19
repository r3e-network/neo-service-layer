using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Shared.Blockchain;

namespace NeoServiceLayer.Tee.Host.Blockchain.Components
{
    /// <summary>
    /// Base class for blockchain service implementations.
    /// </summary>
    public abstract class BlockchainServiceBase
    {
        protected readonly ILogger _logger;
        protected readonly HttpClient _httpClient;
        protected readonly string _rpcUrl;
        protected readonly string _network;
        protected readonly JsonSerializerOptions _jsonOptions;
        protected readonly ConcurrentDictionary<string, BlockchainSubscription> _subscriptions;
        protected readonly SemaphoreSlim _semaphore;
        protected int _requestId = 1;
        protected bool _initialized;
        protected bool _disposed;

        /// <summary>
        /// Initializes a new instance of the BlockchainServiceBase class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="rpcUrl">The RPC URL.</param>
        /// <param name="network">The network name.</param>
        protected BlockchainServiceBase(ILogger logger, string rpcUrl, string network = "mainnet")
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _rpcUrl = rpcUrl ?? throw new ArgumentNullException(nameof(rpcUrl));
            _network = network ?? "mainnet";
            _httpClient = new HttpClient();
            _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            _subscriptions = new ConcurrentDictionary<string, BlockchainSubscription>();
            _semaphore = new SemaphoreSlim(1, 1);
            _initialized = false;
            _disposed = false;
        }

        /// <summary>
        /// Sends an RPC request to the blockchain node.
        /// </summary>
        /// <param name="method">The RPC method.</param>
        /// <param name="parameters">The RPC parameters.</param>
        /// <returns>The JSON response.</returns>
        protected async Task<JsonDocument> SendRpcRequestAsync(string method, object[] parameters)
        {
            var requestId = Interlocked.Increment(ref _requestId);
            var request = new
            {
                jsonrpc = "2.0",
                id = requestId,
                method,
                @params = parameters
            };

            var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(_rpcUrl, content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonDocument.Parse(responseJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending RPC request to {RpcUrl}: {Method}", _rpcUrl, method);
                throw;
            }
        }

        /// <summary>
        /// Checks if the service is connected to the blockchain node.
        /// </summary>
        /// <returns>True if connected, false otherwise.</returns>
        protected async Task<bool> IsConnectedAsync()
        {
            try
            {
                var response = await SendRpcRequestAsync("getversion", Array.Empty<object>());
                return response.RootElement.TryGetProperty("result", out _);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking connection to Neo N3 RPC server at {RpcUrl}", _rpcUrl);
                return false;
            }
        }

        /// <summary>
        /// Checks if the service is initialized.
        /// </summary>
        protected void CheckInitialized()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Blockchain service is not initialized");
            }
        }

        /// <summary>
        /// Checks if the service is disposed.
        /// </summary>
        protected void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
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
                    _httpClient.Dispose();
                    _semaphore.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
