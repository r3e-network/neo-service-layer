using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Services.ZeroKnowledge;
using NeoServiceLayer.Services.ZeroKnowledge.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;


namespace NeoServiceLayer.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ZeroKnowledgeController : ControllerBase
{
    private readonly NeoServiceLayer.Services.ZeroKnowledge.IZeroKnowledgeService _zeroKnowledgeService;
    private readonly ILogger<ZeroKnowledgeController> _logger;

    public ZeroKnowledgeController(NeoServiceLayer.Services.ZeroKnowledge.IZeroKnowledgeService zeroKnowledgeService, ILogger<ZeroKnowledgeController> logger)
    {
        _zeroKnowledgeService = zeroKnowledgeService;
        _logger = logger;
    }

    [HttpPost("proof/generate")]
    public async Task<IActionResult> GenerateProof([FromBody] GenerateProofRequest request)
    {
        try
        {
            // Convert to Core model
            var coreRequest = new NeoServiceLayer.Core.Models.ProofRequest
            {
                CircuitId = request.CircuitId,
                PrivateInputs = request.PrivateInputs.ToDictionary(x => x.Key, x => (object)x.Value),
                PublicInputs = request.PublicInputs.ToDictionary(x => x.Key, x => (object)x.Value),
                ProofType = NeoServiceLayer.Core.Models.ProofType.SNARK,
                Parameters = new Dictionary<string, object>()
            };

            var result = await _zeroKnowledgeService.GenerateProofAsync(coreRequest, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating proof");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("proof/verify")]
    public async Task<IActionResult> VerifyProof([FromBody] VerifyProofRequest request)
    {
        try
        {
            // Convert to Core model
            var coreRequest = new NeoServiceLayer.Core.Models.ProofVerification
            {
                Proof = request.ProofData,
                ProofData = Convert.FromBase64String(request.ProofData),
                PublicInputs = request.PublicInputs.ToDictionary(x => x.Key, x => (object)x.Value),
                PublicSignals = request.PublicInputs.ToDictionary(x => x.Key, x => (object)x.Value),
                CircuitId = request.CircuitId,
                Parameters = new Dictionary<string, object>()
            };

            var result = await _zeroKnowledgeService.VerifyProofAsync(coreRequest, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying proof");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("circuit/compile")]
    public async Task<IActionResult> CompileCircuit([FromBody] CompileCircuitRequest request)
    {
        // Method not available in interface - return not implemented
        return StatusCode(501, new { error = "Circuit compilation not implemented in current interface" });
    }

    [HttpPost("commitment/create")]
    public Task<IActionResult> CreateCommitment([FromBody] CreateCommitmentRequest request)
    {
        return Task.FromResult<IActionResult>(StatusCode(501, new { error = "CreateCommitment not implemented in current interface" }));
    }

    [HttpPost("commitment/reveal")]
    public Task<IActionResult> RevealCommitment([FromBody] RevealCommitmentRequest request)
    {
        return Task.FromResult<IActionResult>(StatusCode(501, new { error = "RevealCommitment not implemented in current interface" }));
    }

    [HttpPost("merkle-tree/create")]
    public Task<IActionResult> CreateMerkleTree([FromBody] CreateMerkleTreeRequest request)
    {
        return Task.FromResult<IActionResult>(StatusCode(501, new { error = "CreateMerkleTree not implemented in current interface" }));
    }

    [HttpPost("merkle-tree/proof")]
    public Task<IActionResult> GenerateMerkleProof([FromBody] GenerateMerkleProofRequest request)
    {
        return Task.FromResult<IActionResult>(StatusCode(501, new { error = "GenerateMerkleProof not implemented in current interface" }));
    }

    [HttpPost("range-proof/create")]
    public Task<IActionResult> CreateRangeProof([FromBody] CreateRangeProofRequest request)
    {
        return Task.FromResult<IActionResult>(StatusCode(501, new { error = "CreateRangeProof not implemented in current interface" }));
    }

    [HttpPost("bulletproof/generate")]
    public Task<IActionResult> GenerateBulletproof([FromBody] GenerateBulletproofRequest request)
    {
        return Task.FromResult<IActionResult>(StatusCode(501, new { error = "GenerateBulletproof not implemented in current interface" }));
    }

    [HttpGet("proof/{proofId}")]
    public Task<IActionResult> GetProof(string proofId)
    {
        return Task.FromResult<IActionResult>(StatusCode(501, new { error = "GetProof not implemented in current interface" }));
    }

    [HttpGet("circuit/{circuitId}")]
    public Task<IActionResult> GetCircuit(string circuitId)
    {
        return Task.FromResult<IActionResult>(StatusCode(501, new { error = "GetCircuit not implemented in current interface" }));
    }

    [HttpPost("batch-verify")]
    public Task<IActionResult> BatchVerifyProofs([FromBody] BatchVerifyProofsRequest request)
    {
        return Task.FromResult<IActionResult>(StatusCode(501, new { error = "BatchVerifyProofs not implemented in current interface" }));
    }

    [HttpGet("statistics")]
    public Task<IActionResult> GetZkStatistics()
    {
        return Task.FromResult<IActionResult>(StatusCode(501, new { error = "GetZkStatistics not implemented in current interface" }));
    }
}
