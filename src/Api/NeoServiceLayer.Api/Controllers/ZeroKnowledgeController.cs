using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NeoServiceLayer.Api.Controllers;
using NeoServiceLayer.Services.ZeroKnowledge;
using NeoServiceLayer.Services.ZeroKnowledge.Models;

namespace NeoServiceLayer.Api.Controllers;

/// <summary>
/// API controller for zero-knowledge proof operations.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Tags("Zero Knowledge")]
public class ZeroKnowledgeController : BaseApiController
{
    private readonly ZeroKnowledgeService _zkService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZeroKnowledgeController"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="zkService">The zero-knowledge service.</param>
    public ZeroKnowledgeController(ILogger<ZeroKnowledgeController> logger, ZeroKnowledgeService zkService)
        : base(logger)
    {
        _zkService = zkService;
    }

    /// <summary>
    /// Compiles a zero-knowledge circuit.
    /// </summary>
    /// <param name="request">The circuit compilation request.</param>
    /// <returns>The compiled circuit information.</returns>
    [HttpPost("circuits/compile")]
    [Authorize(Roles = "Admin,Developer")]
    [ProducesResponseType(typeof(ApiResponse<CircuitCompilationResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> CompileCircuit([FromBody] CircuitCompilationRequest request)
    {
        try
        {
            Logger.LogInformation("Compiling ZK circuit for user {UserId}", GetCurrentUserId());
            
            var result = await _zkService.CompileCircuitAsync(request);
            return Ok(CreateResponse(result, "Circuit compiled successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "CompileCircuit");
        }
    }

    /// <summary>
    /// Generates a zero-knowledge proof.
    /// </summary>
    /// <param name="request">The proof generation request.</param>
    /// <returns>The generated proof.</returns>
    [HttpPost("proofs/generate")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<ProofGenerationResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GenerateProof([FromBody] ProofGenerationRequest request)
    {
        try
        {
            Logger.LogInformation("Generating ZK proof for user {UserId}", GetCurrentUserId());
            
            var result = await _zkService.GenerateProofAsync(request);
            return Ok(CreateResponse(result, "Proof generated successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GenerateProof");
        }
    }

    /// <summary>
    /// Verifies a zero-knowledge proof.
    /// </summary>
    /// <param name="request">The proof verification request.</param>
    /// <returns>The verification result.</returns>
    [HttpPost("proofs/verify")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<ProofVerificationResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> VerifyProof([FromBody] ProofVerificationRequest request)
    {
        try
        {
            Logger.LogInformation("Verifying ZK proof for user {UserId}", GetCurrentUserId());
            
            var result = await _zkService.VerifyProofAsync(request);
            return Ok(CreateResponse(result, "Proof verification completed"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "VerifyProof");
        }
    }

    /// <summary>
    /// Gets the setup parameters for a circuit.
    /// </summary>
    /// <param name="circuitId">The circuit ID.</param>
    /// <returns>The setup parameters.</returns>
    [HttpGet("circuits/{circuitId}/setup")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<CircuitSetupResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetCircuitSetup(string circuitId)
    {
        try
        {
            var result = await _zkService.GetCircuitSetupAsync(circuitId);
            return Ok(CreateResponse(result, "Circuit setup retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetCircuitSetup");
        }
    }

    /// <summary>
    /// Lists available zero-knowledge circuits.
    /// </summary>
    /// <returns>The list of available circuits.</returns>
    [HttpGet("circuits")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CircuitInfo>>), 200)]
    public async Task<IActionResult> GetAvailableCircuits()
    {
        try
        {
            var result = await _zkService.GetAvailableCircuitsAsync();
            return Ok(CreateResponse(result, "Available circuits retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetAvailableCircuits");
        }
    }

    /// <summary>
    /// Performs a trusted setup ceremony for a circuit.
    /// </summary>
    /// <param name="request">The trusted setup request.</param>
    /// <returns>The setup ceremony result.</returns>
    [HttpPost("circuits/trusted-setup")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<TrustedSetupResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> PerformTrustedSetup([FromBody] TrustedSetupRequest request)
    {
        try
        {
            Logger.LogInformation("Performing trusted setup for user {UserId}", GetCurrentUserId());
            
            var result = await _zkService.PerformTrustedSetupAsync(request);
            return Ok(CreateResponse(result, "Trusted setup completed successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "PerformTrustedSetup");
        }
    }

    /// <summary>
    /// Gets proof statistics and metrics.
    /// </summary>
    /// <param name="fromDate">Start date for statistics.</param>
    /// <param name="toDate">End date for statistics.</param>
    /// <returns>The proof statistics.</returns>
    [HttpGet("statistics")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<ZkStatistics>), 200)]
    public async Task<IActionResult> GetProofStatistics([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var result = await _zkService.GetProofStatisticsAsync(fromDate, toDate);
            return Ok(CreateResponse(result, "Proof statistics retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetProofStatistics");
        }
    }
}