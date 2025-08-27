using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Neo.SecretsManagement.Service.Models;
using Neo.SecretsManagement.Service.Services;
using System.Security.Claims;

namespace Neo.SecretsManagement.Service.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "SecretAdmin,SystemAdmin")]
public class BackupsController : ControllerBase
{
    private readonly IBackupService _backupService;
    private readonly IAuditService _auditService;
    private readonly ILogger<BackupsController> _logger;

    public BackupsController(
        IBackupService backupService,
        IAuditService auditService,
        ILogger<BackupsController> logger)
    {
        _backupService = backupService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new backup
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SecretBackup>> CreateBackup([FromBody] CreateBackupRequest request)
    {
        try
        {
            var userId = GetUserId();
            var clientIp = GetClientIp();

            var backupRequest = new BackupRequest
            {
                Name = request.Name,
                Description = request.Description,
                BackupType = request.BackupType,
                ExpiresAt = request.ExpiresAt,
                Since = request.Since,
                SecretPaths = request.SecretPaths
            };

            var backup = await _backupService.CreateBackupAsync(backupRequest, userId);

            await _auditService.LogAsync(
                userId,
                "create",
                "backup",
                backup.Id.ToString(),
                null,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["backup_name"] = backup.Name,
                    ["backup_type"] = backup.BackupType.ToString(),
                    ["expires_at"] = backup.ExpiresAt?.ToString("O") ?? "never"
                },
                clientIp,
                Request.Headers.UserAgent.FirstOrDefault()
            );

            return CreatedAtAction(nameof(GetBackup), new { backupId = backup.Id }, backup);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get a backup by ID
    /// </summary>
    [HttpGet("{backupId:guid}")]
    public async Task<ActionResult<SecretBackup>> GetBackup(Guid backupId)
    {
        try
        {
            var userId = GetUserId();
            var backup = await _backupService.GetBackupAsync(backupId, userId);
            
            if (backup == null)
            {
                return NotFound(new { error = "Backup not found or access denied" });
            }

            return Ok(backup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get backup {BackupId}", backupId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// List all backups
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<SecretBackup>>> ListBackups()
    {
        try
        {
            var userId = GetUserId();
            var backups = await _backupService.ListBackupsAsync(userId);

            return Ok(backups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list backups");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Restore from a backup
    /// </summary>
    [HttpPost("{backupId:guid}/restore")]
    public async Task<ActionResult<RestoreBackupResponse>> RestoreBackup(
        Guid backupId,
        [FromBody] RestoreBackupRequest request)
    {
        try
        {
            var userId = GetUserId();
            var clientIp = GetClientIp();

            var restoreRequest = new RestoreRequest
            {
                BackupId = backupId,
                OverwriteExisting = request.OverwriteExisting
            };

            var success = await _backupService.RestoreBackupAsync(restoreRequest, userId);

            await _auditService.LogAsync(
                userId,
                "restore",
                "backup",
                backupId.ToString(),
                null,
                success,
                success ? null : "Restore operation failed",
                new Dictionary<string, object>
                {
                    ["overwrite_existing"] = request.OverwriteExisting,
                    ["restore_result"] = success
                },
                clientIp,
                Request.Headers.UserAgent.FirstOrDefault()
            );

            var response = new RestoreBackupResponse
            {
                Success = success,
                BackupId = backupId,
                RestoredAt = DateTime.UtcNow,
                Message = success ? "Backup restored successfully" : "Backup restore failed"
            };

            return success ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore backup {BackupId}", backupId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete a backup
    /// </summary>
    [HttpDelete("{backupId:guid}")]
    public async Task<IActionResult> DeleteBackup(Guid backupId)
    {
        try
        {
            var userId = GetUserId();
            var clientIp = GetClientIp();

            var success = await _backupService.DeleteBackupAsync(backupId, userId);
            if (!success)
            {
                return NotFound(new { error = "Backup not found or access denied" });
            }

            await _auditService.LogAsync(
                userId,
                "delete",
                "backup",
                backupId.ToString(),
                null,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["backup_id"] = backupId.ToString(),
                    ["deleted_at"] = DateTime.UtcNow.ToString("O")
                },
                clientIp,
                Request.Headers.UserAgent.FirstOrDefault()
            );

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete backup {BackupId}", backupId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Validate backup integrity
    /// </summary>
    [HttpPost("{backupId:guid}/validate")]
    public async Task<ActionResult<BackupValidationResponse>> ValidateBackup(Guid backupId)
    {
        try
        {
            var userId = GetUserId();
            var clientIp = GetClientIp();

            var isValid = await _backupService.ValidateBackupIntegrityAsync(backupId);

            await _auditService.LogAsync(
                userId,
                "validate",
                "backup",
                backupId.ToString(),
                null,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["validation_result"] = isValid,
                    ["validated_at"] = DateTime.UtcNow.ToString("O")
                },
                clientIp,
                Request.Headers.UserAgent.FirstOrDefault()
            );

            var response = new BackupValidationResponse
            {
                BackupId = backupId,
                IsValid = isValid,
                ValidatedAt = DateTime.UtcNow,
                Message = isValid ? "Backup integrity verified" : "Backup integrity check failed"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate backup {BackupId}", backupId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get backup statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<BackupStatisticsResponse>> GetBackupStatistics()
    {
        try
        {
            var userId = GetUserId();
            var backups = await _backupService.ListBackupsAsync(userId);

            var statistics = new BackupStatisticsResponse
            {
                TotalBackups = backups.Count,
                CompletedBackups = backups.Count(b => b.Status == BackupStatus.Completed),
                InProgressBackups = backups.Count(b => b.Status == BackupStatus.InProgress),
                FailedBackups = backups.Count(b => b.Status == BackupStatus.Failed),
                TotalSize = backups.Where(b => b.Status == BackupStatus.Completed).Sum(b => b.Size),
                OldestBackup = backups.Where(b => b.Status == BackupStatus.Completed).MinBy(b => b.CreatedAt)?.CreatedAt,
                NewestBackup = backups.Where(b => b.Status == BackupStatus.Completed).MaxBy(b => b.CreatedAt)?.CreatedAt,
                ExpiringBackups = backups.Count(b => b.ExpiresAt.HasValue && b.ExpiresAt.Value <= DateTime.UtcNow.AddDays(7)),
                BackupsByType = backups.GroupBy(b => b.BackupType)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count())
            };

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get backup statistics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Cleanup expired backups (Admin only)
    /// </summary>
    [HttpPost("cleanup-expired")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<CleanupResponse>> CleanupExpiredBackups()
    {
        try
        {
            var userId = GetUserId();
            var clientIp = GetClientIp();

            var beforeCount = (await _backupService.ListBackupsAsync(userId)).Count;
            await _backupService.CleanupExpiredBackupsAsync();
            var afterCount = (await _backupService.ListBackupsAsync(userId)).Count;

            var deletedCount = beforeCount - afterCount;

            await _auditService.LogAsync(
                userId,
                "cleanup",
                "backups",
                "expired",
                null,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["deleted_count"] = deletedCount,
                    ["cleanup_type"] = "expired",
                    ["cleanup_time"] = DateTime.UtcNow.ToString("O")
                },
                clientIp,
                Request.Headers.UserAgent.FirstOrDefault()
            );

            var response = new CleanupResponse
            {
                Success = true,
                DeletedCount = deletedCount,
                CleanedUpAt = DateTime.UtcNow,
                Message = $"Successfully cleaned up {deletedCount} expired backups"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired backups");
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
public class CreateBackupRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public BackupType BackupType { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? Since { get; set; }
    public List<string>? SecretPaths { get; set; }
}

public class RestoreBackupRequest
{
    public bool OverwriteExisting { get; set; } = false;
}

public class RestoreBackupResponse
{
    public bool Success { get; set; }
    public Guid BackupId { get; set; }
    public DateTime RestoredAt { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class BackupValidationResponse
{
    public Guid BackupId { get; set; }
    public bool IsValid { get; set; }
    public DateTime ValidatedAt { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class BackupStatisticsResponse
{
    public int TotalBackups { get; set; }
    public int CompletedBackups { get; set; }
    public int InProgressBackups { get; set; }
    public int FailedBackups { get; set; }
    public long TotalSize { get; set; }
    public DateTime? OldestBackup { get; set; }
    public DateTime? NewestBackup { get; set; }
    public int ExpiringBackups { get; set; }
    public Dictionary<string, int> BackupsByType { get; set; } = new();
}

public class CleanupResponse
{
    public bool Success { get; set; }
    public int DeletedCount { get; set; }
    public DateTime CleanedUpAt { get; set; }
    public string Message { get; set; } = string.Empty;
}