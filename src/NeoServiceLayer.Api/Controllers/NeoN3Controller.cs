using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Api.Models;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Infrastructure.Services;
using System;
using System.Threading.Tasks;

namespace NeoServiceLayer.Api.Controllers
{
    /// <summary>
    /// Controller for Neo N3 blockchain operations.
    /// </summary>
    [ApiController]
    [Route("api/neo")]
    public class NeoN3Controller : ControllerBase
    {
        private readonly ILogger<NeoN3Controller> _logger;
        private readonly INeoN3BlockchainService _blockchainService;
        private readonly NeoN3EventListenerService _eventListenerService;

        /// <summary>
        /// Initializes a new instance of the <see cref="NeoN3Controller"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="blockchainService">The blockchain service.</param>
        /// <param name="eventListenerService">The event listener service.</param>
        public NeoN3Controller(
            ILogger<NeoN3Controller> logger,
            INeoN3BlockchainService blockchainService,
            NeoN3EventListenerService eventListenerService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _blockchainService = blockchainService ?? throw new ArgumentNullException(nameof(blockchainService));
            _eventListenerService = eventListenerService ?? throw new ArgumentNullException(nameof(eventListenerService));
        }

        /// <summary>
        /// Gets the current blockchain height.
        /// </summary>
        /// <returns>The blockchain height.</returns>
        [HttpGet("height")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 500)]
        public async Task<IActionResult> GetBlockchainHeight()
        {
            try
            {
                _logger.LogInformation("Getting blockchain height");

                var height = await _blockchainService.GetBlockchainHeightAsync();

                return Ok(ApiResponse<string>.CreateSuccess(height));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blockchain height");
                return StatusCode(500, ApiResponse<string>.CreateError(ApiErrorCodes.InternalServerError, "Error getting blockchain height"));
            }
        }

        /// <summary>
        /// Gets a transaction by its hash.
        /// </summary>
        /// <param name="txHash">The transaction hash.</param>
        /// <returns>The transaction details.</returns>
        [HttpGet("transaction/{txHash}")]
        [ProducesResponseType(typeof(ApiResponse<Core.Models.BlockchainTransaction>), 200)]
        [ProducesResponseType(typeof(ApiResponse<Core.Models.BlockchainTransaction>), 404)]
        [ProducesResponseType(typeof(ApiResponse<Core.Models.BlockchainTransaction>), 500)]
        public async Task<IActionResult> GetTransaction(string txHash)
        {
            try
            {
                _logger.LogInformation("Getting transaction: {TxHash}", txHash);

                var tx = await _blockchainService.GetTransactionAsync(txHash);
                if (tx == null)
                {
                    return NotFound(ApiResponse<Core.Models.BlockchainTransaction>.CreateError(ApiErrorCodes.NotFound, "Transaction not found"));
                }

                return Ok(ApiResponse<Core.Models.BlockchainTransaction>.CreateSuccess(tx));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction: {TxHash}", txHash);
                return StatusCode(500, ApiResponse<Core.Models.BlockchainTransaction>.CreateError(ApiErrorCodes.InternalServerError, "Error getting transaction"));
            }
        }

        /// <summary>
        /// Invokes a smart contract method.
        /// </summary>
        /// <param name="request">The contract invocation request.</param>
        /// <returns>The transaction hash of the invocation.</returns>
        [HttpPost("invoke")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        [ProducesResponseType(typeof(ApiResponse<string>), 500)]
        public async Task<IActionResult> InvokeContract([FromBody] ContractInvocationRequest request)
        {
            try
            {
                _logger.LogInformation("Invoking contract: {ScriptHash}, operation: {Operation}", request.ScriptHash, request.Operation);

                if (string.IsNullOrEmpty(request.ScriptHash))
                {
                    return BadRequest(ApiResponse<string>.CreateError(ApiErrorCodes.ValidationError, "Script hash is required"));
                }

                if (string.IsNullOrEmpty(request.Operation))
                {
                    return BadRequest(ApiResponse<string>.CreateError(ApiErrorCodes.ValidationError, "Operation is required"));
                }

                var txHash = await _blockchainService.InvokeContractAsync(request.ScriptHash, request.Operation, request.Args ?? Array.Empty<object>());

                return Ok(ApiResponse<string>.CreateSuccess(txHash));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invoking contract: {ScriptHash}, operation: {Operation}", request.ScriptHash, request.Operation);
                return StatusCode(500, ApiResponse<string>.CreateError(ApiErrorCodes.InternalServerError, "Error invoking contract"));
            }
        }

        /// <summary>
        /// Test invokes a smart contract method without submitting a transaction.
        /// </summary>
        /// <param name="request">The contract invocation request.</param>
        /// <returns>The result of the test invocation.</returns>
        [HttpPost("testinvoke")]
        [ProducesResponseType(typeof(ApiResponse<Core.Models.ContractInvocationResult>), 200)]
        [ProducesResponseType(typeof(ApiResponse<Core.Models.ContractInvocationResult>), 400)]
        [ProducesResponseType(typeof(ApiResponse<Core.Models.ContractInvocationResult>), 500)]
        public async Task<IActionResult> TestInvokeContract([FromBody] ContractInvocationRequest request)
        {
            try
            {
                _logger.LogInformation("Test invoking contract: {ScriptHash}, operation: {Operation}", request.ScriptHash, request.Operation);

                if (string.IsNullOrEmpty(request.ScriptHash))
                {
                    return BadRequest(ApiResponse<Core.Models.ContractInvocationResult>.CreateError(ApiErrorCodes.ValidationError, "Script hash is required"));
                }

                if (string.IsNullOrEmpty(request.Operation))
                {
                    return BadRequest(ApiResponse<Core.Models.ContractInvocationResult>.CreateError(ApiErrorCodes.ValidationError, "Operation is required"));
                }

                var result = await _blockchainService.TestInvokeContractAsync(request.ScriptHash, request.Operation, request.Args ?? Array.Empty<object>());

                return Ok(ApiResponse<Core.Models.ContractInvocationResult>.CreateSuccess(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error test invoking contract: {ScriptHash}, operation: {Operation}", request.ScriptHash, request.Operation);
                return StatusCode(500, ApiResponse<Core.Models.ContractInvocationResult>.CreateError(ApiErrorCodes.InternalServerError, "Error test invoking contract"));
            }
        }

        /// <summary>
        /// Gets events emitted by a smart contract.
        /// </summary>
        /// <param name="scriptHash">The script hash of the contract.</param>
        /// <param name="fromBlock">The block index to start from.</param>
        /// <param name="count">The maximum number of events to return.</param>
        /// <returns>An array of blockchain events.</returns>
        [HttpGet("events/{scriptHash}")]
        [ProducesResponseType(typeof(ApiResponse<Core.Models.BlockchainEvent[]>), 200)]
        [ProducesResponseType(typeof(ApiResponse<Core.Models.BlockchainEvent[]>), 400)]
        [ProducesResponseType(typeof(ApiResponse<Core.Models.BlockchainEvent[]>), 500)]
        public async Task<IActionResult> GetContractEvents(string scriptHash, [FromQuery] int fromBlock = 0, [FromQuery] int count = 100)
        {
            try
            {
                _logger.LogInformation("Getting contract events: {ScriptHash}, from block: {FromBlock}, count: {Count}", scriptHash, fromBlock, count);

                if (string.IsNullOrEmpty(scriptHash))
                {
                    return BadRequest(ApiResponse<Core.Models.BlockchainEvent[]>.CreateError(ApiErrorCodes.ValidationError, "Script hash is required"));
                }

                var events = await _blockchainService.GetContractEventsAsync(scriptHash, fromBlock, count);

                return Ok(ApiResponse<Core.Models.BlockchainEvent[]>.CreateSuccess(events));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contract events: {ScriptHash}", scriptHash);
                return StatusCode(500, ApiResponse<Core.Models.BlockchainEvent[]>.CreateError(ApiErrorCodes.InternalServerError, "Error getting contract events"));
            }
        }

        /// <summary>
        /// Subscribes to a contract event.
        /// </summary>
        /// <param name="request">The event subscription request.</param>
        /// <returns>The subscription ID.</returns>
        [HttpPost("subscribe")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        [ProducesResponseType(typeof(ApiResponse<string>), 500)]
        public IActionResult SubscribeToEvent([FromBody] EventSubscriptionRequest request)
        {
            try
            {
                _logger.LogInformation("Subscribing to event: {ScriptHash}, event: {EventName}", request.ScriptHash, request.EventName);

                if (string.IsNullOrEmpty(request.ScriptHash))
                {
                    return BadRequest(ApiResponse<string>.CreateError(ApiErrorCodes.ValidationError, "Script hash is required"));
                }

                if (string.IsNullOrEmpty(request.EventName))
                {
                    return BadRequest(ApiResponse<string>.CreateError(ApiErrorCodes.ValidationError, "Event name is required"));
                }

                if (string.IsNullOrEmpty(request.CallbackUrl))
                {
                    return BadRequest(ApiResponse<string>.CreateError(ApiErrorCodes.ValidationError, "Callback URL is required"));
                }

                var subscriptionId = _eventListenerService.AddSubscription(request.ScriptHash, request.EventName, request.CallbackUrl, request.StartBlock);

                return Ok(ApiResponse<string>.CreateSuccess(subscriptionId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to event: {ScriptHash}, event: {EventName}", request.ScriptHash, request.EventName);
                return StatusCode(500, ApiResponse<string>.CreateError(ApiErrorCodes.InternalServerError, "Error subscribing to event"));
            }
        }

        /// <summary>
        /// Unsubscribes from a contract event.
        /// </summary>
        /// <param name="subscriptionId">The subscription ID.</param>
        /// <returns>True if the subscription was removed, false otherwise.</returns>
        [HttpDelete("unsubscribe/{subscriptionId}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 500)]
        public IActionResult UnsubscribeFromEvent(string subscriptionId)
        {
            try
            {
                _logger.LogInformation("Unsubscribing from event: {SubscriptionId}", subscriptionId);

                if (string.IsNullOrEmpty(subscriptionId))
                {
                    return BadRequest(ApiResponse<bool>.CreateError(ApiErrorCodes.ValidationError, "Subscription ID is required"));
                }

                var result = _eventListenerService.RemoveSubscription(subscriptionId);

                return Ok(ApiResponse<bool>.CreateSuccess(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsubscribing from event: {SubscriptionId}", subscriptionId);
                return StatusCode(500, ApiResponse<bool>.CreateError(ApiErrorCodes.InternalServerError, "Error unsubscribing from event"));
            }
        }
    }
}
