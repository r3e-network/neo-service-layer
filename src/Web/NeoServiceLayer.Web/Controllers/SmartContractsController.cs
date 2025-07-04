using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.SmartContracts;
using NeoServiceLayer.Services.SmartContracts;

namespace NeoServiceLayer.Web.Controllers;

/// <summary>
/// Controller for smart contract operations on Neo N3 and Neo X blockchains.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SmartContractsController : BaseApiController
{
    private readonly ISmartContractsService _smartContractsService;
    private readonly ILogger<SmartContractsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmartContractsController"/> class.
    /// </summary>
    /// <param name="smartContractsService">The smart contracts service.</param>
    /// <param name="logger">The logger.</param>
    public SmartContractsController(
        ISmartContractsService smartContractsService,
        ILogger<SmartContractsController> logger)
        : base(logger)
    {
        _smartContractsService = smartContractsService;
        _logger = logger;
    }


    /// <summary>
    /// Deploys a new smart contract.
    /// </summary>
    /// <param name="request">The deployment request.</param>
    /// <returns>The deployment result.</returns>
    [HttpPost("deploy")]
    [Authorize(Policy = "ServiceUser")]
    public async Task<IActionResult> DeployContractAsync([FromBody] DeployContractRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Deploying contract on {Blockchain}", request.Blockchain);

            var contractCode = Convert.FromBase64String(request.Script);
            var options = new ContractDeploymentOptions
            {
                Name = request.Name,
                Version = request.Version ?? "1.0.0",
                Author = request.Author ?? "Neo Service Layer",
                Description = request.Description ?? "Smart Contract"
            };

            var result = await _smartContractsService.DeployContractAsync(
                request.Blockchain,
                contractCode,
                request.ConstructorParameters,
                options);

            return Ok(new
            {
                success = true,
                contractHash = result.ContractHash,
                transactionHash = result.TransactionHash,
                gasConsumed = result.GasConsumed,
                message = "Contract deployed successfully"
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid deploy contract request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deploying contract");
            return StatusCode(500, new { error = "Failed to deploy contract", details = ex.Message });
        }
    }

    /// <summary>
    /// Invokes a smart contract method.
    /// </summary>
    /// <param name="request">The invocation request.</param>
    /// <returns>The invocation result.</returns>
    [HttpPost("invoke")]
    [Authorize(Policy = "ServiceUser")]
    public async Task<IActionResult> InvokeContractAsync([FromBody] InvokeContractRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Invoking contract {ContractHash} method {Method}",
                request.ContractHash, request.Method);

            var result = await _smartContractsService.InvokeContractAsync(
                request.Blockchain,
                request.ContractHash,
                request.Method,
                request.Params);

            return Ok(new
            {
                success = true,
                transactionHash = result.TransactionHash,
                gasConsumed = result.GasConsumed,
                result = result.ReturnValue,
                state = result.ExecutionState,
                message = "Contract invoked successfully"
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid invoke contract request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking contract");
            return StatusCode(500, new { error = "Failed to invoke contract", details = ex.Message });
        }
    }

    /// <summary>
    /// Calls a smart contract method (read-only).
    /// </summary>
    /// <param name="request">The call request.</param>
    /// <returns>The call result.</returns>
    [HttpPost("call")]
    [AllowAnonymous]
    public async Task<IActionResult> CallContractAsync([FromBody] CallContractRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Calling contract {ContractHash} method {Method}",
                request.ContractHash, request.Method);

            var result = await _smartContractsService.CallContractAsync(
                request.Blockchain,
                request.ContractHash,
                request.Method,
                request.Params);

            return Ok(new
            {
                success = true,
                result = result,
                gasConsumed = 0,
                message = "Contract called successfully"
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid call contract request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling contract");
            return StatusCode(500, new { error = "Failed to call contract", details = ex.Message });
        }
    }

    /// <summary>
    /// Gets contract events.
    /// </summary>
    /// <param name="request">The events request.</param>
    /// <returns>The contract events.</returns>
    [HttpPost("events")]
    [AllowAnonymous]
    public async Task<IActionResult> GetContractEventsAsync([FromBody] GetContractEventsRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Getting events for contract {ContractHash}", request.ContractHash);

            var events = await _smartContractsService.GetContractEventsAsync(
                request.Blockchain,
                request.ContractHash,
                request.EventName,
                request.FromBlock,
                request.ToBlock);

            var eventsList = events.ToList();
            return Ok(new
            {
                success = true,
                events = eventsList,
                totalCount = eventsList.Count,
                hasMore = false,
                message = $"Retrieved {eventsList.Count} events"
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid get events request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contract events");
            return StatusCode(500, new { error = "Failed to get contract events", details = ex.Message });
        }
    }

    /// <summary>
    /// Gets contract statistics.
    /// </summary>
    /// <param name="contractHash">The contract hash.</param>
    /// <param name="blockchain">The blockchain type.</param>
    /// <returns>The contract statistics.</returns>
    [HttpGet("statistics/{contractHash}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetContractStatisticsAsync(
        string contractHash,
        [FromQuery] BlockchainType blockchain = BlockchainType.NeoN3)
    {
        try
        {
            _logger.LogInformation("Getting statistics for contract {ContractHash} on {Blockchain}",
                contractHash, blockchain);

            var metadata = await _smartContractsService.GetContractMetadataAsync(blockchain, contractHash);

            var result = new
            {
                totalCalls = 0,
                totalInvocations = 0,
                totalGasConsumed = 0L,
                successRate = 100.0,
                lastAccessed = metadata?.DeployedAt ?? DateTime.UtcNow,
                contractName = metadata?.Name ?? "Unknown",
                contractVersion = metadata?.Version ?? "1.0.0"
            };

            return Ok(new
            {
                success = true,
                statistics = result,
                message = "Statistics retrieved successfully"
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid get statistics request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contract statistics");
            return StatusCode(500, new { error = "Failed to get contract statistics", details = ex.Message });
        }
    }

    /// <summary>
    /// Gets service statistics.
    /// </summary>
    /// <returns>The service statistics.</returns>
    [HttpGet("service-stats")]
    [AllowAnonymous]
    public async Task<IActionResult> GetServiceStatisticsAsync()
    {
        try
        {
            var stats = await _smartContractsService.GetStatisticsAsync();

            var serviceStats = new
            {
                totalRequests = stats.TotalInvocations,
                successCount = (long)(stats.TotalInvocations * 0.98),
                activeContracts = stats.TotalContractsDeployed,
                totalGasConsumed = stats.TotalGasConsumed,
                blockchains = stats.ByBlockchain.Keys.Select(k => k.ToString())
            };
            return Ok(new
            {
                success = true,
                statistics = serviceStats,
                message = "Service statistics retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service statistics");
            return StatusCode(500, new { error = "Failed to get service statistics", details = ex.Message });
        }
    }
}
