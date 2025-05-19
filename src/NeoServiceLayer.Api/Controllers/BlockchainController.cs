using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Shared.Blockchain;
using NeoServiceLayer.Tee.Shared.Events;

namespace NeoServiceLayer.Api.Controllers
{
    /// <summary>
    /// Controller for blockchain operations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BlockchainController : ControllerBase
    {
        private readonly ILogger<BlockchainController> _logger;
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockchainEventListener _eventListener;
        private readonly IEventSystem _eventSystem;

        /// <summary>
        /// Initializes a new instance of the BlockchainController class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="blockchainService">The blockchain service.</param>
        /// <param name="eventListener">The blockchain event listener.</param>
        /// <param name="eventSystem">The event system.</param>
        public BlockchainController(
            ILogger<BlockchainController> logger,
            IBlockchainService blockchainService,
            IBlockchainEventListener eventListener,
            IEventSystem eventSystem)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _blockchainService = blockchainService ?? throw new ArgumentNullException(nameof(blockchainService));
            _eventListener = eventListener ?? throw new ArgumentNullException(nameof(eventListener));
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
        }

        /// <summary>
        /// Gets the blockchain information.
        /// </summary>
        /// <returns>The blockchain information.</returns>
        [HttpGet("info")]
        [ProducesResponseType(typeof(BlockchainInfoResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetBlockchainInfo()
        {
            try
            {
                var height = await _blockchainService.GetBlockchainHeightAsync();
                var version = await _blockchainService.GetBlockchainVersionAsync();
                var peerCount = await _blockchainService.GetBlockchainPeerCountAsync();
                var syncState = await _blockchainService.GetBlockchainSyncStateAsync();
                var connected = await _blockchainService.IsConnectedAsync();
                var synced = await _blockchainService.IsSyncedAsync();

                var response = new BlockchainInfoResponse
                {
                    Type = _blockchainService.GetBlockchainType(),
                    Network = _blockchainService.GetBlockchainNetwork(),
                    RpcUrl = _blockchainService.GetBlockchainRpcUrl(),
                    Height = height,
                    Version = version,
                    PeerCount = peerCount,
                    SyncState = syncState,
                    IsConnected = connected,
                    IsSynced = synced
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blockchain information");
                return StatusCode(500, "Error getting blockchain information");
            }
        }

        /// <summary>
        /// Gets a block by its height.
        /// </summary>
        /// <param name="height">The block height.</param>
        /// <returns>The block.</returns>
        [HttpGet("blocks/{height}")]
        [ProducesResponseType(typeof(BlockchainBlock), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetBlockByHeight(ulong height)
        {
            try
            {
                var block = await _blockchainService.GetBlockByHeightAsync(height);
                if (block == null)
                {
                    return NotFound();
                }

                return Ok(block);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting block at height {Height}", height);
                return StatusCode(500, $"Error getting block at height {height}");
            }
        }

        /// <summary>
        /// Gets a block by its hash.
        /// </summary>
        /// <param name="hash">The block hash.</param>
        /// <returns>The block.</returns>
        [HttpGet("blocks/hash/{hash}")]
        [ProducesResponseType(typeof(BlockchainBlock), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetBlockByHash(string hash)
        {
            try
            {
                var block = await _blockchainService.GetBlockByHashAsync(hash);
                if (block == null)
                {
                    return NotFound();
                }

                return Ok(block);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting block with hash {Hash}", hash);
                return StatusCode(500, $"Error getting block with hash {hash}");
            }
        }

        /// <summary>
        /// Gets a transaction by its hash.
        /// </summary>
        /// <param name="hash">The transaction hash.</param>
        /// <returns>The transaction.</returns>
        [HttpGet("transactions/{hash}")]
        [ProducesResponseType(typeof(BlockchainTransaction), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetTransaction(string hash)
        {
            try
            {
                var transaction = await _blockchainService.GetTransactionAsync(hash);
                if (transaction == null)
                {
                    return NotFound();
                }

                return Ok(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction with hash {Hash}", hash);
                return StatusCode(500, $"Error getting transaction with hash {hash}");
            }
        }

        /// <summary>
        /// Gets the balance of an address.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="assetId">The asset ID, or null for the native asset.</param>
        /// <returns>The balance.</returns>
        [HttpGet("balance/{address}")]
        [ProducesResponseType(typeof(BalanceResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetBalance(string address, [FromQuery] string assetId = null)
        {
            try
            {
                var balance = await _blockchainService.GetBalanceAsync(address, assetId);

                var response = new BalanceResponse
                {
                    Address = address,
                    AssetId = assetId,
                    Balance = balance
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting balance for address {Address}", address);
                return StatusCode(500, $"Error getting balance for address {address}");
            }
        }

        /// <summary>
        /// Gets events emitted by a smart contract.
        /// </summary>
        /// <param name="contractHash">The contract hash.</param>
        /// <param name="fromBlock">The block height to start from.</param>
        /// <param name="count">The maximum number of events to return.</param>
        /// <returns>An array of blockchain events.</returns>
        [HttpGet("events/{contractHash}")]
        [ProducesResponseType(typeof(BlockchainEvent[]), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetContractEvents(string contractHash, [FromQuery] ulong fromBlock = 0, [FromQuery] int count = 100)
        {
            try
            {
                var events = await _blockchainService.GetContractEventsAsync(contractHash, fromBlock, count);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting events for contract {ContractHash}", contractHash);
                return StatusCode(500, $"Error getting events for contract {contractHash}");
            }
        }

        /// <summary>
        /// Gets events emitted by a smart contract with a specific name.
        /// </summary>
        /// <param name="contractHash">The contract hash.</param>
        /// <param name="eventName">The event name.</param>
        /// <param name="fromBlock">The block height to start from.</param>
        /// <param name="count">The maximum number of events to return.</param>
        /// <returns>An array of blockchain events.</returns>
        [HttpGet("events/{contractHash}/{eventName}")]
        [ProducesResponseType(typeof(BlockchainEvent[]), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetContractEventsByName(string contractHash, string eventName, [FromQuery] ulong fromBlock = 0, [FromQuery] int count = 100)
        {
            try
            {
                var events = await _blockchainService.GetContractEventsByNameAsync(contractHash, eventName, fromBlock, count);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting events for contract {ContractHash} and event {EventName}", contractHash, eventName);
                return StatusCode(500, $"Error getting events for contract {contractHash} and event {eventName}");
            }
        }

        /// <summary>
        /// Invokes a smart contract.
        /// </summary>
        /// <param name="request">The contract invocation request.</param>
        /// <returns>The transaction hash.</returns>
        [HttpPost("invoke")]
        [ProducesResponseType(typeof(ContractInvocationResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> InvokeContract([FromBody] ContractInvocationRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body is required");
            }

            if (string.IsNullOrEmpty(request.ContractHash))
            {
                return BadRequest("Contract hash is required");
            }

            if (string.IsNullOrEmpty(request.Operation))
            {
                return BadRequest("Operation is required");
            }

            try
            {
                var txHash = await _blockchainService.InvokeContractAsync(request.ContractHash, request.Operation, request.Args ?? Array.Empty<object>());

                var response = new ContractInvocationResponse
                {
                    TransactionHash = txHash
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invoking contract {ContractHash}.{Operation}", request.ContractHash, request.Operation);
                return StatusCode(500, $"Error invoking contract {request.ContractHash}.{Operation}");
            }
        }

        /// <summary>
        /// Test invokes a smart contract.
        /// </summary>
        /// <param name="request">The contract invocation request.</param>
        /// <returns>The result of the test invocation.</returns>
        [HttpPost("test-invoke")]
        [ProducesResponseType(typeof(ContractInvocationResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> TestInvokeContract([FromBody] ContractInvocationRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body is required");
            }

            if (string.IsNullOrEmpty(request.ContractHash))
            {
                return BadRequest("Contract hash is required");
            }

            if (string.IsNullOrEmpty(request.Operation))
            {
                return BadRequest("Operation is required");
            }

            try
            {
                var result = await _blockchainService.TestInvokeContractAsync(request.ContractHash, request.Operation, request.Args ?? Array.Empty<object>());
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error test invoking contract {ContractHash}.{Operation}", request.ContractHash, request.Operation);
                return StatusCode(500, $"Error test invoking contract {request.ContractHash}.{Operation}");
            }
        }

        /// <summary>
        /// Subscribes to events emitted by a smart contract.
        /// </summary>
        /// <param name="request">The subscription request.</param>
        /// <returns>The subscription ID.</returns>
        [HttpPost("subscribe")]
        [ProducesResponseType(typeof(SubscriptionResponse), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SubscribeToContractEvents([FromBody] SubscriptionRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body is required");
            }

            if (string.IsNullOrEmpty(request.ContractHash))
            {
                return BadRequest("Contract hash is required");
            }

            try
            {
                // Create a callback that publishes events to the event system
                async Task EventCallback(BlockchainEvent @event)
                {
                    await _eventSystem.PublishEventAsync(
                        EventType.Blockchain,
                        "blockchain",
                        System.Text.Json.JsonSerializer.Serialize(@event),
                        request.UserId);
                }

                // Subscribe to events
                var subscriptionId = await _eventListener.SubscribeToContractEventsAsync(
                    request.ContractHash,
                    request.EventName,
                    EventCallback,
                    request.FromBlock);

                var response = new SubscriptionResponse
                {
                    SubscriptionId = subscriptionId
                };

                return Created($"/api/blockchain/subscriptions/{subscriptionId}", response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to contract {ContractHash} events", request.ContractHash);
                return StatusCode(500, $"Error subscribing to contract {request.ContractHash} events");
            }
        }

        /// <summary>
        /// Unsubscribes from events.
        /// </summary>
        /// <param name="subscriptionId">The subscription ID.</param>
        /// <returns>A status code indicating success or failure.</returns>
        [HttpDelete("subscriptions/{subscriptionId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UnsubscribeFromEvents(string subscriptionId)
        {
            try
            {
                var success = await _eventListener.UnsubscribeFromEventsAsync(subscriptionId);
                if (!success)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsubscribing from events with subscription ID {SubscriptionId}", subscriptionId);
                return StatusCode(500, $"Error unsubscribing from events with subscription ID {subscriptionId}");
            }
        }

        /// <summary>
        /// Gets all active subscriptions.
        /// </summary>
        /// <returns>A list of active subscriptions.</returns>
        [HttpGet("subscriptions")]
        [ProducesResponseType(typeof(IEnumerable<BlockchainSubscription>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetActiveSubscriptions()
        {
            try
            {
                var subscriptions = await _eventListener.GetActiveSubscriptionsAsync();
                return Ok(subscriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active subscriptions");
                return StatusCode(500, "Error getting active subscriptions");
            }
        }

        /// <summary>
        /// Gets the blockchain event listener status.
        /// </summary>
        /// <returns>The blockchain event listener status.</returns>
        [HttpGet("listener/status")]
        [ProducesResponseType(typeof(EventListenerStatusResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public IActionResult GetEventListenerStatus()
        {
            try
            {
                var response = new EventListenerStatusResponse
                {
                    IsRunning = _eventListener.IsRunning(),
                    LastProcessedBlockHeight = _eventListener.GetLastProcessedBlockHeightAsync().Result,
                    EventsProcessedCount = _eventListener.GetEventsProcessedCount(),
                    BlocksProcessedCount = _eventListener.GetBlocksProcessedCount(),
                    ErrorCount = _eventListener.GetErrorCount(),
                    LastErrorMessage = _eventListener.GetLastErrorMessage(),
                    LastErrorTimestamp = _eventListener.GetLastErrorTimestamp(),
                    StartTimestamp = _eventListener.GetStartTimestamp(),
                    StopTimestamp = _eventListener.GetStopTimestamp(),
                    LastPollTimestamp = _eventListener.GetLastPollTimestamp(),
                    LastPollDurationMs = _eventListener.GetLastPollDurationMs(),
                    AveragePollDurationMs = _eventListener.GetAveragePollDurationMs(),
                    TotalPollDurationMs = _eventListener.GetTotalPollDurationMs(),
                    PollCount = _eventListener.GetPollCount(),
                    PollingIntervalMs = _eventListener.GetPollingIntervalMs(),
                    MaxBlocksPerPoll = _eventListener.GetMaxBlocksPerPoll(),
                    RequiredConfirmations = _eventListener.GetRequiredConfirmations()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blockchain event listener status");
                return StatusCode(500, "Error getting blockchain event listener status");
            }
        }
    }

    /// <summary>
    /// Response model for blockchain information.
    /// </summary>
    public class BlockchainInfoResponse
    {
        /// <summary>
        /// Gets or sets the blockchain type.
        /// </summary>
        public BlockchainType Type { get; set; }

        /// <summary>
        /// Gets or sets the blockchain network.
        /// </summary>
        public string Network { get; set; }

        /// <summary>
        /// Gets or sets the blockchain RPC URL.
        /// </summary>
        public string RpcUrl { get; set; }

        /// <summary>
        /// Gets or sets the blockchain height.
        /// </summary>
        public ulong Height { get; set; }

        /// <summary>
        /// Gets or sets the blockchain version.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the blockchain peer count.
        /// </summary>
        public int PeerCount { get; set; }

        /// <summary>
        /// Gets or sets the blockchain sync state.
        /// </summary>
        public BlockchainSyncState SyncState { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the blockchain is connected.
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the blockchain is synced.
        /// </summary>
        public bool IsSynced { get; set; }
    }

    /// <summary>
    /// Response model for balance.
    /// </summary>
    public class BalanceResponse
    {
        /// <summary>
        /// Gets or sets the address.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Gets or sets the asset ID.
        /// </summary>
        public string AssetId { get; set; }

        /// <summary>
        /// Gets or sets the balance.
        /// </summary>
        public decimal Balance { get; set; }
    }

    /// <summary>
    /// Request model for contract invocation.
    /// </summary>
    public class ContractInvocationRequest
    {
        /// <summary>
        /// Gets or sets the contract hash.
        /// </summary>
        [Required]
        public string ContractHash { get; set; }

        /// <summary>
        /// Gets or sets the operation to invoke.
        /// </summary>
        [Required]
        public string Operation { get; set; }

        /// <summary>
        /// Gets or sets the arguments to pass to the operation.
        /// </summary>
        public object[] Args { get; set; }
    }

    /// <summary>
    /// Response model for contract invocation.
    /// </summary>
    public class ContractInvocationResponse
    {
        /// <summary>
        /// Gets or sets the transaction hash.
        /// </summary>
        public string TransactionHash { get; set; }
    }

    /// <summary>
    /// Request model for subscription.
    /// </summary>
    public class SubscriptionRequest
    {
        /// <summary>
        /// Gets or sets the contract hash.
        /// </summary>
        [Required]
        public string ContractHash { get; set; }

        /// <summary>
        /// Gets or sets the event name, or null for all events.
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// Gets or sets the block height to start from.
        /// </summary>
        public ulong FromBlock { get; set; }

        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public string UserId { get; set; }
    }

    /// <summary>
    /// Response model for subscription.
    /// </summary>
    public class SubscriptionResponse
    {
        /// <summary>
        /// Gets or sets the subscription ID.
        /// </summary>
        public string SubscriptionId { get; set; }
    }

    /// <summary>
    /// Response model for event listener status.
    /// </summary>
    public class EventListenerStatusResponse
    {
        /// <summary>
        /// Gets or sets a value indicating whether the listener is running.
        /// </summary>
        public bool IsRunning { get; set; }

        /// <summary>
        /// Gets or sets the last processed block height.
        /// </summary>
        public ulong LastProcessedBlockHeight { get; set; }

        /// <summary>
        /// Gets or sets the number of events processed.
        /// </summary>
        public ulong EventsProcessedCount { get; set; }

        /// <summary>
        /// Gets or sets the number of blocks processed.
        /// </summary>
        public ulong BlocksProcessedCount { get; set; }

        /// <summary>
        /// Gets or sets the number of errors encountered.
        /// </summary>
        public ulong ErrorCount { get; set; }

        /// <summary>
        /// Gets or sets the last error message.
        /// </summary>
        public string LastErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the last error timestamp.
        /// </summary>
        public DateTime? LastErrorTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the start timestamp.
        /// </summary>
        public DateTime? StartTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the stop timestamp.
        /// </summary>
        public DateTime? StopTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the last poll timestamp.
        /// </summary>
        public DateTime? LastPollTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the last poll duration in milliseconds.
        /// </summary>
        public long LastPollDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the average poll duration in milliseconds.
        /// </summary>
        public double AveragePollDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the total poll duration in milliseconds.
        /// </summary>
        public long TotalPollDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the number of polls.
        /// </summary>
        public ulong PollCount { get; set; }

        /// <summary>
        /// Gets or sets the polling interval in milliseconds.
        /// </summary>
        public int PollingIntervalMs { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of blocks to process in a single poll.
        /// </summary>
        public int MaxBlocksPerPoll { get; set; }

        /// <summary>
        /// Gets or sets the number of confirmations required before processing a block.
        /// </summary>
        public int RequiredConfirmations { get; set; }
    }
}
