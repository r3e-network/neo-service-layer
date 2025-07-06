using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.SmartContracts;
using NeoServiceLayer.Services.SmartContracts.Models;

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
    [ProducesResponseType(typeof(ApiResponse<DeploymentResult>), 200)]
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
            var result = await _smartContractsService.DeployContractAsync(request, blockchain);

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
    [ProducesResponseType(typeof(ApiResponse<InvocationResult>), 200)]
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
            var result = await _smartContractsService.InvokeContractAsync(request, blockchain);

            return Ok(CreateResponse(result, "Contract invoked successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "invoking contract");
        }
    }

    /// <summary>
    /// Gets contract information.
    /// </summary>
    /// <param name="contractHash">The contract hash.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The contract information.</returns>
    [HttpGet("{blockchainType}/{contractHash}")]
    [ProducesResponseType(typeof(ApiResponse<ContractInfo>), 200)]
    public async Task<IActionResult> GetContract(
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
            var result = await _smartContractsService.GetContractAsync(contractHash, blockchain);

            return Ok(CreateResponse(result, "Contract information retrieved"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving contract information");
        }
    }
}