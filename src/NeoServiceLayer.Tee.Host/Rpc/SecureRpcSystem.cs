using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Shared.Models.Rpc;
using NeoServiceLayer.Tee.Host.Events;
using NeoServiceLayer.Tee.Host.Exceptions;
using NeoServiceLayer.Tee.Host.Logging;
using NeoServiceLayer.Tee.Shared.Interfaces;

namespace NeoServiceLayer.Tee.Host.Rpc
{
    /// <summary>
    /// Provides a secure remote procedure call (RPC) system for enclave communication.
    /// </summary>
    public class SecureRpcSystem : ISecureRpcSystem, IDisposable
    {
        private readonly ILogger<SecureRpcSystem> _logger;
        private readonly IOcclumInterface _occlumInterface;
        private readonly IEnclaveEventSystem _eventSystem;
        private readonly ISecureLogger _secureLogger;
        private readonly SecureRpcOptions _options;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<RpcResponse>> _pendingRequests;
        private readonly ConcurrentDictionary<string, RpcMethodHandler> _methodHandlers;
        private readonly IEnclaveEventSubscription _requestSubscription;
        private readonly IEnclaveEventSubscription _responseSubscription;
        private readonly SemaphoreSlim _requestSemaphore;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecureRpcSystem"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for logging information and errors.</param>
        /// <param name="occlumInterface">The Occlum interface to use for secure operations.</param>
        /// <param name="eventSystem">The event system to use for RPC communication.</param>
        /// <param name="secureLogger">The secure logger to use for logging.</param>
        /// <param name="options">The options for the RPC system.</param>
        public SecureRpcSystem(
            ILogger<SecureRpcSystem> logger,
            IOcclumInterface occlumInterface,
            IEnclaveEventSystem eventSystem,
            ISecureLogger secureLogger,
            SecureRpcOptions options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _occlumInterface = occlumInterface ?? throw new ArgumentNullException(nameof(occlumInterface));
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _secureLogger = secureLogger ?? throw new ArgumentNullException(nameof(secureLogger));
            _options = options ?? new SecureRpcOptions();
            _pendingRequests = new ConcurrentDictionary<string, TaskCompletionSource<RpcResponse>>();
            _methodHandlers = new ConcurrentDictionary<string, RpcMethodHandler>();
            _requestSemaphore = new SemaphoreSlim(_options.MaxConcurrentRequests, _options.MaxConcurrentRequests);
            _disposed = false;

            // Subscribe to RPC request events
            _requestSubscription = _eventSystem.Subscribe(_options.RequestEventType, HandleRpcRequestEventAsync);

            // Subscribe to RPC response events
            _responseSubscription = _eventSystem.Subscribe(_options.ResponseEventType, HandleRpcResponseEventAsync);

            _logger.LogInformation("Secure RPC system initialized");
        }

        /// <summary>
        /// Calls a remote procedure.
        /// </summary>
        /// <param name="method">The name of the method to call.</param>
        /// <param name="parameters">The parameters for the method.</param>
        /// <param name="timeout">The timeout for the call in milliseconds.</param>
        /// <returns>The response from the remote procedure.</returns>
        public async Task<RpcResponse> CallAsync(string method, object parameters, int? timeout = null)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(method))
            {
                throw new ArgumentException("Method cannot be null or empty", nameof(method));
            }

            _logger.LogDebug("Calling RPC method {Method}", method);

            // Create a cancellation token source with the specified timeout
            int actualTimeout = timeout ?? _options.DefaultTimeoutMs;
            using var cancellationTokenSource = new CancellationTokenSource(actualTimeout);

            try
            {
                // Acquire a semaphore to limit concurrent requests
                await _requestSemaphore.WaitAsync(cancellationTokenSource.Token);
                try
                {
                    // Create the request
                    var request = new RpcRequest
                    {
                        Id = Guid.NewGuid().ToString(),
                        Method = method,
                        Parameters = parameters,
                        Timestamp = DateTime.UtcNow
                    };

                    // Create a task completion source for the response
                    var tcs = new TaskCompletionSource<RpcResponse>();
                    _pendingRequests[request.Id] = tcs;

                    try
                    {
                        // Publish the request as an event
                        await _eventSystem.PublishAsync(_options.RequestEventType, request);

                        // Wait for the response
                        var responseTask = tcs.Task;
                        var completedTask = await Task.WhenAny(responseTask, Task.Delay(actualTimeout, cancellationTokenSource.Token));

                        if (completedTask == responseTask)
                        {
                            // The response was received
                            var response = await responseTask;
                            _logger.LogDebug("Received response for RPC method {Method}", method);
                            return response;
                        }
                        else
                        {
                            // The request timed out
                            _logger.LogWarning("RPC method {Method} timed out after {Timeout}ms", method, actualTimeout);
                            throw new RpcTimeoutException($"RPC method {method} timed out after {actualTimeout}ms");
                        }
                    }
                    finally
                    {
                        // Remove the pending request
                        _pendingRequests.TryRemove(request.Id, out _);
                    }
                }
                finally
                {
                    // Release the semaphore
                    _requestSemaphore.Release();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("RPC method {Method} was canceled", method);
                throw new RpcTimeoutException($"RPC method {method} was canceled");
            }
            catch (Exception ex) when (!(ex is RpcException))
            {
                _logger.LogError(ex, "Error calling RPC method {Method}", method);
                throw new RpcException($"Error calling RPC method {method}", ex);
            }
        }

        /// <summary>
        /// Registers a method handler.
        /// </summary>
        /// <param name="method">The name of the method to register.</param>
        /// <param name="handler">The handler for the method.</param>
        public void RegisterMethod(string method, Func<RpcRequest, Task<RpcResponse>> handler)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(method))
            {
                throw new ArgumentException("Method cannot be null or empty", nameof(method));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _logger.LogDebug("Registering RPC method {Method}", method);

            try
            {
                // Create a method handler
                var methodHandler = new RpcMethodHandler
                {
                    Method = method,
                    Handler = handler
                };

                // Register the method handler
                _methodHandlers[method] = methodHandler;

                _logger.LogInformation("Registered RPC method {Method}", method);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering RPC method {Method}", method);
                throw new RpcException($"Error registering RPC method {method}", ex);
            }
        }

        /// <summary>
        /// Unregisters a method handler.
        /// </summary>
        /// <param name="method">The name of the method to unregister.</param>
        /// <returns>True if the method was unregistered, false otherwise.</returns>
        public bool UnregisterMethod(string method)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(method))
            {
                throw new ArgumentException("Method cannot be null or empty", nameof(method));
            }

            _logger.LogDebug("Unregistering RPC method {Method}", method);

            try
            {
                // Unregister the method handler
                bool removed = _methodHandlers.TryRemove(method, out _);

                if (removed)
                {
                    _logger.LogInformation("Unregistered RPC method {Method}", method);
                }
                else
                {
                    _logger.LogWarning("RPC method {Method} not found for unregistration", method);
                }

                return removed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unregistering RPC method {Method}", method);
                throw new RpcException($"Error unregistering RPC method {method}", ex);
            }
        }

        /// <summary>
        /// Gets all registered methods.
        /// </summary>
        /// <returns>A list of registered methods.</returns>
        public IReadOnlyList<string> GetRegisteredMethods()
        {
            CheckDisposed();

            return new List<string>(_methodHandlers.Keys);
        }

        /// <summary>
        /// Disposes the RPC system.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the RPC system.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Unsubscribe from events
                    _requestSubscription?.Dispose();
                    _responseSubscription?.Dispose();

                    // Dispose the semaphore
                    _requestSemaphore?.Dispose();

                    // Complete all pending requests with an error
                    foreach (var pendingRequest in _pendingRequests)
                    {
                        pendingRequest.Value.TrySetException(new ObjectDisposedException(nameof(SecureRpcSystem)));
                    }
                    _pendingRequests.Clear();
                }

                _disposed = true;
            }
        }

        private async Task HandleRpcRequestEventAsync(EnclaveEvent enclaveEvent)
        {
            if (enclaveEvent == null || string.IsNullOrEmpty(enclaveEvent.Data))
            {
                return;
            }

            try
            {
                // Deserialize the request
                var request = JsonSerializer.Deserialize<RpcRequest>(enclaveEvent.Data);
                if (request == null)
                {
                    _logger.LogWarning("Received invalid RPC request");
                    return;
                }

                _logger.LogDebug("Handling RPC request for method {Method}", request.Method);

                // Find the method handler
                if (!_methodHandlers.TryGetValue(request.Method, out var methodHandler))
                {
                    _logger.LogWarning("RPC method {Method} not found", request.Method);

                    // Create an error response
                    var errorResponse = new RpcResponse
                    {
                        Id = request.Id,
                        Error = $"Method not found: {request.Method}",
                        Timestamp = DateTime.UtcNow
                    };

                    // Publish the response as an event
                    await _eventSystem.PublishAsync(_options.ResponseEventType, errorResponse);
                    return;
                }

                try
                {
                    // Call the method handler
                    var response = await methodHandler.Handler(request);

                    // Set the request ID and timestamp
                    response.Id = request.Id;
                    if (response.Timestamp == default)
                    {
                        response.Timestamp = DateTime.UtcNow;
                    }

                    // Publish the response as an event
                    await _eventSystem.PublishAsync(_options.ResponseEventType, response);

                    _logger.LogDebug("Handled RPC request for method {Method}", request.Method);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling RPC request for method {Method}", request.Method);

                    // Create an error response
                    var errorResponse = new RpcResponse
                    {
                        Id = request.Id,
                        Error = $"Error handling request: {ex.Message}",
                        Timestamp = DateTime.UtcNow
                    };

                    // Publish the response as an event
                    await _eventSystem.PublishAsync(_options.ResponseEventType, errorResponse);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RPC request event");
            }
        }

        private async Task HandleRpcResponseEventAsync(EnclaveEvent enclaveEvent)
        {
            if (enclaveEvent == null || string.IsNullOrEmpty(enclaveEvent.Data))
            {
                return;
            }

            try
            {
                // Deserialize the response
                var response = JsonSerializer.Deserialize<RpcResponse>(enclaveEvent.Data);
                if (response == null || string.IsNullOrEmpty(response.Id))
                {
                    _logger.LogWarning("Received invalid RPC response");
                    return;
                }

                _logger.LogDebug("Handling RPC response for request {RequestId}", response.Id);

                // Find the pending request
                if (_pendingRequests.TryGetValue(response.Id, out var tcs))
                {
                    // Complete the task with the response
                    tcs.TrySetResult(response);
                    _logger.LogDebug("Completed pending RPC request {RequestId}", response.Id);
                }
                else
                {
                    _logger.LogWarning("Received RPC response for unknown request {RequestId}", response.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RPC response event");
            }
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SecureRpcSystem));
            }
        }
    }
}
