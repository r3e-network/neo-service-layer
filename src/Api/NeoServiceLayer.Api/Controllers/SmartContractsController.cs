using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.SmartContracts;
using NeoServiceLayer.Services.SmartContracts.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Api.Controllers;

/// <summary>
/// Controller for smart contract management operations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/smart-contracts")]
[Authorize]
[Tags("Smart Contracts")]
public class SmartContractsController : BaseApiController
{
    private readonly ISmartContractsService _smartContractsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmartContractsController"/> class.
    /// </summary>
    /// <param name="smartContractsService">The smart contracts service.</param>
    /// <param name="logger">The logger.</param>
    public SmartContractsController(ISmartContractsService smartContractsService, ILogger<SmartContractsController> logger)
        : base(logger)
    {
        _smartContractsService = smartContractsService ?? throw new ArgumentNullException(nameof(smartContractsService));
    }

    /// <summary>
    /// Deploys a smart contract.
    /// </summary>
    /// <param name="request">The deployment request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The deployment result.</returns>
    [HttpPost("{blockchainType}/deploy")]
    [Authorize(Roles = "Admin,Developer")]
    [ProducesResponseType(typeof(ApiResponse<Core.SmartContracts.ContractDeploymentResult>), 200)]
    public async Task<IActionResult> DeployContract(
        [FromBody] DeployContractRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var contractCode = Convert.FromBase64String(request.Code);
            var result = await _smartContractsService.DeployContractAsync(
                blockchain,
                contractCode,
                request.Parameters?.Values.ToArray());

            return Ok(CreateResponse(result, "Contract deployed successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "deploying contract");
        }
    }

    /// <summary>
    /// Invokes a smart contract method.
    /// </summary>
    /// <param name="request">The invocation request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The invocation result.</returns>
    [HttpPost("{blockchainType}/invoke")]
    [ProducesResponseType(typeof(ApiResponse<Core.SmartContracts.ContractInvocationResult>), 200)]
    public async Task<IActionResult> InvokeContract(
        [FromBody] InvokeContractRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _smartContractsService.InvokeContractAsync(
                blockchain,
                request.ContractHash,
                request.Method,
                request.Parameters.ToArray());

            return Ok(CreateResponse(result, "Contract invoked successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "invoking contract");
        }
    }

    /// <summary>
    /// Gets contract metadata.
    /// </summary>
    /// <param name="contractHash">The contract hash.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The contract metadata.</returns>
    [HttpGet("{blockchainType}/{contractHash}")]
    [ProducesResponseType(typeof(ApiResponse<Core.SmartContracts.ContractMetadata>), 200)]
    public async Task<IActionResult> GetContractMetadata(
        [FromRoute] string contractHash,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _smartContractsService.GetContractMetadataAsync(blockchain, contractHash);

            if (result == null)
            {
                return NotFound(CreateErrorResponse($"Contract not found: {contractHash}"));
            }

            return Ok(CreateResponse(result, "Contract metadata retrieved"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving contract metadata");
        }
    }

    /// <summary>
    /// Calls a read-only smart contract method.
    /// </summary>
    /// <param name="request">The call request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The call result.</returns>
    [HttpPost("{blockchainType}/call")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> CallContract(
        [FromBody] InvokeContractRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _smartContractsService.CallContractAsync(
                blockchain,
                request.ContractHash,
                request.Method,
                request.Parameters.ToArray());

            return Ok(CreateResponse(result, "Contract called successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "calling contract");
        }
    }

    /// <summary>
    /// Lists all deployed contracts.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The list of deployed contracts.</returns>
    [HttpGet("{blockchainType}/contracts")]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<BlockchainType, IEnumerable<Core.SmartContracts.ContractMetadata>>>), 200)]
    public async Task<IActionResult> ListDeployedContracts(
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _smartContractsService.ListAllDeployedContractsAsync(blockchain);

            return Ok(CreateResponse(result, "Deployed contracts retrieved"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "listing deployed contracts");
        }
    }

    /// <summary>
    /// Gets contract events.
    /// </summary>
    /// <param name="contractHash">The contract hash.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <param name="eventName">The event name filter.</param>
    /// <param name="fromBlock">The starting block number.</param>
    /// <param name="toBlock">The ending block number.</param>
    /// <returns>The contract events.</returns>
    [HttpGet("{blockchainType}/{contractHash}/events")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<Core.SmartContracts.ContractEvent>>), 200)]
    public async Task<IActionResult> GetContractEvents(
        [FromRoute] string contractHash,
        [FromRoute] string blockchainType,
        [FromQuery] string? eventName = null,
        [FromQuery] long? fromBlock = null,
        [FromQuery] long? toBlock = null)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _smartContractsService.GetContractEventsAsync(
                blockchain,
                contractHash,
                eventName,
                fromBlock,
                toBlock);

            return Ok(CreateResponse(result, "Contract events retrieved"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving contract events");
        }
    }

    /// <summary>
    /// Estimates gas for a contract invocation.
    /// </summary>
    /// <param name="request">The estimation request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The gas estimate.</returns>
    [HttpPost("{blockchainType}/estimate-gas")]
    [ProducesResponseType(typeof(ApiResponse<long>), 200)]
    public async Task<IActionResult> EstimateGas(
        [FromBody] InvokeContractRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _smartContractsService.EstimateGasAsync(
                blockchain,
                request.ContractHash,
                request.Method,
                request.Parameters.ToArray());

            return Ok(CreateResponse(result, "Gas estimate completed"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "estimating gas");
        }
    }

    /// <summary>
    /// Gets smart contract statistics.
    /// </summary>
    /// <returns>The smart contract statistics.</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ApiResponse<SmartContractStatistics>), 200)]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            var result = await _smartContractsService.GetStatisticsAsync();

            return Ok(CreateResponse(result, "Smart contract statistics retrieved"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving smart contract statistics");
        }
    }
}
