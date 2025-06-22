using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Compliance;
using NeoServiceLayer.Services.Compliance.Models;

namespace NeoServiceLayer.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ComplianceController : ControllerBase
{
    private readonly IComplianceService _complianceService;
    private readonly ILogger<ComplianceController> _logger;

    public ComplianceController(IComplianceService complianceService, ILogger<ComplianceController> logger)
    {
        _complianceService = complianceService;
        _logger = logger;
    }

    [HttpPost("check")]
    public async Task<IActionResult> CheckCompliance([FromBody] ComplianceCheckRequest request)
    {
        try
        {
            var result = await _complianceService.CheckComplianceAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking compliance");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("report")]
    public async Task<IActionResult> GenerateComplianceReport([FromBody] ComplianceReportRequest request)
    {
        try
        {
            var result = await _complianceService.GenerateComplianceReportAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating compliance report");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("rule")]
    public async Task<IActionResult> CreateComplianceRule([FromBody] CreateComplianceRuleRequest request)
    {
        try
        {
            var result = await _complianceService.CreateComplianceRuleAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating compliance rule");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("rule/{ruleId}")]
    public async Task<IActionResult> UpdateComplianceRule(string ruleId, [FromBody] UpdateComplianceRuleRequest request)
    {
        try
        {
            request.RuleId = ruleId;
            var result = await _complianceService.UpdateComplianceRuleAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating compliance rule");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("rule/{ruleId}")]
    public async Task<IActionResult> DeleteComplianceRule(string ruleId)
    {
        try
        {
            var request = new DeleteComplianceRuleRequest { RuleId = ruleId };
            var result = await _complianceService.DeleteComplianceRuleAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting compliance rule");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("rules")]
    public async Task<IActionResult> GetComplianceRules([FromQuery] int pageSize = 20, [FromQuery] int pageNumber = 1)
    {
        try
        {
            var request = new GetComplianceRulesRequest { PageSize = pageSize, PageNumber = pageNumber };
            var result = await _complianceService.GetComplianceRulesAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting compliance rules");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("audit")]
    public async Task<IActionResult> StartAudit([FromBody] StartAuditRequest request)
    {
        try
        {
            var result = await _complianceService.StartAuditAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting audit");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("audit/{auditId}")]
    public async Task<IActionResult> GetAuditStatus(string auditId)
    {
        try
        {
            var request = new GetAuditStatusRequest { AuditId = auditId };
            var result = await _complianceService.GetAuditStatusAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit status");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("violation")]
    public async Task<IActionResult> ReportViolation([FromBody] ReportViolationRequest request)
    {
        try
        {
            var result = await _complianceService.ReportViolationAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting violation");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("violations")]
    public async Task<IActionResult> GetViolations([FromQuery] int pageSize = 20, [FromQuery] int pageNumber = 1)
    {
        try
        {
            var request = new GetViolationsRequest { PageSize = pageSize, PageNumber = pageNumber };
            var result = await _complianceService.GetViolationsAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting violations");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("remediation/{violationId}")]
    public async Task<IActionResult> CreateRemediationPlan(string violationId, [FromBody] CreateRemediationPlanRequest request)
    {
        try
        {
            request.ViolationId = violationId;
            var result = await _complianceService.CreateRemediationPlanAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating remediation plan");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetComplianceDashboard()
    {
        try
        {
            var request = new ComplianceDashboardRequest();
            var result = await _complianceService.GetComplianceDashboardAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting compliance dashboard");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("certification")]
    public async Task<IActionResult> RequestCertification([FromBody] RequestCertificationRequest request)
    {
        try
        {
            var result = await _complianceService.RequestCertificationAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting certification");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
