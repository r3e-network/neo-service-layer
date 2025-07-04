using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.Services.Compliance.Models;

namespace NeoServiceLayer.Services.Compliance;

public partial class ComplianceService
{
    private IPersistentStorageProvider? _persistentStorage;
    private Timer? _persistenceTimer;
    private Timer? _cleanupTimer;

    // Storage key prefixes
    private const string RULE_PREFIX = "compliance:rule:";
    private const string CHECK_PREFIX = "compliance:check:";
    private const string RESULT_PREFIX = "compliance:result:";
    private const string AUDIT_PREFIX = "compliance:audit:";
    private const string VIOLATION_PREFIX = "compliance:violation:";
    private const string REPORT_PREFIX = "compliance:report:";
    private const string INDEX_PREFIX = "compliance:index:";
    private const string STATS_PREFIX = "compliance:stats:";

    /// <summary>
    /// Initializes persistent storage for the compliance service.
    /// </summary>
    private async Task InitializePersistentStorageAsync()
    {
        try
        {
            _persistentStorage = _serviceProvider?.GetService(typeof(IPersistentStorageProvider)) as IPersistentStorageProvider;

            if (_persistentStorage != null)
            {
                await _persistentStorage.InitializeAsync();
                Logger.LogInformation("Persistent storage initialized for ComplianceService");

                // Restore compliance data from storage
                await RestoreComplianceDataFromStorageAsync();

                // Start periodic persistence timer (every 30 seconds)
                _persistenceTimer = new Timer(
                    async _ => await PersistComplianceDataAsync(),
                    null,
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(30));

                // Start cleanup timer (every hour)
                _cleanupTimer = new Timer(
                    async _ => await CleanupExpiredDataAsync(),
                    null,
                    TimeSpan.FromHours(1),
                    TimeSpan.FromHours(1));
            }
            else
            {
                Logger.LogWarning("Persistent storage provider not available for ComplianceService");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize persistent storage for ComplianceService");
        }
    }

    /// <summary>
    /// Persists a compliance rule to storage.
    /// </summary>
    private async Task PersistComplianceRuleAsync(ComplianceRule rule)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{RULE_PREFIX}{rule.RuleId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(rule);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(365) // Keep rules for 1 year
            });

            // Update index
            await UpdateRuleIndexAsync(rule.RuleType, rule.RuleId);

            Logger.LogDebug("Persisted compliance rule {RuleId} to storage", rule.RuleId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist compliance rule {RuleId}", rule.RuleId);
        }
    }

    /// <summary>
    /// Persists a compliance check result to storage.
    /// </summary>
    private async Task PersistComplianceCheckAsync(string checkId, ComplianceCheckResult result)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{RESULT_PREFIX}{checkId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(result);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(90) // Keep results for 90 days
            });

            // Store audit log entry
            await PersistAuditLogAsync(checkId, result);

            Logger.LogDebug("Persisted compliance check result {CheckId} to storage", checkId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist compliance check result {CheckId}", checkId);
        }
    }

    /// <summary>
    /// Persists a compliance violation to storage.
    /// </summary>
    private async Task PersistViolationAsync(ComplianceViolation violation)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{VIOLATION_PREFIX}{violation.ViolationId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(violation);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(730) // Keep violations for 2 years
            });

            // Update violation index
            await UpdateViolationIndexAsync(violation.Address ?? "unknown", violation.ViolationId);

            Logger.LogDebug("Persisted compliance violation {ViolationId} to storage", violation.ViolationId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist compliance violation {ViolationId}", violation.ViolationId);
        }
    }

    /// <summary>
    /// Persists an audit log entry.
    /// </summary>
    private async Task PersistAuditLogAsync(string checkId, ComplianceCheckResult result)
    {
        if (_persistentStorage == null) return;

        try
        {
            var auditEntry = new ComplianceAuditEntry
            {
                AuditId = Guid.NewGuid().ToString(),
                CheckId = checkId,
                Timestamp = result.CheckedAt,
                Result = result,
                UserId = "system",
                Action = "ComplianceCheck",
                Details = JsonSerializer.Serialize(result)
            };

            var key = $"{AUDIT_PREFIX}{auditEntry.AuditId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(auditEntry);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(2555) // Keep audit logs for 7 years
            });

            Logger.LogDebug("Persisted audit log entry {AuditId} to storage", auditEntry.AuditId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist audit log entry for check {CheckId}", checkId);
        }
    }

    /// <summary>
    /// Persists a compliance report to storage.
    /// </summary>
    private async Task PersistComplianceReportAsync(ComplianceReport report)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{REPORT_PREFIX}{report.ReportId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(report);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(2555) // Keep reports for 7 years
            });

            Logger.LogDebug("Persisted compliance report {ReportId} to storage", report.ReportId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist compliance report {ReportId}", report.ReportId);
        }
    }

    /// <summary>
    /// Updates rule type index in storage.
    /// </summary>
    private async Task UpdateRuleIndexAsync(string ruleType, string ruleId)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{INDEX_PREFIX}rule_type:{ruleType}";
            var existingData = await _persistentStorage.RetrieveAsync(key);

            var ruleIds = existingData != null
                ? JsonSerializer.Deserialize<HashSet<string>>(existingData) ?? new HashSet<string>()
                : new HashSet<string>();

            ruleIds.Add(ruleId);

            var data = JsonSerializer.SerializeToUtf8Bytes(ruleIds);
            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update rule type index for {RuleType}", ruleType);
        }
    }

    /// <summary>
    /// Updates violation index in storage.
    /// </summary>
    private async Task UpdateViolationIndexAsync(string entityId, string violationId)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{INDEX_PREFIX}entity:{entityId}";
            var existingData = await _persistentStorage.RetrieveAsync(key);

            var violationIds = existingData != null
                ? JsonSerializer.Deserialize<HashSet<string>>(existingData) ?? new HashSet<string>()
                : new HashSet<string>();

            violationIds.Add(violationId);

            var data = JsonSerializer.SerializeToUtf8Bytes(violationIds);
            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update violation index for entity {EntityId}", entityId);
        }
    }

    /// <summary>
    /// Restores compliance data from persistent storage.
    /// </summary>
    private async Task RestoreComplianceDataFromStorageAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            Logger.LogInformation("Restoring compliance data from persistent storage");

            // Restore rules
            await RestoreRulesFromStorageAsync();

            // Restore recent check results
            await RestoreRecentCheckResultsAsync();

            // Restore active violations
            await RestoreActiveViolationsAsync();

            // Restore statistics
            await RestoreStatisticsAsync();

            Logger.LogInformation("Compliance data restored from storage");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore compliance data from storage");
        }
    }

    /// <summary>
    /// Restores compliance rules from storage.
    /// </summary>
    private async Task RestoreRulesFromStorageAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            var ruleKeys = await _persistentStorage.ListKeysAsync($"{RULE_PREFIX}*");
            var restoredCount = 0;

            foreach (var key in ruleKeys)
            {
                try
                {
                    var data = await _persistentStorage.RetrieveAsync(key);

                    if (data != null)
                    {
                        var rule = JsonSerializer.Deserialize<ComplianceRule>(data);
                        if (rule != null)
                        {
                            _complianceRules[rule.RuleId] = rule;
                            restoredCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to restore rule from key {Key}", key);
                }
            }

            Logger.LogInformation("Restored {Count} compliance rules from storage", restoredCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore compliance rules from storage");
        }
    }

    /// <summary>
    /// Restores recent check results from storage.
    /// </summary>
    private async Task RestoreRecentCheckResultsAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            var resultKeys = await _persistentStorage.ListKeysAsync($"{RESULT_PREFIX}*");
            var cutoffDate = DateTime.UtcNow.AddDays(-7); // Only restore results from last 7 days
            var restoredCount = 0;

            foreach (var key in resultKeys.Take(100)) // Limit to recent 100 results
            {
                try
                {
                    var data = await _persistentStorage.RetrieveAsync(key);

                    if (data != null)
                    {
                        var result = JsonSerializer.Deserialize<ComplianceCheckResult>(data);
                        if (result != null && result.CheckedAt >= cutoffDate)
                        {
                            var checkId = key.Replace(RESULT_PREFIX, "");
                            _recentCheckResults[checkId] = result;
                            restoredCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to restore check result from key {Key}", key);
                }
            }

            Logger.LogInformation("Restored {Count} recent compliance check results", restoredCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore check results from storage");
        }
    }

    /// <summary>
    /// Restores active violations from storage.
    /// </summary>
    private async Task RestoreActiveViolationsAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            var violationKeys = await _persistentStorage.ListKeysAsync($"{VIOLATION_PREFIX}*");
            var restoredCount = 0;

            foreach (var key in violationKeys)
            {
                try
                {
                    var data = await _persistentStorage.RetrieveAsync(key);

                    if (data != null)
                    {
                        var violation = JsonSerializer.Deserialize<ComplianceViolation>(data);
                        if (violation != null && violation.Status == ViolationStatus.Open)
                        {
                            _activeViolations[violation.ViolationId] = violation;
                            restoredCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to restore violation from key {Key}", key);
                }
            }

            Logger.LogInformation("Restored {Count} active violations from storage", restoredCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore violations from storage");
        }
    }

    /// <summary>
    /// Restores service statistics from storage.
    /// </summary>
    private async Task RestoreStatisticsAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{STATS_PREFIX}current";
            var data = await _persistentStorage.RetrieveAsync(key);

            if (data != null)
            {
                var stats = JsonSerializer.Deserialize<ComplianceServiceStatistics>(data);
                if (stats != null)
                {
                    _totalChecksPerformed = stats.TotalChecksPerformed;
                    _totalViolationsDetected = stats.TotalViolationsDetected;
                    _totalRulesEvaluated = stats.TotalRulesEvaluated;

                    Logger.LogInformation("Restored compliance statistics from storage");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore statistics from storage");
        }
    }

    /// <summary>
    /// Persists all current compliance data to storage.
    /// </summary>
    private async Task PersistComplianceDataAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            // Persist rules
            foreach (var rule in _complianceRules.Values)
            {
                await PersistComplianceRuleAsync(rule);
            }

            // Persist statistics
            await PersistServiceStatisticsAsync();

            Logger.LogDebug("Persisted compliance data to storage");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist compliance data");
        }
    }

    /// <summary>
    /// Persists service statistics to storage.
    /// </summary>
    private async Task PersistServiceStatisticsAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            var stats = new ComplianceServiceStatistics
            {
                TotalChecksPerformed = _totalChecksPerformed,
                TotalViolationsDetected = _totalViolationsDetected,
                TotalRulesEvaluated = _totalRulesEvaluated,
                ActiveRules = _complianceRules.Count,
                ActiveViolations = _activeViolations.Count,
                LastUpdated = DateTime.UtcNow
            };

            var key = $"{STATS_PREFIX}current";
            var data = JsonSerializer.SerializeToUtf8Bytes(stats);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist service statistics");
        }
    }

    /// <summary>
    /// Cleans up expired data from storage.
    /// </summary>
    private async Task CleanupExpiredDataAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            Logger.LogInformation("Starting cleanup of expired compliance data");

            // Clean up old check results (older than 90 days)
            var resultKeys = await _persistentStorage.ListKeysAsync($"{RESULT_PREFIX}*");
            var cleanedCount = 0;

            foreach (var key in resultKeys)
            {
                try
                {
                    var data = await _persistentStorage.RetrieveAsync(key);

                    if (data != null)
                    {
                        var result = JsonSerializer.Deserialize<ComplianceCheckResult>(data);
                        if (result != null && result.CheckedAt < DateTime.UtcNow.AddDays(-90))
                        {
                            await _persistentStorage.DeleteAsync(key);
                            cleanedCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to cleanup result key {Key}", key);
                }
            }

            Logger.LogInformation("Cleaned up {Count} expired compliance check results", cleanedCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to cleanup expired data");
        }
    }

    /// <summary>
    /// Removes a compliance rule from persistent storage.
    /// </summary>
    private async Task RemoveRuleFromStorageAsync(string ruleId)
    {
        if (_persistentStorage == null) return;

        try
        {
            await _persistentStorage.DeleteAsync($"{RULE_PREFIX}{ruleId}");

            Logger.LogDebug("Removed compliance rule {RuleId} from storage", ruleId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to remove rule {RuleId} from storage", ruleId);
        }
    }

    /// <summary>
    /// Disposes persistence resources.
    /// </summary>
    private void DisposePersistenceResources()
    {
        _persistenceTimer?.Dispose();
        _cleanupTimer?.Dispose();
        _persistentStorage?.Dispose();
    }
}

/// <summary>
/// Compliance audit entry for audit trail.
/// </summary>
internal class ComplianceAuditEntry
{
    public string AuditId { get; set; } = string.Empty;
    public string CheckId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public ComplianceCheckResult Result { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

/// <summary>
/// Statistics for compliance service.
/// </summary>
internal class ComplianceServiceStatistics
{
    public long TotalChecksPerformed { get; set; }
    public long TotalViolationsDetected { get; set; }
    public long TotalRulesEvaluated { get; set; }
    public int ActiveRules { get; set; }
    public int ActiveViolations { get; set; }
    public DateTime LastUpdated { get; set; }
}
