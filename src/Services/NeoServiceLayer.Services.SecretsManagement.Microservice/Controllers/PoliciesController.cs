using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Neo.SecretsManagement.Service.Models;
using Neo.SecretsManagement.Service.Services;
using System.Security.Claims;

namespace Neo.SecretsManagement.Service.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "SecretAdmin,SystemAdmin")]
public class PoliciesController : ControllerBase
{
    private readonly ISecretPolicyService _policyService;
    private readonly IAuditService _auditService;
    private readonly ILogger<PoliciesController> _logger;

    public PoliciesController(
        ISecretPolicyService policyService,
        IAuditService auditService,
        ILogger<PoliciesController> logger)
    {
        _policyService = policyService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new secret policy
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SecretPolicy>> CreatePolicy([FromBody] CreateSecretPolicyRequest request)
    {
        try
        {
            var userId = GetUserId();
            var clientIp = GetClientIp();

            var policy = new SecretPolicy
            {
                Name = request.Name,
                Description = request.Description,
                PathPatterns = request.PathPatterns,
                AllowedOperations = request.AllowedOperations,
                DeniedOperations = request.DeniedOperations,
                RequiredRoles = request.RequiredRoles,
                TimeRestrictions = request.TimeRestrictions,
                IpWhitelist = request.IpWhitelist,
                MaxAccessCount = request.MaxAccessCount,
                Priority = request.Priority,
                IsEnabled = request.IsEnabled
            };

            var createdPolicy = await _policyService.CreatePolicyAsync(policy, userId);

            await _auditService.LogAsync(
                userId,
                "create",
                "policy",
                createdPolicy.Id.ToString(),
                null,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["policy_name"] = createdPolicy.Name,
                    ["priority"] = createdPolicy.Priority,
                    ["enabled"] = createdPolicy.IsEnabled
                },
                clientIp,
                Request.Headers.UserAgent.FirstOrDefault()
            );

            return CreatedAtAction(nameof(GetPolicy), new { policyId = createdPolicy.Id }, createdPolicy);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create policy");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get a policy by ID
    /// </summary>
    [HttpGet("{policyId:guid}")]
    public async Task<ActionResult<SecretPolicy>> GetPolicy(Guid policyId)
    {
        try
        {
            var policy = await _policyService.GetPolicyAsync(policyId);
            if (policy == null)
            {
                return NotFound(new { error = "Policy not found" });
            }

            return Ok(policy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get policy {PolicyId}", policyId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// List all policies
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<SecretPolicy>>> ListPolicies()
    {
        try
        {
            var userId = GetUserId();
            var policies = await _policyService.ListPoliciesAsync(userId);

            return Ok(policies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list policies");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Update a policy
    /// </summary>
    [HttpPut("{policyId:guid}")]
    public async Task<IActionResult> UpdatePolicy(Guid policyId, [FromBody] UpdateSecretPolicyRequest request)
    {
        try
        {
            var userId = GetUserId();
            var clientIp = GetClientIp();

            var policy = new SecretPolicy
            {
                Name = request.Name,
                Description = request.Description,
                PathPatterns = request.PathPatterns,
                AllowedOperations = request.AllowedOperations,
                DeniedOperations = request.DeniedOperations,
                RequiredRoles = request.RequiredRoles,
                TimeRestrictions = request.TimeRestrictions,
                IpWhitelist = request.IpWhitelist,
                MaxAccessCount = request.MaxAccessCount,
                Priority = request.Priority,
                IsEnabled = request.IsEnabled
            };

            var success = await _policyService.UpdatePolicyAsync(policyId, policy, userId);
            if (!success)
            {
                return NotFound(new { error = "Policy not found" });
            }

            await _auditService.LogAsync(
                userId,
                "update",
                "policy",
                policyId.ToString(),
                null,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["policy_name"] = policy.Name,
                    ["priority"] = policy.Priority,
                    ["enabled"] = policy.IsEnabled
                },
                clientIp,
                Request.Headers.UserAgent.FirstOrDefault()
            );

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update policy {PolicyId}", policyId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete a policy
    /// </summary>
    [HttpDelete("{policyId:guid}")]
    public async Task<IActionResult> DeletePolicy(Guid policyId)
    {
        try
        {
            var userId = GetUserId();
            var clientIp = GetClientIp();

            var success = await _policyService.DeletePolicyAsync(policyId, userId);
            if (!success)
            {
                return NotFound(new { error = "Policy not found" });
            }

            await _auditService.LogAsync(
                userId,
                "delete",
                "policy",
                policyId.ToString(),
                null,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["policy_id"] = policyId.ToString()
                },
                clientIp,
                Request.Headers.UserAgent.FirstOrDefault()
            );

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete policy {PolicyId}", policyId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Evaluate policies for a specific path and operation
    /// </summary>
    [HttpPost("evaluate")]
    public async Task<ActionResult<PolicyEvaluationResponse>> EvaluatePolicy([FromBody] EvaluatePolicyRequest request)
    {
        try
        {
            var userId = GetUserId();
            var clientIp = GetClientIp();

            var context = new Dictionary<string, object>
            {
                ["client_ip"] = clientIp ?? "unknown",
                ["user_agent"] = Request.Headers.UserAgent.FirstOrDefault() ?? "unknown",
                ["request_time"] = DateTime.UtcNow
            };

            // Add any additional context from headers or claims
            if (User.HasClaim("roles", "admin"))
            {
                context["user_roles"] = new[] { "admin", "user" };
            }
            else
            {
                context["user_roles"] = new[] { "user" };
            }

            var allowed = await _policyService.EvaluatePolicyAsync(request.Path, userId, request.Operation, context);

            var response = new PolicyEvaluationResponse
            {
                Path = request.Path,
                Operation = request.Operation,
                UserId = userId,
                Allowed = allowed,
                EvaluatedAt = DateTime.UtcNow
            };

            await _auditService.LogAsync(
                userId,
                "evaluate",
                "policy",
                "evaluation",
                request.Path,
                allowed,
                allowed ? null : "Access denied by policy",
                new Dictionary<string, object>
                {
                    ["operation"] = request.Operation.ToString(),
                    ["evaluation_result"] = allowed
                },
                clientIp,
                Request.Headers.UserAgent.FirstOrDefault()
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate policy for path {Path}", request.Path);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get policies applicable to a specific path
    /// </summary>
    [HttpGet("applicable")]
    public async Task<ActionResult<List<SecretPolicy>>> GetApplicablePolicies([FromQuery] string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return BadRequest(new { error = "Path is required" });
            }

            var policies = await _policyService.GetApplicablePoliciesAsync(path);
            return Ok(policies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get applicable policies for path {Path}", path);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Test policy patterns against a path
    /// </summary>
    [HttpPost("test-patterns")]
    public ActionResult<PatternTestResponse> TestPatterns([FromBody] PatternTestRequest request)
    {
        try
        {
            var results = new List<PatternTestResult>();

            foreach (var pattern in request.Patterns)
            {
                try
                {
                    var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    var matches = regex.IsMatch(request.Path);

                    results.Add(new PatternTestResult
                    {
                        Pattern = pattern,
                        Matches = matches,
                        IsValid = true,
                        Error = null
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new PatternTestResult
                    {
                        Pattern = pattern,
                        Matches = false,
                        IsValid = false,
                        Error = ex.Message
                    });
                }
            }

            var response = new PatternTestResponse
            {
                Path = request.Path,
                Results = results
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test patterns");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) 
            ?? User.FindFirstValue("sub") 
            ?? User.FindFirstValue("user_id") 
            ?? "anonymous";
    }

    private string? GetClientIp()
    {
        return Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim()
            ?? Request.Headers["X-Real-IP"].FirstOrDefault()
            ?? HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}

// Request/Response DTOs
public class CreateSecretPolicyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> PathPatterns { get; set; } = new();
    public List<SecretOperation>? AllowedOperations { get; set; }
    public List<SecretOperation>? DeniedOperations { get; set; }
    public List<string>? RequiredRoles { get; set; }
    public List<string>? TimeRestrictions { get; set; }
    public List<string>? IpWhitelist { get; set; }
    public int? MaxAccessCount { get; set; }
    public int Priority { get; set; } = 100;
    public bool IsEnabled { get; set; } = true;
}

public class UpdateSecretPolicyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> PathPatterns { get; set; } = new();
    public List<SecretOperation>? AllowedOperations { get; set; }
    public List<SecretOperation>? DeniedOperations { get; set; }
    public List<string>? RequiredRoles { get; set; }
    public List<string>? TimeRestrictions { get; set; }
    public List<string>? IpWhitelist { get; set; }
    public int? MaxAccessCount { get; set; }
    public int Priority { get; set; } = 100;
    public bool IsEnabled { get; set; } = true;
}

public class EvaluatePolicyRequest
{
    public string Path { get; set; } = string.Empty;
    public SecretOperation Operation { get; set; }
}

public class PolicyEvaluationResponse
{
    public string Path { get; set; } = string.Empty;
    public SecretOperation Operation { get; set; }
    public string UserId { get; set; } = string.Empty;
    public bool Allowed { get; set; }
    public DateTime EvaluatedAt { get; set; }
}

public class PatternTestRequest
{
    public string Path { get; set; } = string.Empty;
    public List<string> Patterns { get; set; } = new();
}

public class PatternTestResponse
{
    public string Path { get; set; } = string.Empty;
    public List<PatternTestResult> Results { get; set; } = new();
}

public class PatternTestResult
{
    public string Pattern { get; set; } = string.Empty;
    public bool Matches { get; set; }
    public bool IsValid { get; set; }
    public string? Error { get; set; }
}