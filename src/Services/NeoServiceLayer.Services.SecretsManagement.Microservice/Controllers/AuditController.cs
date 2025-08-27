using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Neo.SecretsManagement.Service.Models;
using Neo.SecretsManagement.Service.Services;
using System.Security.Claims;

namespace Neo.SecretsManagement.Service.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "SecretAdmin,SystemAdmin")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(
        IAuditService auditService,
        ILogger<AuditController> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Get audit logs with filtering and pagination
    /// </summary>
    [HttpGet("logs")]
    public async Task<ActionResult<AuditLogsResponse>> GetAuditLogs([FromQuery] AuditLogsRequest request)
    {
        try
        {
            var userId = GetUserId();

            // If not system admin, restrict to own logs
            if (!User.IsInRole("SystemAdmin") && string.IsNullOrEmpty(request.UserId))
            {
                request.UserId = userId;
            }

            var logs = await _auditService.GetAuditLogsAsync(
                request.UserId,
                request.Operation,
                request.FromDate,
                request.ToDate,
                request.Skip,
                request.Take
            );

            var totalCount = await _auditService.GetAuditLogCountAsync(
                request.UserId,
                request.Operation,
                request.FromDate,
                request.ToDate
            );

            var response = new AuditLogsResponse
            {
                Logs = logs,
                TotalCount = totalCount,
                Skip = request.Skip,
                Take = request.Take,
                HasMore = (request.Skip + request.Take) < totalCount
            };

            // Log the audit query itself (meta-audit)
            await _auditService.LogAsync(
                userId,
                "query",
                "audit_logs",
                "query",
                null,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["queried_user"] = request.UserId ?? "all",
                    ["operation_filter"] = request.Operation ?? "all",
                    ["from_date"] = request.FromDate?.ToString("O") ?? "none",
                    ["to_date"] = request.ToDate?.ToString("O") ?? "none",
                    ["result_count"] = logs.Count,
                    ["total_count"] = totalCount
                },
                GetClientIp(),
                Request.Headers.UserAgent.FirstOrDefault()
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit logs");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get audit log count
    /// </summary>
    [HttpGet("logs/count")]
    public async Task<ActionResult<AuditLogCountResponse>> GetAuditLogCount([FromQuery] AuditLogCountRequest request)
    {
        try
        {
            var userId = GetUserId();

            // If not system admin, restrict to own logs
            if (!User.IsInRole("SystemAdmin") && string.IsNullOrEmpty(request.UserId))
            {
                request.UserId = userId;
            }

            var count = await _auditService.GetAuditLogCountAsync(
                request.UserId,
                request.Operation,
                request.FromDate,
                request.ToDate
            );

            var response = new AuditLogCountResponse
            {
                Count = count,
                UserId = request.UserId,
                Operation = request.Operation,
                FromDate = request.FromDate,
                ToDate = request.ToDate
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit log count");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get operation statistics for a date range
    /// </summary>
    [HttpGet("statistics/operations")]
    public async Task<ActionResult<OperationStatisticsResponse>> GetOperationStatistics([FromQuery] StatisticsRequest request)
    {
        try
        {
            var userId = GetUserId();
            
            var fromDate = request.FromDate ?? DateTime.UtcNow.AddDays(-30);
            var toDate = request.ToDate ?? DateTime.UtcNow;

            var statistics = await _auditService.GetOperationStatisticsAsync(fromDate, toDate);

            var response = new OperationStatisticsResponse
            {
                FromDate = fromDate,
                ToDate = toDate,
                Statistics = statistics,
                TotalOperations = statistics.Values.Sum(),
                MostCommonOperation = statistics.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key,
                OperationCount = statistics.Count
            };

            await _auditService.LogAsync(
                userId,
                "query",
                "operation_statistics",
                "statistics",
                null,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["from_date"] = fromDate.ToString("O"),
                    ["to_date"] = toDate.ToString("O"),
                    ["total_operations"] = response.TotalOperations,
                    ["operation_types"] = response.OperationCount
                },
                GetClientIp(),
                Request.Headers.UserAgent.FirstOrDefault()
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get operation statistics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get user activity statistics for a date range
    /// </summary>
    [HttpGet("statistics/users")]
    public async Task<ActionResult<UserActivityStatisticsResponse>> GetUserActivityStatistics([FromQuery] StatisticsRequest request)
    {
        try
        {
            var userId = GetUserId();
            
            var fromDate = request.FromDate ?? DateTime.UtcNow.AddDays(-30);
            var toDate = request.ToDate ?? DateTime.UtcNow;

            var statistics = await _auditService.GetUserActivityStatisticsAsync(fromDate, toDate);

            var response = new UserActivityStatisticsResponse
            {
                FromDate = fromDate,
                ToDate = toDate,
                Statistics = statistics,
                TotalActivity = statistics.Values.Sum(),
                MostActiveUser = statistics.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key,
                ActiveUserCount = statistics.Count
            };

            await _auditService.LogAsync(
                userId,
                "query",
                "user_activity_statistics",
                "statistics",
                null,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["from_date"] = fromDate.ToString("O"),
                    ["to_date"] = toDate.ToString("O"),
                    ["total_activity"] = response.TotalActivity,
                    ["active_users"] = response.ActiveUserCount
                },
                GetClientIp(),
                Request.Headers.UserAgent.FirstOrDefault()
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user activity statistics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get audit dashboard summary
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<AuditDashboardResponse>> GetAuditDashboard([FromQuery] int daysBack = 30)
    {
        try
        {
            var userId = GetUserId();
            var fromDate = DateTime.UtcNow.AddDays(-daysBack);
            var toDate = DateTime.UtcNow;

            // Get parallel statistics
            var operationStatsTask = _auditService.GetOperationStatisticsAsync(fromDate, toDate);
            var userActivityStatsTask = _auditService.GetUserActivityStatisticsAsync(fromDate, toDate);
            var totalLogsTask = _auditService.GetAuditLogCountAsync(null, null, fromDate, toDate);
            var recentLogsTask = _auditService.GetAuditLogsAsync(null, null, fromDate, toDate, 0, 10);

            await Task.WhenAll(operationStatsTask, userActivityStatsTask, totalLogsTask, recentLogsTask);

            var operationStats = operationStatsTask.Result;
            var userActivityStats = userActivityStatsTask.Result;
            var totalLogs = totalLogsTask.Result;
            var recentLogs = recentLogsTask.Result;

            var response = new AuditDashboardResponse
            {
                FromDate = fromDate,
                ToDate = toDate,
                DaysBack = daysBack,
                TotalLogs = totalLogs,
                TotalOperations = operationStats.Values.Sum(),
                UniqueOperationTypes = operationStats.Count,
                ActiveUsers = userActivityStats.Count,
                MostCommonOperation = operationStats.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key ?? "none",
                MostActiveUser = userActivityStats.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key ?? "none",
                RecentLogs = recentLogs.Take(5).ToList(),
                TopOperations = operationStats.OrderByDescending(kvp => kvp.Value).Take(5).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                TopUsers = userActivityStats.OrderByDescending(kvp => kvp.Value).Take(5).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                SuccessRate = totalLogs > 0 ? (double)recentLogs.Count(l => l.Success) / Math.Min(totalLogs, recentLogs.Count) * 100 : 0,
                ErrorCount = recentLogs.Count(l => !l.Success)
            };

            await _auditService.LogAsync(
                userId,
                "view",
                "audit_dashboard",
                "dashboard",
                null,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["days_back"] = daysBack,
                    ["total_logs"] = totalLogs,
                    ["unique_operations"] = response.UniqueOperationTypes,
                    ["active_users"] = response.ActiveUsers
                },
                GetClientIp(),
                Request.Headers.UserAgent.FirstOrDefault()
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit dashboard");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Cleanup old audit logs (System Admin only)
    /// </summary>
    [HttpPost("cleanup")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<AuditCleanupResponse>> CleanupOldLogs([FromBody] AuditCleanupRequest request)
    {
        try
        {
            var userId = GetUserId();
            var clientIp = GetClientIp();

            var beforeCount = await _auditService.GetAuditLogCountAsync();
            await _auditService.CleanupOldLogsAsync(request.RetentionDays);
            var afterCount = await _auditService.GetAuditLogCountAsync();

            var deletedCount = beforeCount - afterCount;

            var response = new AuditCleanupResponse
            {
                Success = true,
                RetentionDays = request.RetentionDays,
                DeletedCount = deletedCount,
                RemainingCount = afterCount,
                CleanedUpAt = DateTime.UtcNow
            };

            await _auditService.LogAsync(
                userId,
                "cleanup",
                "audit_logs",
                "cleanup",
                null,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["retention_days"] = request.RetentionDays,
                    ["deleted_count"] = deletedCount,
                    ["remaining_count"] = afterCount
                },
                clientIp,
                Request.Headers.UserAgent.FirstOrDefault()
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old audit logs");
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
public class AuditLogsRequest
{
    public string? UserId { get; set; }
    public string? Operation { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
}

public class AuditLogsResponse
{
    public List<AuditLogEntry> Logs { get; set; } = new();
    public int TotalCount { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public bool HasMore { get; set; }
}

public class AuditLogCountRequest
{
    public string? UserId { get; set; }
    public string? Operation { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class AuditLogCountResponse
{
    public int Count { get; set; }
    public string? UserId { get; set; }
    public string? Operation { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class StatisticsRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class OperationStatisticsResponse
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public Dictionary<string, int> Statistics { get; set; } = new();
    public int TotalOperations { get; set; }
    public string? MostCommonOperation { get; set; }
    public int OperationCount { get; set; }
}

public class UserActivityStatisticsResponse
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public Dictionary<string, int> Statistics { get; set; } = new();
    public int TotalActivity { get; set; }
    public string? MostActiveUser { get; set; }
    public int ActiveUserCount { get; set; }
}

public class AuditDashboardResponse
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int DaysBack { get; set; }
    public int TotalLogs { get; set; }
    public int TotalOperations { get; set; }
    public int UniqueOperationTypes { get; set; }
    public int ActiveUsers { get; set; }
    public string MostCommonOperation { get; set; } = string.Empty;
    public string MostActiveUser { get; set; } = string.Empty;
    public List<AuditLogEntry> RecentLogs { get; set; } = new();
    public Dictionary<string, int> TopOperations { get; set; } = new();
    public Dictionary<string, int> TopUsers { get; set; } = new();
    public double SuccessRate { get; set; }
    public int ErrorCount { get; set; }
}

public class AuditCleanupRequest
{
    public int RetentionDays { get; set; } = 90;
}

public class AuditCleanupResponse
{
    public bool Success { get; set; }
    public int RetentionDays { get; set; }
    public int DeletedCount { get; set; }
    public int RemainingCount { get; set; }
    public DateTime CleanedUpAt { get; set; }
}