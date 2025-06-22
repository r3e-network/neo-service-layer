using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Backup;
using NeoServiceLayer.Services.Backup.Models;

namespace NeoServiceLayer.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BackupController : ControllerBase
{
    private readonly IBackupService _backupService;
    private readonly ILogger<BackupController> _logger;

    public BackupController(IBackupService backupService, ILogger<BackupController> logger)
    {
        _backupService = backupService;
        _logger = logger;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateBackup([FromBody] CreateBackupRequest request)
    {
        try
        {
            var result = await _backupService.CreateBackupAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating backup");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("restore")]
    public async Task<IActionResult> RestoreBackup([FromBody] RestoreBackupRequest request)
    {
        try
        {
            var result = await _backupService.RestoreBackupAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring backup");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("status/{backupId}")]
    public async Task<IActionResult> GetBackupStatus(string backupId)
    {
        try
        {
            var request = new BackupStatusRequest { BackupId = backupId };
            var result = await _backupService.GetBackupStatusAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting backup status");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("list")]
    public async Task<IActionResult> ListBackups([FromQuery] int pageSize = 20, [FromQuery] int pageNumber = 1)
    {
        try
        {
            var request = new ListBackupsRequest { PageSize = pageSize, PageNumber = pageNumber };
            var result = await _backupService.ListBackupsAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing backups");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("{backupId}")]
    public async Task<IActionResult> DeleteBackup(string backupId)
    {
        try
        {
            var request = new DeleteBackupRequest { BackupId = backupId };
            var result = await _backupService.DeleteBackupAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting backup");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("validate/{backupId}")]
    public async Task<IActionResult> ValidateBackup(string backupId)
    {
        try
        {
            var request = new ValidateBackupRequest { BackupId = backupId };
            var result = await _backupService.ValidateBackupAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating backup");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("schedule")]
    public async Task<IActionResult> CreateScheduledBackup([FromBody] CreateScheduledBackupRequest request)
    {
        try
        {
            var result = await _backupService.CreateScheduledBackupAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating scheduled backup");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetBackupStatistics()
    {
        try
        {
            var request = new BackupStatisticsRequest();
            var result = await _backupService.GetBackupStatisticsAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting backup statistics");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
