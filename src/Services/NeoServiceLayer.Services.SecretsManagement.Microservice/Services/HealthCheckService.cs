using Microsoft.Extensions.Diagnostics.HealthChecks;
using Neo.SecretsManagement.Service.Data;
using Neo.SecretsManagement.Service.Services;

namespace Neo.SecretsManagement.Service.Services;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly SecretsDbContext _context;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(SecretsDbContext context, ILogger<DatabaseHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("Cannot connect to database");
            }

            // Test a simple query
            var secretCount = await _context.Secrets.CountAsync(cancellationToken);
            
            var data = new Dictionary<string, object>
            {
                ["database_connected"] = true,
                ["secret_count"] = secretCount,
                ["connection_string"] = _context.Database.GetConnectionString()?.Substring(0, 50) + "..."
            };

            return HealthCheckResult.Healthy("Database is accessible", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy("Database health check failed", ex);
        }
    }
}

public class HsmHealthCheck : IHealthCheck
{
    private readonly IHsmService _hsmService;
    private readonly ILogger<HsmHealthCheck> _logger;

    public HsmHealthCheck(IHsmService hsmService, ILogger<HsmHealthCheck> logger)
    {
        _hsmService = hsmService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var hsmStatus = await _hsmService.GetStatusAsync();
            
            if (!hsmStatus.IsAvailable)
            {
                return HealthCheckResult.Degraded("HSM is not available", null, new Dictionary<string, object>
                {
                    ["hsm_available"] = false,
                    ["version"] = hsmStatus.Version,
                    ["alarms"] = hsmStatus.Alarms
                });
            }

            // Test basic HSM operation
            var keys = await _hsmService.ListKeysAsync();
            
            var data = new Dictionary<string, object>
            {
                ["hsm_available"] = true,
                ["version"] = hsmStatus.Version,
                ["active_slots"] = hsmStatus.ActiveSlots,
                ["total_slots"] = hsmStatus.TotalSlots,
                ["cpu_usage"] = hsmStatus.CpuUsage,
                ["memory_usage"] = hsmStatus.MemoryUsage,
                ["key_count"] = keys.Count,
                ["alarms"] = hsmStatus.Alarms
            };

            // Check for alarms
            if (hsmStatus.Alarms.Any())
            {
                return HealthCheckResult.Degraded("HSM has active alarms", null, data);
            }

            return HealthCheckResult.Healthy("HSM is operational", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HSM health check failed");
            return HealthCheckResult.Unhealthy("HSM health check failed", ex, new Dictionary<string, object>
            {
                ["hsm_available"] = false,
                ["error"] = ex.Message
            });
        }
    }
}

public class EncryptionHealthCheck : IHealthCheck
{
    private readonly IEncryptionService _encryptionService;
    private readonly IKeyManagementService _keyManagementService;
    private readonly ILogger<EncryptionHealthCheck> _logger;

    public EncryptionHealthCheck(
        IEncryptionService encryptionService,
        IKeyManagementService keyManagementService,
        ILogger<EncryptionHealthCheck> logger)
    {
        _encryptionService = encryptionService;
        _keyManagementService = keyManagementService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Test encryption/decryption round trip
            const string testData = "health_check_test_data_" + nameof(EncryptionHealthCheck);
            const string testKeyId = "health-check-key";

            var encrypted = await _encryptionService.EncryptAsync(testData, testKeyId);
            var decrypted = await _encryptionService.DecryptAsync(encrypted, testKeyId);

            if (decrypted != testData)
            {
                return HealthCheckResult.Unhealthy("Encryption round trip failed - data mismatch");
            }

            // Get key statistics
            var keyStats = await _keyManagementService.GetKeyStatisticsAsync();
            
            var data = new Dictionary<string, object>
            {
                ["encryption_working"] = true,
                ["test_passed"] = true,
                ["active_keys"] = keyStats?.ActiveKeys ?? 0,
                ["total_keys"] = keyStats?.TotalKeys ?? 0,
                ["key_algorithms"] = keyStats?.KeyAlgorithms ?? new Dictionary<string, int>()
            };

            if (keyStats?.ActiveKeys == 0)
            {
                return HealthCheckResult.Degraded("No active encryption keys available", null, data);
            }

            return HealthCheckResult.Healthy("Encryption service is working correctly", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Encryption health check failed");
            return HealthCheckResult.Unhealthy("Encryption health check failed", ex, new Dictionary<string, object>
            {
                ["encryption_working"] = false,
                ["test_passed"] = false,
                ["error"] = ex.Message
            });
        }
    }
}

public class AuditHealthCheck : IHealthCheck
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditHealthCheck> _logger;

    public AuditHealthCheck(IAuditService auditService, ILogger<AuditHealthCheck> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Test audit logging
            await _auditService.LogAsync(
                "health-check",
                "test",
                "health_check",
                "audit_test",
                null,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["test_type"] = "health_check",
                    ["timestamp"] = DateTime.UtcNow.ToString("O")
                }
            );

            // Get recent audit log count
            var recentLogs = await _auditService.GetAuditLogCountAsync(
                null, null, 
                DateTime.UtcNow.AddMinutes(-5), 
                DateTime.UtcNow
            );

            var data = new Dictionary<string, object>
            {
                ["audit_logging_working"] = true,
                ["recent_logs_5min"] = recentLogs,
                ["test_logged"] = true
            };

            return HealthCheckResult.Healthy("Audit service is working correctly", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audit health check failed");
            return HealthCheckResult.Unhealthy("Audit health check failed", ex, new Dictionary<string, object>
            {
                ["audit_logging_working"] = false,
                ["test_logged"] = false,
                ["error"] = ex.Message
            });
        }
    }
}

public class BackupHealthCheck : IHealthCheck
{
    private readonly IBackupService _backupService;
    private readonly ILogger<BackupHealthCheck> _logger;

    public BackupHealthCheck(IBackupService backupService, ILogger<BackupHealthCheck> logger)
    {
        _backupService = backupService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get backup statistics
            var backups = await _backupService.ListBackupsAsync("health-check");
            var completedBackups = backups.Where(b => b.Status == BackupStatus.Completed).ToList();
            var failedBackups = backups.Where(b => b.Status == BackupStatus.Failed).ToList();

            // Check for recent successful backup
            var recentSuccessfulBackup = completedBackups
                .Where(b => b.CompletedAt >= DateTime.UtcNow.AddDays(-7))
                .OrderByDescending(b => b.CompletedAt)
                .FirstOrDefault();

            var data = new Dictionary<string, object>
            {
                ["total_backups"] = backups.Count,
                ["completed_backups"] = completedBackups.Count,
                ["failed_backups"] = failedBackups.Count,
                ["recent_successful_backup"] = recentSuccessfulBackup?.CompletedAt?.ToString("O") ?? "none",
                ["backup_service_accessible"] = true
            };

            // Validate integrity of a recent backup if available
            if (recentSuccessfulBackup != null)
            {
                var isValid = await _backupService.ValidateBackupIntegrityAsync(recentSuccessfulBackup.Id);
                data["recent_backup_integrity"] = isValid;

                if (!isValid)
                {
                    return HealthCheckResult.Degraded("Recent backup failed integrity check", null, data);
                }
            }

            if (failedBackups.Any(b => b.CreatedAt >= DateTime.UtcNow.AddDays(-1)))
            {
                return HealthCheckResult.Degraded("Recent backup failures detected", null, data);
            }

            return HealthCheckResult.Healthy("Backup service is working correctly", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup health check failed");
            return HealthCheckResult.Unhealthy("Backup health check failed", ex, new Dictionary<string, object>
            {
                ["backup_service_accessible"] = false,
                ["error"] = ex.Message
            });
        }
    }
}

public class MemoryHealthCheck : IHealthCheck
{
    private readonly ILogger<MemoryHealthCheck> _logger;

    public MemoryHealthCheck(ILogger<MemoryHealthCheck> logger)
    {
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var totalMemory = GC.GetTotalMemory(false);
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var workingSet = process.WorkingSet64;
            
            // Memory thresholds (configurable in production)
            const long warningThreshold = 500 * 1024 * 1024; // 500MB
            const long criticalThreshold = 1024 * 1024 * 1024; // 1GB

            var data = new Dictionary<string, object>
            {
                ["total_memory_bytes"] = totalMemory,
                ["working_set_bytes"] = workingSet,
                ["gc_gen0_collections"] = GC.CollectionCount(0),
                ["gc_gen1_collections"] = GC.CollectionCount(1),
                ["gc_gen2_collections"] = GC.CollectionCount(2),
                ["thread_count"] = process.Threads.Count,
                ["handle_count"] = process.HandleCount
            };

            if (workingSet > criticalThreshold)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Memory usage is critical: {workingSet / 1024 / 1024}MB", 
                    null, data));
            }

            if (workingSet > warningThreshold)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Memory usage is high: {workingSet / 1024 / 1024}MB", 
                    null, data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"Memory usage is normal: {workingSet / 1024 / 1024}MB", data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Memory health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy("Memory health check failed", ex));
        }
    }
}

public class DiskSpaceHealthCheck : IHealthCheck
{
    private readonly ILogger<DiskSpaceHealthCheck> _logger;

    public DiskSpaceHealthCheck(ILogger<DiskSpaceHealthCheck> logger)
    {
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var driveInfo = new DriveInfo(Path.GetPathRoot(currentDirectory) ?? "/");

            if (!driveInfo.IsReady)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Drive is not ready"));
            }

            var freeSpaceGB = driveInfo.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0;
            var totalSpaceGB = driveInfo.TotalSize / 1024.0 / 1024.0 / 1024.0;
            var usedSpaceGB = totalSpaceGB - freeSpaceGB;
            var usedPercentage = (usedSpaceGB / totalSpaceGB) * 100;

            var data = new Dictionary<string, object>
            {
                ["drive_name"] = driveInfo.Name,
                ["total_space_gb"] = Math.Round(totalSpaceGB, 2),
                ["free_space_gb"] = Math.Round(freeSpaceGB, 2),
                ["used_space_gb"] = Math.Round(usedSpaceGB, 2),
                ["used_percentage"] = Math.Round(usedPercentage, 2)
            };

            // Disk space thresholds
            if (usedPercentage > 90)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Disk space is critical: {usedPercentage:F1}% used", 
                    null, data));
            }

            if (usedPercentage > 80)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Disk space is low: {usedPercentage:F1}% used", 
                    null, data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"Disk space is sufficient: {usedPercentage:F1}% used", data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Disk space health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy("Disk space health check failed", ex));
        }
    }
}

// Extension method to register all health checks
public static class HealthCheckExtensions
{
    public static IServiceCollection AddSecretsManagementHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready", "database" })
            .AddCheck<HsmHealthCheck>("hsm", tags: new[] { "ready", "hsm" })
            .AddCheck<EncryptionHealthCheck>("encryption", tags: new[] { "ready", "encryption" })
            .AddCheck<AuditHealthCheck>("audit", tags: new[] { "ready", "audit" })
            .AddCheck<BackupHealthCheck>("backup", tags: new[] { "ready", "backup" })
            .AddCheck<MemoryHealthCheck>("memory", tags: new[] { "live", "memory" })
            .AddCheck<DiskSpaceHealthCheck>("diskspace", tags: new[] { "live", "storage" });

        return services;
    }
}