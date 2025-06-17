using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.ZeroKnowledge;
using NeoServiceLayer.Services.ZeroKnowledge.Models;

namespace NeoServiceLayer.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ZeroKnowledgeController : ControllerBase
{
    private readonly IZeroKnowledgeService _zeroKnowledgeService;
    private readonly ILogger<ZeroKnowledgeController> _logger;

    public ZeroKnowledgeController(IZeroKnowledgeService zeroKnowledgeService, ILogger<ZeroKnowledgeController> logger)
    {
        _zeroKnowledgeService = zeroKnowledgeService;
        _logger = logger;
    }

    [HttpPost("proof/generate")]
    public async Task<IActionResult> GenerateProof([FromBody] GenerateProofRequest request)
    {
        try
        {
            var result = await _zeroKnowledgeService.GenerateProofAsync(request, BlockchainType.NeoN3);
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
            var result = await _zeroKnowledgeService.VerifyProofAsync(request, BlockchainType.NeoN3);
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
        try
        {
            var result = await _zeroKnowledgeService.CompileCircuitAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compiling circuit");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("commitment/create")]
    public async Task<IActionResult> CreateCommitment([FromBody] CreateCommitmentRequest request)
    {
        try
        {
            var result = await _zeroKnowledgeService.CreateCommitmentAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating commitment");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("commitment/reveal")]
    public async Task<IActionResult> RevealCommitment([FromBody] RevealCommitmentRequest request)
    {
        try
        {
            var result = await _zeroKnowledgeService.RevealCommitmentAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revealing commitment");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("merkle-tree/create")]
    public async Task<IActionResult> CreateMerkleTree([FromBody] CreateMerkleTreeRequest request)
    {
        try
        {
            var result = await _zeroKnowledgeService.CreateMerkleTreeAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating merkle tree");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("merkle-tree/proof")]
    public async Task<IActionResult> GenerateMerkleProof([FromBody] GenerateMerkleProofRequest request)
    {
        try
        {
            var result = await _zeroKnowledgeService.GenerateMerkleProofAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating merkle proof");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("range-proof/create")]
    public async Task<IActionResult> CreateRangeProof([FromBody] CreateRangeProofRequest request)
    {
        try
        {
            var result = await _zeroKnowledgeService.CreateRangeProofAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating range proof");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("bulletproof/generate")]
    public async Task<IActionResult> GenerateBulletproof([FromBody] GenerateBulletproofRequest request)
    {
        try
        {
            var result = await _zeroKnowledgeService.GenerateBulletproofAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating bulletproof");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("proof/{proofId}")]
    public async Task<IActionResult> GetProof(string proofId)
    {
        try
        {
            var request = new GetProofRequest { ProofId = proofId };
            var result = await _zeroKnowledgeService.GetProofAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting proof");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("circuit/{circuitId}")]
    public async Task<IActionResult> GetCircuit(string circuitId)
    {
        try
        {
            var request = new GetCircuitRequest { CircuitId = circuitId };
            var result = await _zeroKnowledgeService.GetCircuitAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting circuit");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("batch-verify")]
    public async Task<IActionResult> BatchVerifyProofs([FromBody] BatchVerifyProofsRequest request)
    {
        try
        {
            var result = await _zeroKnowledgeService.BatchVerifyProofsAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error batch verifying proofs");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetZkStatistics()
    {
        try
        {
            var request = new ZkStatisticsRequest();
            var result = await _zeroKnowledgeService.GetZkStatisticsAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ZK statistics");
            return StatusCode(500, new { error = ex.Message });
        }
    }
} 