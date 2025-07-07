using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Api.Controllers;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Backup;
using NeoServiceLayer.Services.Backup.Models;

namespace NeoServiceLayer.Api.Controllers;

/// <summary>
/// API controller for backup and restore operations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[Tags("Backup")]
public class BackupController : BaseApiController
{
    private readonly IBackupService _backupService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupController"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="backupService">The backup service.</param>
    public BackupController(ILogger<BackupController> logger, IBackupService backupService)
        : base(logger)
    {
        _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
    }

    /// <summary>
    /// Creates a new backup job.
    /// </summary>
    /// <param name="request">The backup creation request.</param>
    /// <returns>The backup job details.</returns>
    [HttpPost]
    [Authorize(Roles = "Admin,BackupUser")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> CreateBackup([FromBody] CreateBackupRequest request)
    {
        try
        {
            Logger.LogInformation("Creating backup job for user {UserId}", GetCurrentUserId());

            var result = await _backupService.CreateBackupAsync(request, BlockchainType.NeoN3);
            return Ok(CreateResponse(result, "Backup job created successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "CreateBackup");
        }
    }

    /// <summary>
    /// Gets the status of a backup job.
    /// </summary>
    /// <param name="jobId">The backup job ID.</param>
    /// <returns>The backup job status.</returns>
    [HttpGet("{jobId}/status")]
    [Authorize(Roles = "Admin,BackupUser")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetBackupStatus(string jobId)
    {
        try
        {
            var result = await _backupService.GetBackupStatusAsync(new BackupStatusRequest { BackupId = jobId }, BlockchainType.NeoN3);
            return Ok(CreateResponse(result, "Backup status retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetBackupStatus");
        }
    }

    /// <summary>
    /// Lists all backup jobs for the current user.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>The list of backup jobs.</returns>
    [HttpGet]
    [Authorize(Roles = "Admin,BackupUser")]
    [ProducesResponseType(typeof(PaginatedResponse<object>), 200)]
    public async Task<IActionResult> GetBackups([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = GetCurrentUserId();
            var request = new ListBackupsRequest
            {
                PageNumber = page,
                PageSize = pageSize,
                FilterCriteria = new BackupFilterCriteria
                {
                    UserId = userId,
                    IncludeExpired = false
                },
                SortBy = "CreatedAt",
                SortDescending = true
            };

            var result = await _backupService.ListBackupsAsync(request, BlockchainType.NeoN3);

            var paginatedResponse = new PaginatedResponse<BackupEntry>
            {
                Data = result.Backups,
                TotalItems = result.TotalCount,
                Page = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = (int)Math.Ceiling((double)result.TotalCount / result.PageSize),
                Success = result.Success,
                Message = result.Success ? "Backups retrieved successfully" : result.ErrorMessage,
                Timestamp = DateTime.UtcNow
            };

            return Ok(paginatedResponse);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetBackups");
        }
    }

    /// <summary>
    /// Initiates a restore operation.
    /// </summary>
    /// <param name="request">The restore request.</param>
    /// <returns>The restore job details.</returns>
    [HttpPost("restore")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> RestoreBackup([FromBody] RestoreBackupRequest request)
    {
        try
        {
            Logger.LogInformation("Initiating restore operation for user {UserId}", GetCurrentUserId());

            var result = await _backupService.RestoreBackupAsync(request, BlockchainType.NeoN3);
            return Ok(CreateResponse(result, "Restore operation initiated successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "RestoreBackup");
        }
    }

    /// <summary>
    /// Deletes a backup.
    /// </summary>
    /// <param name="backupId">The backup ID.</param>
    /// <returns>Success status.</returns>
    [HttpDelete("{backupId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> DeleteBackup(string backupId)
    {
        try
        {
            await _backupService.DeleteBackupAsync(new DeleteBackupRequest { BackupId = backupId }, BlockchainType.NeoN3);
            return Ok(CreateResponse<object>(null, "Backup deleted successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "DeleteBackup");
        }
    }
}
