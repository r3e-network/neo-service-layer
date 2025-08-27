using Microsoft.EntityFrameworkCore;
using Neo.SecretsManagement.Service.Data;
using Neo.SecretsManagement.Service.Models;

namespace Neo.SecretsManagement.Service.Services;

public class AuditService : IAuditService
{
    private readonly SecretsDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        SecretsDbContext context,
        ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(string userId, string operation, string resourceType, string resourceId, 
                              string? resourcePath = null, bool success = true, string? errorMessage = null, 
                              Dictionary<string, object>? details = null, string? clientIp = null, string? userAgent = null)
    {
        try
        {
            var auditEntry = new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                UserId = userId,
                ServiceName = "secrets-management",
                Operation = operation,
                ResourceType = resourceType,
                ResourceId = resourceId,
                ResourcePath = resourcePath,
                Success = success,
                ErrorMessage = errorMessage,
                ClientIpAddress = clientIp ?? "unknown",
                UserAgent = userAgent ?? "unknown",
                Details = details ?? new Dictionary<string, object>()
            };

            _context.AuditLogs.Add(auditEntry);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Audit log created: User {UserId} performed {Operation} on {ResourceType}:{ResourceId} - Success: {Success}",
                userId, operation, resourceType, resourceId, success);
        }
        catch (Exception ex)
        {
            // Never throw from audit logging - it could break the main operation
            _logger.LogError(ex, "Failed to create audit log entry for user {UserId}, operation {Operation}",
                userId, operation);
        }
    }

    public async Task<List<AuditLogEntry>> GetAuditLogsAsync(string? userId = null, string? operation = null, 
                                                            DateTime? fromDate = null, DateTime? toDate = null, 
                                                            int skip = 0, int take = 100)
    {
        try
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(a => a.UserId == userId);
            }

            if (!string.IsNullOrEmpty(operation))
            {
                query = query.Where(a => a.Operation == operation);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(a => a.Timestamp >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(a => a.Timestamp <= toDate.Value);
            }

            return await query
                .OrderByDescending(a => a.Timestamp)
                .Skip(skip)
                .Take(Math.Min(take, 1000)) // Limit to 1000 max
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit logs");
            return new List<AuditLogEntry>();
        }
    }

    public async Task<int> GetAuditLogCountAsync(string? userId = null, string? operation = null, 
                                                DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(a => a.UserId == userId);
            }

            if (!string.IsNullOrEmpty(operation))
            {
                query = query.Where(a => a.Operation == operation);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(a => a.Timestamp >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(a => a.Timestamp <= toDate.Value);
            }

            return await query.CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit log count");
            return 0;
        }
    }

    public async Task CleanupOldLogsAsync(int retentionDays)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
            
            var deletedCount = await _context.AuditLogs
                .Where(a => a.Timestamp < cutoffDate)
                .ExecuteDeleteAsync();

            if (deletedCount > 0)
            {
                _logger.LogInformation("Cleaned up {DeletedCount} audit log entries older than {RetentionDays} days",
                    deletedCount, retentionDays);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old audit logs");
        }
    }

    public async Task<Dictionary<string, int>> GetOperationStatisticsAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            var stats = await _context.AuditLogs
                .Where(a => a.Timestamp >= fromDate && a.Timestamp <= toDate)
                .GroupBy(a => a.Operation)
                .Select(g => new { Operation = g.Key, Count = g.Count() })
                .ToListAsync();

            return stats.ToDictionary(s => s.Operation, s => s.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get operation statistics");
            return new Dictionary<string, int>();
        }
    }

    public async Task<Dictionary<string, int>> GetUserActivityStatisticsAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            var stats = await _context.AuditLogs
                .Where(a => a.Timestamp >= fromDate && a.Timestamp <= toDate)
                .GroupBy(a => a.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .OrderByDescending(s => s.Count)
                .Take(50) // Top 50 most active users
                .ToListAsync();

            return stats.ToDictionary(s => s.UserId, s => s.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user activity statistics");
            return new Dictionary<string, int>();
        }
    }
}