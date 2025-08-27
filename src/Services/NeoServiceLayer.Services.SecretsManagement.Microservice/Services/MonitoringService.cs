using Microsoft.Extensions.Diagnostics.HealthChecks;
using Neo.SecretsManagement.Service.Data;
using Neo.SecretsManagement.Service.Services;
using System.Diagnostics;
using System.Text.Json;

namespace Neo.SecretsManagement.Service.Services;

public interface IMonitoringService
{
    Task<ServiceHealthStatus> GetHealthStatusAsync();
    Task<ServiceMetrics> GetMetricsAsync();
    Task<PerformanceMetrics> GetPerformanceMetricsAsync();
    Task<SecurityMetrics> GetSecurityMetricsAsync();
    Task RecordOperationAsync(string operation, TimeSpan duration, bool success);
    Task RecordSecurityEventAsync(string eventType, string details, string? userId = null);
}

public class MonitoringService : IMonitoringService
{
    private readonly SecretsDbContext _context;
    private readonly IHsmService _hsmService;
    private readonly IAuditService _auditService;
    private readonly ILogger<MonitoringService> _logger;
    private readonly HealthCheckService _healthCheckService;

    // In-memory metrics storage (in production, use Redis or similar)
    private static readonly Dictionary<string, OperationMetrics> _operationMetrics = new();
    private static readonly List<SecurityEvent> _securityEvents = new();
    private static readonly object _metricsLock = new();

    public MonitoringService(
        SecretsDbContext context,
        IHsmService hsmService,
        IAuditService auditService,
        ILogger<MonitoringService> logger,
        HealthCheckService healthCheckService)
    {
        _context = context;
        _hsmService = hsmService;
        _auditService = auditService;
        _logger = logger;
        _healthCheckService = healthCheckService;
    }

    public async Task<ServiceHealthStatus> GetHealthStatusAsync()
    {
        try
        {
            var healthReport = await _healthCheckService.CheckHealthAsync();
            var hsmStatus = await _hsmService.GetStatusAsync();
            
            var status = new ServiceHealthStatus
            {
                OverallStatus = healthReport.Status.ToString(),
                Timestamp = DateTime.UtcNow,
                Checks = new Dictionary<string, HealthCheckResult>()
            };

            // Add health check results
            foreach (var check in healthReport.Entries)
            {
                status.Checks[check.Key] = new HealthCheckResult
                {
                    Status = check.Value.Status.ToString(),
                    Description = check.Value.Description,
                    Duration = check.Value.Duration,
                    Exception = check.Value.Exception?.Message,
                    Data = check.Value.Data?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? "null")
                };
            }

            // Add database connectivity
            status.DatabaseConnected = await CheckDatabaseConnectivityAsync();

            // Add HSM status
            status.HsmStatus = new HsmHealthStatus
            {
                Available = hsmStatus.IsAvailable,
                Version = hsmStatus.Version,
                ActiveSlots = hsmStatus.ActiveSlots,
                TotalSlots = hsmStatus.TotalSlots,
                CpuUsage = hsmStatus.CpuUsage,
                MemoryUsage = hsmStatus.MemoryUsage,
                LastHealthCheck = hsmStatus.LastHealthCheck,
                Alarms = hsmStatus.Alarms
            };

            // Add service metrics
            status.ServiceMetrics = await GetBasicServiceMetricsAsync();

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get health status");
            return new ServiceHealthStatus
            {
                OverallStatus = HealthStatus.Unhealthy.ToString(),
                Timestamp = DateTime.UtcNow,
                Error = ex.Message
            };
        }
    }

    public async Task<ServiceMetrics> GetMetricsAsync()
    {
        try
        {
            var metrics = new ServiceMetrics
            {
                Timestamp = DateTime.UtcNow
            };

            // Get secret statistics
            var totalSecrets = await _context.Secrets.CountAsync();
            var activeSecrets = await _context.Secrets.CountAsync(s => s.Status == SecretStatus.Active);
            var expiredSecrets = await _context.Secrets.CountAsync(s => s.ExpiresAt.HasValue && s.ExpiresAt.Value < DateTime.UtcNow);
            var expiringSecrets = await _context.Secrets.CountAsync(s => s.ExpiresAt.HasValue && s.ExpiresAt.Value <= DateTime.UtcNow.AddDays(30));

            metrics.SecretMetrics = new SecretMetrics
            {
                TotalSecrets = totalSecrets,
                ActiveSecrets = activeSecrets,
                ExpiredSecrets = expiredSecrets,
                ExpiringSecrets = expiringSecrets,
                SecretsByType = await GetSecretsByTypeAsync()
            };

            // Get encryption key statistics
            var totalKeys = await _context.EncryptionKeys.CountAsync();
            var activeKeys = await _context.EncryptionKeys.CountAsync(k => k.IsActive);

            metrics.KeyMetrics = new KeyMetrics
            {
                TotalKeys = totalKeys,
                ActiveKeys = activeKeys,
                KeysByAlgorithm = await GetKeysByAlgorithmAsync()
            };

            // Get audit statistics
            var today = DateTime.UtcNow.Date;
            var auditLogsToday = await _auditService.GetAuditLogCountAsync(null, null, today, today.AddDays(1));
            var auditLogsThisWeek = await _auditService.GetAuditLogCountAsync(null, null, today.AddDays(-7), DateTime.UtcNow);

            metrics.AuditMetrics = new AuditMetrics
            {
                LogsToday = auditLogsToday,
                LogsThisWeek = auditLogsThisWeek,
                TopOperations = await _auditService.GetOperationStatisticsAsync(today.AddDays(-7), DateTime.UtcNow)
            };

            // Get backup statistics
            var totalBackups = await _context.SecretBackups.CountAsync();
            var completedBackups = await _context.SecretBackups.CountAsync(b => b.Status == BackupStatus.Completed);

            metrics.BackupMetrics = new BackupMetrics
            {
                TotalBackups = totalBackups,
                CompletedBackups = completedBackups,
                TotalBackupSize = await _context.SecretBackups
                    .Where(b => b.Status == BackupStatus.Completed)
                    .SumAsync(b => b.Size)
            };

            // Get operation metrics
            lock (_metricsLock)
            {
                metrics.OperationMetrics = _operationMetrics.Values.ToList();
            }

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get service metrics");
            throw;
        }
    }

    public async Task<PerformanceMetrics> GetPerformanceMetricsAsync()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var hsmMetrics = await _hsmService.GetPerformanceMetricsAsync();

            var metrics = new PerformanceMetrics
            {
                Timestamp = DateTime.UtcNow,
                CpuUsage = GetCpuUsage(),
                MemoryUsage = process.WorkingSet64,
                ThreadCount = process.Threads.Count,
                HandleCount = process.HandleCount,
                GcCollections = new Dictionary<int, long>
                {
                    [0] = GC.CollectionCount(0),
                    [1] = GC.CollectionCount(1),
                    [2] = GC.CollectionCount(2)
                },
                TotalMemory = GC.GetTotalMemory(false)
            };

            // Add HSM performance metrics
            if (hsmMetrics != null)
            {
                metrics.HsmMetrics = new HsmPerformanceData
                {
                    OperationsPerSecond = hsmMetrics.OperationsPerSecond,
                    AverageResponseTime = hsmMetrics.AverageResponseTime,
                    ActiveConnections = hsmMetrics.ActiveConnections,
                    CpuUtilization = hsmMetrics.CpuUtilization,
                    MemoryUtilization = hsmMetrics.MemoryUtilization,
                    ErrorRate = hsmMetrics.ErrorRate
                };
            }

            // Add database performance
            metrics.DatabaseMetrics = await GetDatabasePerformanceAsync();

            // Add operation performance from in-memory metrics
            lock (_metricsLock)
            {
                metrics.OperationPerformance = _operationMetrics.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new OperationPerformanceData
                    {
                        AverageResponseTime = kvp.Value.AverageResponseTime,
                        TotalOperations = kvp.Value.TotalOperations,
                        SuccessRate = kvp.Value.SuccessRate,
                        ErrorCount = kvp.Value.ErrorCount,
                        LastOperation = kvp.Value.LastOperation
                    }
                );
            }

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get performance metrics");
            throw;
        }
    }

    public async Task<SecurityMetrics> GetSecurityMetricsAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            var last24Hours = now.AddHours(-24);
            var last7Days = now.AddDays(-7);

            var metrics = new SecurityMetrics
            {
                Timestamp = now
            };

            // Get failed authentication attempts from audit logs
            var failedLogins = await _auditService.GetAuditLogCountAsync(null, "authenticate", last24Hours, now);
            var successfulLogins = await _auditService.GetAuditLogCountAsync(null, "authenticate", last24Hours, now);

            metrics.AuthenticationMetrics = new AuthenticationMetrics
            {
                FailedLoginAttempts24h = failedLogins,
                SuccessfulLogins24h = successfulLogins,
                LoginSuccessRate = successfulLogins + failedLogins > 0 
                    ? (double)successfulLogins / (successfulLogins + failedLogins) * 100 
                    : 0
            };

            // Get access violations
            var accessViolations = await GetSecurityViolationsAsync(last24Hours, now);
            metrics.AccessViolations24h = accessViolations;

            // Get security events from in-memory storage
            lock (_metricsLock)
            {
                var recentEvents = _securityEvents
                    .Where(e => e.Timestamp >= last24Hours)
                    .ToList();

                metrics.SecurityEvents = recentEvents.GroupBy(e => e.EventType)
                    .ToDictionary(g => g.Key, g => g.Count());

                metrics.SuspiciousActivities = recentEvents
                    .Where(e => e.EventType.Contains("suspicious") || e.EventType.Contains("anomaly"))
                    .Count();
            }

            // Get policy violations
            metrics.PolicyViolations = await GetPolicyViolationsAsync(last7Days, now);

            // Get encryption health
            metrics.EncryptionHealth = await GetEncryptionHealthAsync();

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get security metrics");
            throw;
        }
    }

    public async Task RecordOperationAsync(string operation, TimeSpan duration, bool success)
    {
        try
        {
            lock (_metricsLock)
            {
                if (!_operationMetrics.ContainsKey(operation))
                {
                    _operationMetrics[operation] = new OperationMetrics
                    {
                        Operation = operation,
                        TotalOperations = 0,
                        TotalDuration = TimeSpan.Zero,
                        SuccessCount = 0,
                        ErrorCount = 0,
                        FirstOperation = DateTime.UtcNow,
                        LastOperation = DateTime.UtcNow
                    };
                }

                var metrics = _operationMetrics[operation];
                metrics.TotalOperations++;
                metrics.TotalDuration = metrics.TotalDuration.Add(duration);
                metrics.LastOperation = DateTime.UtcNow;

                if (success)
                {
                    metrics.SuccessCount++;
                }
                else
                {
                    metrics.ErrorCount++;
                }

                // Update calculated properties
                metrics.AverageResponseTime = metrics.TotalDuration.TotalMilliseconds / metrics.TotalOperations;
                metrics.SuccessRate = metrics.TotalOperations > 0 
                    ? (double)metrics.SuccessCount / metrics.TotalOperations * 100 
                    : 0;
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record operation metrics for {Operation}", operation);
        }
    }

    public async Task RecordSecurityEventAsync(string eventType, string details, string? userId = null)
    {
        try
        {
            var securityEvent = new SecurityEvent
            {
                EventType = eventType,
                Details = details,
                UserId = userId,
                Timestamp = DateTime.UtcNow
            };

            lock (_metricsLock)
            {
                _securityEvents.Add(securityEvent);

                // Keep only last 1000 events in memory
                while (_securityEvents.Count > 1000)
                {
                    _securityEvents.RemoveAt(0);
                }
            }

            // Also log to audit service for persistence
            await _auditService.LogAsync(
                userId ?? "system",
                "security_event",
                "security",
                eventType,
                null,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["event_type"] = eventType,
                    ["details"] = details
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record security event {EventType}", eventType);
        }
    }

    private async Task<bool> CheckDatabaseConnectivityAsync()
    {
        try
        {
            await _context.Database.CanConnectAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<Dictionary<string, object>> GetBasicServiceMetricsAsync()
    {
        var metrics = new Dictionary<string, object>();

        try
        {
            var process = Process.GetCurrentProcess();
            metrics["uptime_seconds"] = (DateTime.UtcNow - process.StartTime).TotalSeconds;
            metrics["memory_mb"] = process.WorkingSet64 / 1024 / 1024;
            metrics["cpu_usage_percent"] = GetCpuUsage();
            metrics["thread_count"] = process.Threads.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get basic service metrics");
            metrics["error"] = ex.Message;
        }

        return metrics;
    }

    private async Task<Dictionary<string, int>> GetSecretsByTypeAsync()
    {
        try
        {
            var secretsByType = await _context.Secrets
                .Where(s => s.Status == SecretStatus.Active)
                .GroupBy(s => s.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Type.ToString(), x => x.Count);

            return secretsByType;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get secrets by type");
            return new Dictionary<string, int>();
        }
    }

    private async Task<Dictionary<string, int>> GetKeysByAlgorithmAsync()
    {
        try
        {
            var keysByAlgorithm = await _context.EncryptionKeys
                .Where(k => k.IsActive)
                .GroupBy(k => k.Algorithm)
                .Select(g => new { Algorithm = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Algorithm, x => x.Count);

            return keysByAlgorithm;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get keys by algorithm");
            return new Dictionary<string, int>();
        }
    }

    private async Task<DatabasePerformanceData> GetDatabasePerformanceAsync()
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var canConnect = await _context.Database.CanConnectAsync();
            stopwatch.Stop();

            return new DatabasePerformanceData
            {
                ConnectionTime = stopwatch.ElapsedMilliseconds,
                IsConnected = canConnect,
                ActiveConnections = 1, // Simplified - in real implementation, query connection pool
                ConnectionPoolSize = 100 // Simplified - get from configuration
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get database performance metrics");
            return new DatabasePerformanceData
            {
                ConnectionTime = -1,
                IsConnected = false,
                Error = ex.Message
            };
        }
    }

    private async Task<int> GetSecurityViolationsAsync(DateTime from, DateTime to)
    {
        try
        {
            // Get audit logs that indicate security violations
            var violations = await _auditService.GetAuditLogCountAsync(null, null, from, to);
            // Filter for failed operations that might indicate violations
            return violations; // Simplified - in real implementation, analyze audit logs for violations
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get security violations");
            return 0;
        }
    }

    private async Task<int> GetPolicyViolationsAsync(DateTime from, DateTime to)
    {
        try
        {
            // Count policy evaluation failures from audit logs
            var policyLogs = await _auditService.GetAuditLogsAsync(null, "evaluate_policy", from, to);
            return policyLogs.Count(l => !l.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get policy violations");
            return 0;
        }
    }

    private async Task<EncryptionHealthData> GetEncryptionHealthAsync()
    {
        try
        {
            var hsmStatus = await _hsmService.GetStatusAsync();
            var totalKeys = await _context.EncryptionKeys.CountAsync();
            var activeKeys = await _context.EncryptionKeys.CountAsync(k => k.IsActive);

            return new EncryptionHealthData
            {
                HsmAvailable = hsmStatus.IsAvailable,
                TotalKeys = totalKeys,
                ActiveKeys = activeKeys,
                KeyRotationHealth = activeKeys > 0 ? 100 : 0, // Simplified health score
                EncryptionErrors24h = 0 // Would track from metrics in real implementation
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get encryption health");
            return new EncryptionHealthData
            {
                HsmAvailable = false,
                Error = ex.Message
            };
        }
    }

    private double GetCpuUsage()
    {
        try
        {
            // Simplified CPU usage calculation
            // In production, use performance counters or System.Diagnostics.PerformanceCounter
            var process = Process.GetCurrentProcess();
            return Math.Round(process.TotalProcessorTime.TotalMilliseconds / Environment.ProcessorCount / 1000, 2);
        }
        catch
        {
            return 0;
        }
    }
}

// Data models for monitoring
public class ServiceHealthStatus
{
    public string OverallStatus { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, HealthCheckResult> Checks { get; set; } = new();
    public bool DatabaseConnected { get; set; }
    public HsmHealthStatus? HsmStatus { get; set; }
    public Dictionary<string, object> ServiceMetrics { get; set; } = new();
    public string? Error { get; set; }
}

public class HealthCheckResult
{
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TimeSpan Duration { get; set; }
    public string? Exception { get; set; }
    public Dictionary<string, string>? Data { get; set; }
}

public class HsmHealthStatus
{
    public bool Available { get; set; }
    public string Version { get; set; } = string.Empty;
    public int ActiveSlots { get; set; }
    public int TotalSlots { get; set; }
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public DateTime LastHealthCheck { get; set; }
    public List<string> Alarms { get; set; } = new();
}

public class ServiceMetrics
{
    public DateTime Timestamp { get; set; }
    public SecretMetrics SecretMetrics { get; set; } = new();
    public KeyMetrics KeyMetrics { get; set; } = new();
    public AuditMetrics AuditMetrics { get; set; } = new();
    public BackupMetrics BackupMetrics { get; set; } = new();
    public List<OperationMetrics> OperationMetrics { get; set; } = new();
}

public class SecretMetrics
{
    public int TotalSecrets { get; set; }
    public int ActiveSecrets { get; set; }
    public int ExpiredSecrets { get; set; }
    public int ExpiringSecrets { get; set; }
    public Dictionary<string, int> SecretsByType { get; set; } = new();
}

public class KeyMetrics
{
    public int TotalKeys { get; set; }
    public int ActiveKeys { get; set; }
    public Dictionary<string, int> KeysByAlgorithm { get; set; } = new();
}

public class AuditMetrics
{
    public int LogsToday { get; set; }
    public int LogsThisWeek { get; set; }
    public Dictionary<string, int> TopOperations { get; set; } = new();
}

public class BackupMetrics
{
    public int TotalBackups { get; set; }
    public int CompletedBackups { get; set; }
    public long TotalBackupSize { get; set; }
}

public class OperationMetrics
{
    public string Operation { get; set; } = string.Empty;
    public long TotalOperations { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public long SuccessCount { get; set; }
    public long ErrorCount { get; set; }
    public double AverageResponseTime { get; set; }
    public double SuccessRate { get; set; }
    public DateTime FirstOperation { get; set; }
    public DateTime LastOperation { get; set; }
}

public class PerformanceMetrics
{
    public DateTime Timestamp { get; set; }
    public double CpuUsage { get; set; }
    public long MemoryUsage { get; set; }
    public int ThreadCount { get; set; }
    public int HandleCount { get; set; }
    public Dictionary<int, long> GcCollections { get; set; } = new();
    public long TotalMemory { get; set; }
    public HsmPerformanceData? HsmMetrics { get; set; }
    public DatabasePerformanceData? DatabaseMetrics { get; set; }
    public Dictionary<string, OperationPerformanceData> OperationPerformance { get; set; } = new();
}

public class HsmPerformanceData
{
    public int OperationsPerSecond { get; set; }
    public double AverageResponseTime { get; set; }
    public int ActiveConnections { get; set; }
    public double CpuUtilization { get; set; }
    public double MemoryUtilization { get; set; }
    public int ErrorRate { get; set; }
}

public class DatabasePerformanceData
{
    public long ConnectionTime { get; set; }
    public bool IsConnected { get; set; }
    public int ActiveConnections { get; set; }
    public int ConnectionPoolSize { get; set; }
    public string? Error { get; set; }
}

public class OperationPerformanceData
{
    public double AverageResponseTime { get; set; }
    public long TotalOperations { get; set; }
    public double SuccessRate { get; set; }
    public long ErrorCount { get; set; }
    public DateTime LastOperation { get; set; }
}

public class SecurityMetrics
{
    public DateTime Timestamp { get; set; }
    public AuthenticationMetrics AuthenticationMetrics { get; set; } = new();
    public int AccessViolations24h { get; set; }
    public Dictionary<string, int> SecurityEvents { get; set; } = new();
    public int SuspiciousActivities { get; set; }
    public int PolicyViolations { get; set; }
    public EncryptionHealthData EncryptionHealth { get; set; } = new();
}

public class AuthenticationMetrics
{
    public int FailedLoginAttempts24h { get; set; }
    public int SuccessfulLogins24h { get; set; }
    public double LoginSuccessRate { get; set; }
}

public class EncryptionHealthData
{
    public bool HsmAvailable { get; set; }
    public int TotalKeys { get; set; }
    public int ActiveKeys { get; set; }
    public double KeyRotationHealth { get; set; }
    public int EncryptionErrors24h { get; set; }
    public string? Error { get; set; }
}

public class SecurityEvent
{
    public string EventType { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public DateTime Timestamp { get; set; }
}