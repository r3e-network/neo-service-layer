using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Host.Storage;
using NeoServiceLayer.Tee.Shared.Blockchain;
using NeoServiceLayer.Tee.Shared.Storage;

namespace NeoServiceLayer.Tee.Host.Blockchain
{
    /// <summary>
    /// Implementation of the blockchain event listener.
    /// </summary>
    public class BlockchainEventListener : IBlockchainEventListener
    {
        private readonly ILogger<BlockchainEventListener> _logger;
        private readonly IBlockchainService _blockchainService;
        private readonly IStorageManager _storageManager;
        private readonly ConcurrentDictionary<string, BlockchainSubscription> _subscriptions;
        private readonly SemaphoreSlim _semaphore;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly List<Task> _runningTasks;
        private readonly Stopwatch _pollStopwatch;
        private int _pollingIntervalMs;
        private int _maxBlocksPerPoll;
        private int _requiredConfirmations;
        private ulong _lastProcessedBlockHeight;
        private ulong _eventsProcessedCount;
        private ulong _blocksProcessedCount;
        private ulong _errorCount;
        private string _lastErrorMessage;
        private DateTime? _lastErrorTimestamp;
        private DateTime? _startTimestamp;
        private DateTime? _stopTimestamp;
        private DateTime? _lastPollTimestamp;
        private long _lastPollDurationMs;
        private long _totalPollDurationMs;
        private ulong _pollCount;
        private bool _isRunning;
        private bool _initialized;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the BlockchainEventListener class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="blockchainService">The blockchain service.</param>
        /// <param name="storageManager">The storage manager.</param>
        public BlockchainEventListener(
            ILogger<BlockchainEventListener> logger,
            IBlockchainService blockchainService,
            IStorageManager storageManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _blockchainService = blockchainService ?? throw new ArgumentNullException(nameof(blockchainService));
            _storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
            _subscriptions = new ConcurrentDictionary<string, BlockchainSubscription>();
            _semaphore = new SemaphoreSlim(1, 1);
            _cancellationTokenSource = new CancellationTokenSource();
            _runningTasks = new List<Task>();
            _pollStopwatch = new Stopwatch();
            _pollingIntervalMs = 15000; // 15 seconds
            _maxBlocksPerPoll = 10;
            _requiredConfirmations = 1;
            _lastProcessedBlockHeight = 0;
            _eventsProcessedCount = 0;
            _blocksProcessedCount = 0;
            _errorCount = 0;
            _lastErrorMessage = null;
            _lastErrorTimestamp = null;
            _startTimestamp = null;
            _stopTimestamp = null;
            _lastPollTimestamp = null;
            _lastPollDurationMs = 0;
            _totalPollDurationMs = 0;
            _pollCount = 0;
            _isRunning = false;
            _initialized = false;
            _disposed = false;
        }

        /// <inheritdoc/>
        public async Task<bool> InitializeAsync()
        {
            CheckDisposed();

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    if (_initialized)
                    {
                        return true;
                    }

                    // Initialize blockchain service
                    if (!await _blockchainService.InitializeAsync())
                    {
                        _logger.LogError("Failed to initialize blockchain service");
                        return false;
                    }

                    // Initialize storage provider for subscriptions
                    var storageProvider = _storageManager.GetProvider("blockchain_subscriptions");
                    if (storageProvider == null)
                    {
                        // Create a new storage provider for subscriptions
                        storageProvider = await _storageManager.CreateProviderAsync(
                            "blockchain_subscriptions",
                            StorageProviderType.File,
                            new FileStorageOptions { StorageDirectory = "blockchain_subscriptions" });

                        if (storageProvider == null)
                        {
                            _logger.LogError("Failed to create storage provider for blockchain subscriptions");
                            return false;
                        }
                    }

                    // Load subscriptions from storage
                    if (!await LoadSubscriptionsAsync())
                    {
                        _logger.LogError("Failed to load blockchain subscriptions from storage");
                        return false;
                    }

                    // Load last processed block height from storage
                    if (!await LoadLastProcessedBlockHeightAsync())
                    {
                        _logger.LogError("Failed to load last processed block height from storage");
                        return false;
                    }

                    _initialized = true;
                    _logger.LogInformation("Blockchain event listener initialized with {SubscriptionCount} subscriptions", _subscriptions.Count);
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing blockchain event listener");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> StartAsync()
        {
            CheckDisposed();
            CheckInitialized();

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    if (_isRunning)
                    {
                        return true;
                    }

                    // Start the polling task
                    var pollingTask = Task.Run(() => PollBlockchainAsync(_cancellationTokenSource.Token));
                    _runningTasks.Add(pollingTask);

                    _isRunning = true;
                    _startTimestamp = DateTime.UtcNow;
                    _stopTimestamp = null;

                    _logger.LogInformation("Blockchain event listener started");
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting blockchain event listener");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> StopAsync()
        {
            CheckDisposed();
            CheckInitialized();

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    if (!_isRunning)
                    {
                        return true;
                    }

                    // Cancel the polling task
                    _cancellationTokenSource.Cancel();

                    // Wait for all tasks to complete
                    await Task.WhenAll(_runningTasks);
                    _runningTasks.Clear();

                    _isRunning = false;
                    _stopTimestamp = DateTime.UtcNow;

                    _logger.LogInformation("Blockchain event listener stopped");
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping blockchain event listener");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<string> SubscribeToContractEventsAsync(string contractHash, string eventName, Func<BlockchainEvent, Task> callback, ulong fromBlock = 0)
        {
            CheckDisposed();
            CheckInitialized();

            if (string.IsNullOrEmpty(contractHash))
            {
                throw new ArgumentException("Contract hash cannot be null or empty", nameof(contractHash));
            }

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            try
            {
                // Create a new subscription
                var subscription = new BlockchainSubscription
                {
                    ContractHash = contractHash,
                    EventName = eventName,
                    FromBlock = fromBlock,
                    LastProcessedBlock = fromBlock
                };

                // Store the callback in the metadata
                subscription.Metadata["callback"] = callback;

                // Add the subscription
                _subscriptions[subscription.Id] = subscription;

                // Save the subscription to storage
                await SaveSubscriptionAsync(subscription);

                _logger.LogInformation("Subscribed to contract {ContractHash} events{EventName} from block {FromBlock}",
                    contractHash, eventName != null ? " with name " + eventName : "", fromBlock);

                return subscription.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to contract {ContractHash} events", contractHash);
                throw;
            }
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
                    // Delete the subscription from storage
                    await DeleteSubscriptionAsync(subscriptionId);

                    _logger.LogInformation("Unsubscribed from contract {ContractHash} events", subscription.ContractHash);
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
        public IBlockchainService GetBlockchainService()
        {
            CheckDisposed();
            CheckInitialized();

            return _blockchainService;
        }

        /// <inheritdoc/>
        public BlockchainType GetBlockchainType()
        {
            CheckDisposed();
            CheckInitialized();

            return _blockchainService.GetBlockchainType();
        }

        /// <inheritdoc/>
        public string GetBlockchainNetwork()
        {
            CheckDisposed();
            CheckInitialized();

            return _blockchainService.GetBlockchainNetwork();
        }

        /// <inheritdoc/>
        public int GetPollingIntervalMs()
        {
            CheckDisposed();
            CheckInitialized();

            return _pollingIntervalMs;
        }

        /// <inheritdoc/>
        public void SetPollingIntervalMs(int intervalMs)
        {
            CheckDisposed();
            CheckInitialized();

            if (intervalMs <= 0)
            {
                throw new ArgumentException("Polling interval must be greater than zero", nameof(intervalMs));
            }

            _pollingIntervalMs = intervalMs;
        }

        /// <inheritdoc/>
        public int GetMaxBlocksPerPoll()
        {
            CheckDisposed();
            CheckInitialized();

            return _maxBlocksPerPoll;
        }

        /// <inheritdoc/>
        public void SetMaxBlocksPerPoll(int maxBlocks)
        {
            CheckDisposed();
            CheckInitialized();

            if (maxBlocks <= 0)
            {
                throw new ArgumentException("Max blocks per poll must be greater than zero", nameof(maxBlocks));
            }

            _maxBlocksPerPoll = maxBlocks;
        }

        /// <inheritdoc/>
        public int GetRequiredConfirmations()
        {
            CheckDisposed();
            CheckInitialized();

            return _requiredConfirmations;
        }

        /// <inheritdoc/>
        public void SetRequiredConfirmations(int confirmations)
        {
            CheckDisposed();
            CheckInitialized();

            if (confirmations < 0)
            {
                throw new ArgumentException("Required confirmations must be greater than or equal to zero", nameof(confirmations));
            }

            _requiredConfirmations = confirmations;
        }

        /// <inheritdoc/>
        public bool IsRunning()
        {
            CheckDisposed();
            CheckInitialized();

            return _isRunning;
        }

        /// <inheritdoc/>
        public async Task<ulong> GetLastProcessedBlockHeightAsync()
        {
            CheckDisposed();
            CheckInitialized();

            return _lastProcessedBlockHeight;
        }

        /// <inheritdoc/>
        public async Task<bool> SetLastProcessedBlockHeightAsync(ulong height)
        {
            CheckDisposed();
            CheckInitialized();

            try
            {
                _lastProcessedBlockHeight = height;
                return await SaveLastProcessedBlockHeightAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting last processed block height to {Height}", height);
                return false;
            }
        }

        /// <inheritdoc/>
        public ulong GetEventsProcessedCount()
        {
            CheckDisposed();
            CheckInitialized();

            return _eventsProcessedCount;
        }

        /// <inheritdoc/>
        public ulong GetBlocksProcessedCount()
        {
            CheckDisposed();
            CheckInitialized();

            return _blocksProcessedCount;
        }

        /// <inheritdoc/>
        public ulong GetErrorCount()
        {
            CheckDisposed();
            CheckInitialized();

            return _errorCount;
        }

        /// <inheritdoc/>
        public string GetLastErrorMessage()
        {
            CheckDisposed();
            CheckInitialized();

            return _lastErrorMessage;
        }

        /// <inheritdoc/>
        public DateTime? GetLastErrorTimestamp()
        {
            CheckDisposed();
            CheckInitialized();

            return _lastErrorTimestamp;
        }

        /// <inheritdoc/>
        public DateTime? GetStartTimestamp()
        {
            CheckDisposed();
            CheckInitialized();

            return _startTimestamp;
        }

        /// <inheritdoc/>
        public DateTime? GetStopTimestamp()
        {
            CheckDisposed();
            CheckInitialized();

            return _stopTimestamp;
        }

        /// <inheritdoc/>
        public DateTime? GetLastPollTimestamp()
        {
            CheckDisposed();
            CheckInitialized();

            return _lastPollTimestamp;
        }

        /// <inheritdoc/>
        public long GetLastPollDurationMs()
        {
            CheckDisposed();
            CheckInitialized();

            return _lastPollDurationMs;
        }

        /// <inheritdoc/>
        public double GetAveragePollDurationMs()
        {
            CheckDisposed();
            CheckInitialized();

            if (_pollCount == 0)
            {
                return 0;
            }

            return (double)_totalPollDurationMs / _pollCount;
        }

        /// <inheritdoc/>
        public long GetTotalPollDurationMs()
        {
            CheckDisposed();
            CheckInitialized();

            return _totalPollDurationMs;
        }

        /// <inheritdoc/>
        public ulong GetPollCount()
        {
            CheckDisposed();
            CheckInitialized();

            return _pollCount;
        }

        /// <summary>
        /// Polls the blockchain for new blocks and events.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task PollBlockchainAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _lastPollTimestamp = DateTime.UtcNow;
                    _pollStopwatch.Restart();

                    // Get the current blockchain height
                    var currentHeight = await _blockchainService.GetBlockchainHeightAsync();

                    // Calculate the target height (current height - required confirmations)
                    var targetHeight = currentHeight > (ulong)_requiredConfirmations ? currentHeight - (ulong)_requiredConfirmations : 0;

                    // If the target height is greater than the last processed height, process new blocks
                    if (targetHeight > _lastProcessedBlockHeight)
                    {
                        // Calculate the number of blocks to process
                        var blocksToProcess = Math.Min(targetHeight - _lastProcessedBlockHeight, (ulong)_maxBlocksPerPoll);

                        // Process blocks
                        for (ulong i = 0; i < blocksToProcess; i++)
                        {
                            var blockHeight = _lastProcessedBlockHeight + i + 1;
                            await ProcessBlockAsync(blockHeight);
                        }

                        // Update the last processed block height
                        _lastProcessedBlockHeight += blocksToProcess;
                        await SaveLastProcessedBlockHeightAsync();
                    }

                    _pollStopwatch.Stop();
                    _lastPollDurationMs = _pollStopwatch.ElapsedMilliseconds;
                    _totalPollDurationMs += _lastPollDurationMs;
                    _pollCount++;

                    _logger.LogDebug("Blockchain poll completed in {PollDurationMs}ms, processed {BlocksProcessed} blocks, last processed height: {LastProcessedBlockHeight}",
                        _lastPollDurationMs, _blocksProcessedCount, _lastProcessedBlockHeight);
                }
                catch (Exception ex)
                {
                    _errorCount++;
                    _lastErrorMessage = ex.Message;
                    _lastErrorTimestamp = DateTime.UtcNow;
                    _logger.LogError(ex, "Error polling blockchain");
                }

                // Wait for the next poll
                try
                {
                    await Task.Delay(_pollingIntervalMs, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    // Cancellation requested
                    break;
                }
            }
        }

        /// <summary>
        /// Processes a block.
        /// </summary>
        /// <param name="blockHeight">The block height.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ProcessBlockAsync(ulong blockHeight)
        {
            try
            {
                // Get the block
                var block = await _blockchainService.GetBlockByHeightAsync(blockHeight);
                if (block == null)
                {
                    _logger.LogWarning("Block at height {BlockHeight} not found", blockHeight);
                    return;
                }

                // Process each subscription
                foreach (var subscription in _subscriptions.Values)
                {
                    // Skip inactive subscriptions
                    if (!subscription.IsActive)
                    {
                        continue;
                    }

                    // Skip if the block height is less than the subscription's from block
                    if (blockHeight < subscription.FromBlock)
                    {
                        continue;
                    }

                    try
                    {
                        // Get events for the subscription
                        BlockchainEvent[] events;
                        if (string.IsNullOrEmpty(subscription.EventName))
                        {
                            events = await _blockchainService.GetContractEventsAsync(subscription.ContractHash, blockHeight, 100);
                        }
                        else
                        {
                            events = await _blockchainService.GetContractEventsByNameAsync(subscription.ContractHash, subscription.EventName, blockHeight, 100);
                        }

                        // Process events
                        foreach (var @event in events)
                        {
                            try
                            {
                                // Set the block height and hash
                                @event.BlockHeight = blockHeight;
                                @event.BlockHash = block.Hash;
                                @event.Timestamp = block.Timestamp;

                                // Check if this is a JavaScript execution request event
                                if (@event.EventName == "ExecuteJavaScript" || @event.EventName == "ExecuteFunction")
                                {
                                    // Process JavaScript execution request
                                    await ProcessJavaScriptExecutionRequestAsync(@event);
                                    _eventsProcessedCount++;
                                }
                                else
                                {
                                    // Get the callback for other event types
                                    if (subscription.Metadata.TryGetValue("callback", out var callbackObj) && callbackObj is Func<BlockchainEvent, Task> callback)
                                    {
                                        // Invoke the callback
                                        await callback(@event);
                                        _eventsProcessedCount++;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _errorCount++;
                                _lastErrorMessage = ex.Message;
                                _lastErrorTimestamp = DateTime.UtcNow;
                                _logger.LogError(ex, "Error processing event {EventId} for subscription {SubscriptionId}", @event.Id, subscription.Id);
                            }
                        }

                        // Update the subscription's last processed block
                        subscription.LastProcessedBlock = blockHeight;
                        subscription.UpdatedAt = DateTime.UtcNow;

                        // Save the subscription
                        await SaveSubscriptionAsync(subscription);
                    }
                    catch (Exception ex)
                    {
                        _errorCount++;
                        _lastErrorMessage = ex.Message;
                        _lastErrorTimestamp = DateTime.UtcNow;
                        _logger.LogError(ex, "Error processing subscription {SubscriptionId} for block {BlockHeight}", subscription.Id, blockHeight);
                    }
                }

                _blocksProcessedCount++;
            }
            catch (Exception ex)
            {
                _errorCount++;
                _lastErrorMessage = ex.Message;
                _lastErrorTimestamp = DateTime.UtcNow;
                _logger.LogError(ex, "Error processing block {BlockHeight}", blockHeight);
            }
        }

        /// <summary>
        /// Saves a subscription to storage.
        /// </summary>
        /// <param name="subscription">The subscription to save.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task SaveSubscriptionAsync(BlockchainSubscription subscription)
        {
            try
            {
                var storageProvider = _storageManager.GetProvider("blockchain_subscriptions");
                if (storageProvider == null)
                {
                    _logger.LogError("Storage provider for blockchain subscriptions not found");
                    return;
                }

                // Clone the subscription to avoid modifying the original
                var subscriptionCopy = new BlockchainSubscription
                {
                    Id = subscription.Id,
                    ContractHash = subscription.ContractHash,
                    EventName = subscription.EventName,
                    FromBlock = subscription.FromBlock,
                    LastProcessedBlock = subscription.LastProcessedBlock,
                    CreatedAt = subscription.CreatedAt,
                    UpdatedAt = subscription.UpdatedAt,
                    IsActive = subscription.IsActive,
                    Metadata = new Dictionary<string, object>(subscription.Metadata)
                };

                // Remove the callback from the metadata
                subscriptionCopy.Metadata.Remove("callback");

                // Serialize the subscription
                var json = System.Text.Json.JsonSerializer.Serialize(subscriptionCopy);
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);

                // Save the subscription
                await storageProvider.WriteAsync(subscription.Id, bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving subscription {SubscriptionId} to storage", subscription.Id);
            }
        }

        /// <summary>
        /// Deletes a subscription from storage.
        /// </summary>
        /// <param name="subscriptionId">The subscription ID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task DeleteSubscriptionAsync(string subscriptionId)
        {
            try
            {
                var storageProvider = _storageManager.GetProvider("blockchain_subscriptions");
                if (storageProvider == null)
                {
                    _logger.LogError("Storage provider for blockchain subscriptions not found");
                    return;
                }

                // Delete the subscription
                await storageProvider.DeleteAsync(subscriptionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subscription {SubscriptionId} from storage", subscriptionId);
            }
        }

        /// <summary>
        /// Loads subscriptions from storage.
        /// </summary>
        /// <returns>True if subscriptions were loaded successfully, false otherwise.</returns>
        private async Task<bool> LoadSubscriptionsAsync()
        {
            try
            {
                var storageProvider = _storageManager.GetProvider("blockchain_subscriptions");
                if (storageProvider == null)
                {
                    _logger.LogError("Storage provider for blockchain subscriptions not found");
                    return false;
                }

                // Get all subscription IDs
                var subscriptionIds = await storageProvider.GetAllKeysAsync();

                // Load each subscription
                foreach (var subscriptionId in subscriptionIds)
                {
                    try
                    {
                        // Read the subscription
                        var bytes = await storageProvider.ReadAsync(subscriptionId);
                        if (bytes == null || bytes.Length == 0)
                        {
                            _logger.LogWarning("Empty subscription data for ID {SubscriptionId}", subscriptionId);
                            continue;
                        }

                        // Deserialize the subscription
                        var json = System.Text.Encoding.UTF8.GetString(bytes);
                        var subscription = System.Text.Json.JsonSerializer.Deserialize<BlockchainSubscription>(json);

                        // Add the subscription to the dictionary
                        _subscriptions[subscriptionId] = subscription;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error loading subscription {SubscriptionId} from storage", subscriptionId);
                    }
                }

                _logger.LogInformation("Loaded {SubscriptionCount} blockchain subscriptions from storage", _subscriptions.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading blockchain subscriptions from storage");
                return false;
            }
        }

        /// <summary>
        /// Saves the last processed block height to storage.
        /// </summary>
        /// <returns>True if the last processed block height was saved successfully, false otherwise.</returns>
        private async Task<bool> SaveLastProcessedBlockHeightAsync()
        {
            try
            {
                var storageProvider = _storageManager.GetProvider("blockchain_subscriptions");
                if (storageProvider == null)
                {
                    _logger.LogError("Storage provider for blockchain subscriptions not found");
                    return false;
                }

                // Convert the height to bytes
                var bytes = BitConverter.GetBytes(_lastProcessedBlockHeight);

                // Save the height
                await storageProvider.WriteAsync("last_processed_block_height", bytes);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving last processed block height to storage");
                return false;
            }
        }

        /// <summary>
        /// Loads the last processed block height from storage.
        /// </summary>
        /// <returns>True if the last processed block height was loaded successfully, false otherwise.</returns>
        private async Task<bool> LoadLastProcessedBlockHeightAsync()
        {
            try
            {
                var storageProvider = _storageManager.GetProvider("blockchain_subscriptions");
                if (storageProvider == null)
                {
                    _logger.LogError("Storage provider for blockchain subscriptions not found");
                    return false;
                }

                // Read the height
                var bytes = await storageProvider.ReadAsync("last_processed_block_height");
                if (bytes == null || bytes.Length == 0)
                {
                    _logger.LogInformation("No last processed block height found in storage, using default value of 0");
                    _lastProcessedBlockHeight = 0;
                    return true;
                }

                // Convert the bytes to a height
                _lastProcessedBlockHeight = BitConverter.ToUInt64(bytes, 0);

                _logger.LogInformation("Loaded last processed block height from storage: {LastProcessedBlockHeight}", _lastProcessedBlockHeight);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading last processed block height from storage");
                return false;
            }
        }

        /// <summary>
        /// Process a JavaScript execution request from a Neo N3 smart contract.
        /// </summary>
        /// <param name="event">The blockchain event containing the execution request.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ProcessJavaScriptExecutionRequestAsync(BlockchainEvent @event)
        {
            try
            {
                _logger.LogInformation("Processing JavaScript execution request from contract {ContractHash}", @event.ContractHash);

                // Extract parameters from the event state
                // The event state should contain:
                // - functionId: The ID of the JavaScript function to execute
                // - input: The input data for the function (optional)
                // - userId: The ID of the user who owns the function (optional)
                // - callbackMethod: The method to call back with the result (optional)

                string functionId = null;
                string input = "{}";
                string userId = "default";
                string callbackMethod = "ReceiveExecutionResult";

                // Parse the event state
                if (@event.State != null && @event.State.Length > 0)
                {
                    // The state format depends on the Neo N3 smart contract implementation
                    // This is a simplified example - adjust based on your actual event format
                    if (@event.State.Length >= 1 && @event.State[0] != null)
                    {
                        functionId = @event.State[0].ToString();
                    }

                    if (@event.State.Length >= 2 && @event.State[1] != null)
                    {
                        input = @event.State[1].ToString();
                    }

                    if (@event.State.Length >= 3 && @event.State[2] != null)
                    {
                        userId = @event.State[2].ToString();
                    }

                    if (@event.State.Length >= 4 && @event.State[3] != null)
                    {
                        callbackMethod = @event.State[3].ToString();
                    }
                }

                if (string.IsNullOrEmpty(functionId))
                {
                    _logger.LogError("Missing function ID in JavaScript execution request");
                    return;
                }

                // Get the enclave interface
                var enclaveInterface = _serviceProvider.GetRequiredService<ITeeInterface>();

                // Execute the JavaScript function
                _logger.LogInformation("Executing JavaScript function {FunctionId} for user {UserId}", functionId, userId);
                var result = await enclaveInterface.ExecuteJavaScriptAsync("", input, "{}", functionId, userId);

                // Send the result back to the smart contract
                if (!string.IsNullOrEmpty(callbackMethod))
                {
                    await SendCallbackToSmartContractAsync(@event.ContractHash, callbackMethod, functionId, result.Result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing JavaScript execution request from contract {ContractHash}", @event.ContractHash);
            }
        }

        /// <summary>
        /// Send a callback to a Neo N3 smart contract with the JavaScript execution result.
        /// </summary>
        /// <param name="contractHash">The hash of the contract to call.</param>
        /// <param name="method">The method to call.</param>
        /// <param name="functionId">The ID of the JavaScript function that was executed.</param>
        /// <param name="result">The result of the JavaScript execution.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task SendCallbackToSmartContractAsync(string contractHash, string method, string functionId, string result)
        {
            try
            {
                _logger.LogInformation("Sending callback to contract {ContractHash}.{Method} with result for function {FunctionId}",
                    contractHash, method, functionId);

                // Get the blockchain service
                var blockchainService = _serviceProvider.GetRequiredService<INeoN3BlockchainService>();

                // Invoke the callback method on the smart contract
                var txHash = await blockchainService.InvokeContractMethodAsync(
                    contractHash,
                    method,
                    new object[] { functionId, result });

                _logger.LogInformation("Callback sent to contract {ContractHash}.{Method} with transaction hash {TxHash}",
                    contractHash, method, txHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending callback to contract {ContractHash}.{Method}", contractHash, method);
            }
        }

        /// <summary>
        /// Checks if the listener is initialized.
        /// </summary>
        private void CheckInitialized()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Blockchain event listener is not initialized");
            }
        }

        /// <summary>
        /// Checks if the listener is disposed.
        /// </summary>
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(BlockchainEventListener));
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the listener.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Stop the listener
                    if (_isRunning)
                    {
                        StopAsync().GetAwaiter().GetResult();
                    }

                    // Dispose managed resources
                    _cancellationTokenSource.Dispose();
                    _semaphore.Dispose();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizes the listener.
        /// </summary>
        ~BlockchainEventListener()
        {
            Dispose(false);
        }
    }
}
