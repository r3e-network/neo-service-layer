using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Neo.SecretsManagement.Service.Models;
using Neo.SecretsManagement.Service.Services;
using System.Security.Claims;

namespace Neo.SecretsManagement.Service.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class SecretsController : ControllerBase
{
    private readonly ISecretService _secretService;
    private readonly IAuditService _auditService;
    private readonly ILogger<SecretsController> _logger;

    public SecretsController(
        ISecretService secretService,
        IAuditService auditService,
        ILogger<SecretsController> logger)
    {
        _secretService = secretService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new secret
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SecretWriter,SecretAdmin,SystemAdmin")]
    public async Task<ActionResult<SecretResponse>> CreateSecret([FromBody] CreateSecretRequest request)
    {
        try
        {
            var userId = GetUserId();
            var clientIp = GetClientIp();

            var response = await _secretService.CreateSecretAsync(request, userId);

            await _auditService.LogAsync(
                userId,
                "create",
                "secret",
                response.Id.ToString(),
                request.Path,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["secret_name"] = request.Name,
                    ["secret_type"] = request.Type.ToString()
                },
                clientIp,
                Request.Headers.UserAgent.FirstOrDefault()
            );

            return CreatedAtAction(nameof(GetSecret), new { path = request.Path }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create secret at path {Path}", request.Path);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get a secret by path
    /// </summary>
    [HttpGet("{*path}")]
    [Authorize(Roles = "SecretReader,SecretWriter,SecretAdmin,SystemAdmin")]
    public async Task<ActionResult<SecretResponse>> GetSecret(
        string path, 
        [FromQuery] bool includeValue = false)
    {
        try
        {
            var userId = GetUserId();
            var clientIp = GetClientIp();

            var response = await _secretService.GetSecretAsync(path, userId, includeValue);
            if (response == null)
            {
                return NotFound(new { error = "Secret not found" });
            }

            await _auditService.LogAsync(
                userId,
                "read",
                "secret",
                response.Id.ToString(),
                path,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["include_value"] = includeValue
                },
                clientIp,
                Request.Headers.UserAgent.FirstOrDefault()
            );

            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get secret at path {Path}", path);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get a secret by ID
    /// </summary>
    [HttpGet("id/{secretId:guid}")]
    [Authorize(Roles = "SecretReader,SecretWriter,SecretAdmin,SystemAdmin")]
    public async Task<ActionResult<SecretResponse>> GetSecretById(
        Guid secretId,
        [FromQuery] bool includeValue = false)
    {
        try
        {
            var userId = GetUserId();
            var clientIp = GetClientIp();

            var response = await _secretService.GetSecretByIdAsync(secretId, userId, includeValue);
            if (response == null)
            {
                return NotFound(new { error = "Secret not found" });
            }

            await _auditService.LogAsync(
                userId,
                "read",
                "secret",
                secretId.ToString(),
                response.Path,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["include_value"] = includeValue
                },
                clientIp,
                Request.Headers.UserAgent.FirstOrDefault()
            );

            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get secret by ID {SecretId}", secretId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// List secrets with filtering and pagination
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SecretReader,SecretWriter,SecretAdmin,SystemAdmin")]
    public async Task<ActionResult<List<SecretResponse>>> ListSecrets([FromQuery] ListSecretsRequest request)
    {
        try
        {
            var userId = GetUserId();
            var clientIp = GetClientIp();

            var response = await _secretService.ListSecretsAsync(request, userId);

            await _auditService.LogAsync(
                userId,
                "list",
                "secrets",
                "multiple",
                request.PathPrefix,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["result_count"] = response.Count,
                    ["path_prefix"] = request.PathPrefix ?? "all",
                    ["secret_type"] = request.Type?.ToString() ?? "all"
                },
                clientIp,
                Request.Headers.UserAgent.FirstOrDefault()
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list secrets");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Update a secret
    /// </summary>
    [HttpPut("{*path}")]
    [Authorize(Roles = "SecretWriter,SecretAdmin,SystemAdmin")]
    public async Task<IActionResult> UpdateSecret(string path, [FromBody] UpdateSecretRequest request)
    {
        try
        {
            var userId = GetUserId();
            var clientIp = GetClientIp();

            var success = await _secretService.UpdateSecretAsync(path, request, userId);
            if (!success)
            {
                return NotFound(new { error = "Secret not found" });
            }

            await _auditService.LogAsync(
                userId,
                "update",
                "secret",
                "path-based",
                path,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["updated_fields"] = new[] { "value", "description", "tags", "expires_at" }
                },
                clientIp,
                Request.Headers.UserAgent.FirstOrDefault()
            );

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update secret at path {Path}", path);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete a secret
    /// </summary>
    [HttpDelete("{*path}")]
    [Authorize(Roles = "SecretWriter,SecretAdmin,SystemAdmin")]
    public async Task<IActionResult> DeleteSecret(string path)
    {
        try
        {
            var userId = GetUserId();
            var clientIp = GetClientIp();

            var success = await _secretService.DeleteSecretAsync(path, userId);
            if (!success)
            {
                return NotFound(new { error = "Secret not found" });
            }

            await _auditService.LogAsync(
                userId,
                "delete",
                "secret",
                "path-based",
                path,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["deletion_type"] = "soft_delete"
                },
                clientIp,
                Request.Headers.UserAgent.FirstOrDefault()
            );

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete secret at path {Path}", path);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Share a secret with another user or service
    /// </summary>
    [HttpPost("{*path}/share")]
    [Authorize(Roles = "SecretWriter,SecretAdmin,SystemAdmin")]
    public async Task<ActionResult<ShareSecretResponse>> ShareSecret(
        string path, 
        [FromBody] ShareSecretRequest request)
    {
        try
        {
            var userId = GetUserId();
            var clientIp = GetClientIp();

            var response = await _secretService.ShareSecretAsync(path, request, userId);

            await _auditService.LogAsync(
                userId,
                "share",
                "secret",
                response.SecretId.ToString(),
                path,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["shared_with"] = request.SharedWithUserId,
                    ["permissions"] = request.Permissions.ToString(),
                    ["expires_at"] = request.ExpiresAt?.ToString("O") ?? "never"
                },
                clientIp,
                Request.Headers.UserAgent.FirstOrDefault()
            );

            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to share secret at path {Path}", path);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Revoke a secret share
    /// </summary>
    [HttpDelete("shares/{shareId:guid}")]
    [Authorize(Roles = "SecretWriter,SecretAdmin,SystemAdmin")]
    public async Task<IActionResult> RevokeShare(Guid shareId)
    {
        try
        {
            var userId = GetUserId();
            var clientIp = GetClientIp();

            var success = await _secretService.RevokeShareAsync(shareId, userId);
            if (!success)
            {
                return NotFound(new { error = "Share not found" });
            }

            await _auditService.LogAsync(
                userId,
                "revoke_share",
                "secret_share",
                shareId.ToString(),
                null,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["share_id"] = shareId.ToString()
                },
                clientIp,
                Request.Headers.UserAgent.FirstOrDefault()
            );

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke share {ShareId}", shareId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Rotate a secret (generate new value)
    /// </summary>
    [HttpPost("{*path}/rotate")]
    [Authorize(Roles = "SecretWriter,SecretAdmin,SystemAdmin")]
    public async Task<ActionResult<SecretResponse>> RotateSecret(
        string path,
        [FromBody] RotateSecretRequest request)
    {
        try
        {
            var userId = GetUserId();
            var clientIp = GetClientIp();

            var response = await _secretService.RotateSecretAsync(path, request, userId);

            await _auditService.LogAsync(
                userId,
                "rotate",
                "secret",
                response.Id.ToString(),
                path,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["new_version"] = response.CurrentVersion,
                    ["rotation_type"] = "manual"
                },
                clientIp,
                Request.Headers.UserAgent.FirstOrDefault()
            );

            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rotate secret at path {Path}", path);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get secret versions
    /// </summary>
    [HttpGet("{*path}/versions")]
    [Authorize(Roles = "SecretReader,SecretWriter,SecretAdmin,SystemAdmin")]
    public async Task<ActionResult<List<SecretVersion>>> GetSecretVersions(string path)
    {
        try
        {
            var userId = GetUserId();
            var clientIp = GetClientIp();

            var versions = await _secretService.GetSecretVersionsAsync(path, userId);

            await _auditService.LogAsync(
                userId,
                "list_versions",
                "secret",
                "path-based",
                path,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["version_count"] = versions.Count
                },
                clientIp,
                Request.Headers.UserAgent.FirstOrDefault()
            );

            return Ok(versions);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get secret versions for path {Path}", path);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get a specific version of a secret
    /// </summary>
    [HttpGet("{*path}/versions/{version:int}")]
    [Authorize(Roles = "SecretReader,SecretWriter,SecretAdmin,SystemAdmin")]
    public async Task<ActionResult<SecretResponse>> GetSecretVersion(
        string path,
        int version,
        [FromQuery] bool includeValue = false)
    {
        try
        {
            var userId = GetUserId();
            var clientIp = GetClientIp();

            var response = await _secretService.GetSecretVersionAsync(path, version, userId, includeValue);
            if (response == null)
            {
                return NotFound(new { error = "Secret version not found" });
            }

            await _auditService.LogAsync(
                userId,
                "read_version",
                "secret",
                response.Id.ToString(),
                path,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["version"] = version,
                    ["include_value"] = includeValue
                },
                clientIp,
                Request.Headers.UserAgent.FirstOrDefault()
            );

            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get secret version {Version} for path {Path}", version, path);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get secret statistics
    /// </summary>
    [HttpGet("statistics")]
    [Authorize(Roles = "SecretReader,SecretWriter,SecretAdmin,SystemAdmin")]
    public async Task<ActionResult<SecretStatistics>> GetStatistics()
    {
        try
        {
            var userId = GetUserId();
            var statistics = await _secretService.GetSecretStatisticsAsync(userId);

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get secret statistics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get secrets expiring soon
    /// </summary>
    [HttpGet("expiring")]
    [Authorize(Roles = "SecretReader,SecretWriter,SecretAdmin,SystemAdmin")]
    public async Task<ActionResult<List<SecretResponse>>> GetExpiringSecrets([FromQuery] int daysAhead = 30)
    {
        try
        {
            var userId = GetUserId();
            var secrets = await _secretService.GetExpiringSecretsAsync(daysAhead, userId);

            return Ok(secrets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get expiring secrets");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Validate access to a secret
    /// </summary>
    [HttpPost("{*path}/validate-access")]
    [Authorize(Roles = "SecretReader,SecretWriter,SecretAdmin,SystemAdmin")]
    public async Task<ActionResult<bool>> ValidateAccess(
        string path,
        [FromBody] ValidateAccessRequest request)
    {
        try
        {
            var userId = GetUserId();
            var hasAccess = await _secretService.ValidateAccessAsync(path, userId, request.Operation);

            return Ok(new { hasAccess });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate access for path {Path}", path);
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

// Request/Response models for validation
public class ValidateAccessRequest
{
    public SecretOperation Operation { get; set; }
}