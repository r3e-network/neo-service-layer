using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.Services.SocialRecovery.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.SocialRecovery;

/// <summary>
/// Persistent storage extensions for SocialRecoveryService.
/// </summary>
public partial class SocialRecoveryService
{
    private const string GUARDIAN_PREFIX = "social-recovery:guardian:";
    private const string RECOVERY_REQUEST_PREFIX = "social-recovery:recovery:";
    private const string TRUST_RELATION_PREFIX = "social-recovery:trust:";
    private const string AUTH_FACTOR_PREFIX = "social-recovery:auth:";
    private const string ACCOUNT_CONFIG_PREFIX = "social-recovery:config:";
    private const string AUDIT_LOG_PREFIX = "social-recovery:audit:";
    private const string METRICS_KEY = "social-recovery:metrics";

    /// <summary>
    /// Loads persistent data from storage on service initialization.
    /// </summary>
    private async Task LoadPersistentDataAsync()
    {
        if (_persistentStorage == null)
        {
            Logger.LogWarning("Persistent storage not available, using in-memory storage only");
            return;
        }

        try
        {
            Logger.LogInformation("Loading persistent social recovery data...");

            // Load guardians
            var guardianKeys = await _persistentStorage.ListKeysAsync(GUARDIAN_PREFIX);
            foreach (var key in guardianKeys)
            {
                var data = await _persistentStorage.RetrieveAsync(key);
                if (data != null)
                {
                    var guardian = JsonSerializer.Deserialize<GuardianInfo>(data);
                    if (guardian != null && !string.IsNullOrEmpty(guardian.Address))
                    {
                        _guardians[guardian.Address] = guardian;
                    }
                }
            }
            Logger.LogInformation("Loaded {Count} guardians from persistent storage", _guardians.Count);

            // Load recovery requests
            var recoveryKeys = await _persistentStorage.ListKeysAsync(RECOVERY_REQUEST_PREFIX);
            foreach (var key in recoveryKeys)
            {
                var data = await _persistentStorage.RetrieveAsync(key);
                if (data != null)
                {
                    var request = JsonSerializer.Deserialize<RecoveryRequest>(data);
                    if (request != null && !string.IsNullOrEmpty(request.RecoveryId))
                    {
                        _recoveryRequests[request.RecoveryId] = request;
                    }
                }
            }
            Logger.LogInformation("Loaded {Count} recovery requests from persistent storage", _recoveryRequests.Count);

            // Load trust relations
            var trustKeys = await _persistentStorage.ListKeysAsync(TRUST_RELATION_PREFIX);
            foreach (var key in trustKeys)
            {
                var data = await _persistentStorage.RetrieveAsync(key);
                if (data != null)
                {
                    var relations = JsonSerializer.Deserialize<List<TrustRelation>>(data);
                    if (relations != null && relations.Any())
                    {
                        var address = relations.First().Truster;
                        _trustRelations[address] = relations;
                    }
                }
            }
            Logger.LogInformation("Loaded trust relations for {Count} addresses from persistent storage", _trustRelations.Count);

            // Load authentication factors
            var authKeys = await _persistentStorage.ListKeysAsync(AUTH_FACTOR_PREFIX);
            foreach (var key in authKeys)
            {
                var data = await _persistentStorage.RetrieveAsync(key);
                if (data != null)
                {
                    var factors = JsonSerializer.Deserialize<List<AuthFactor>>(data);
                    if (factors != null && factors.Any())
                    {
                        // Extract account address from key
                        var accountAddress = key.Substring(AUTH_FACTOR_PREFIX.Length);
                        _authFactors[accountAddress] = factors;
                    }
                }
            }
            Logger.LogInformation("Loaded auth factors for {Count} accounts from persistent storage", _authFactors.Count);

            // Load account configurations
            var configKeys = await _persistentStorage.ListKeysAsync(ACCOUNT_CONFIG_PREFIX);
            foreach (var key in configKeys)
            {
                var data = await _persistentStorage.RetrieveAsync(key);
                if (data != null)
                {
                    var config = JsonSerializer.Deserialize<AccountRecoveryConfig>(data);
                    if (config != null && !string.IsNullOrEmpty(config.AccountAddress))
                    {
                        _accountConfigs[config.AccountAddress] = config;
                    }
                }
            }
            Logger.LogInformation("Loaded {Count} account configurations from persistent storage", _accountConfigs.Count);

            // Load metrics
            var metricsData = await _persistentStorage.RetrieveAsync(METRICS_KEY);
            if (metricsData != null)
            {
                var metrics = JsonSerializer.Deserialize<SocialRecoveryMetrics>(metricsData);
                if (metrics != null)
                {
                    lock (_metricsLock)
                    {
                        _totalRecoveries = metrics.TotalRecoveries;
                        _successfulRecoveries = metrics.SuccessfulRecoveries;
                        _failedRecoveries = metrics.FailedRecoveries;
                        _totalGuardians = metrics.TotalGuardians;
                        _lastRecoveryTime = metrics.LastRecoveryTime;
                    }
                }
                Logger.LogInformation("Loaded metrics: {Total} recoveries, {Success} successful, {Failed} failed",
                    _totalRecoveries, _successfulRecoveries, _failedRecoveries);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading persistent social recovery data");
            // Continue with in-memory data only
        }
    }

    /// <summary>
    /// Persists a guardian to storage.
    /// </summary>
    private async Task PersistGuardianAsync(GuardianInfo guardian)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{GUARDIAN_PREFIX}{guardian.Address}";
            var data = JsonSerializer.SerializeToUtf8Bytes(guardian);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                Metadata = new Dictionary<string, object>
                {
                    ["Type"] = "Guardian",
                    ["Address"] = guardian.Address,
                    ["ReputationScore"] = guardian.ReputationScore.ToString(),
                    ["IsActive"] = guardian.IsActive.ToString(),
                    ["StakedAmount"] = guardian.StakedAmount.ToString(),
                    ["EnrolledAt"] = DateTime.UtcNow.ToString("O")
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting guardian {Address}", guardian.Address);
        }
    }

    /// <summary>
    /// Persists a recovery request to storage.
    /// </summary>
    private async Task PersistRecoveryRequestAsync(RecoveryRequest request)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{RECOVERY_REQUEST_PREFIX}{request.RecoveryId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(request);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                TimeToLive = request.ExpiresAt.AddDays(30) - DateTime.UtcNow, // Keep for 30 days after expiry
                Metadata = new Dictionary<string, object>
                {
                    ["Type"] = "RecoveryRequest",
                    ["AccountAddress"] = request.AccountAddress,
                    ["NewOwner"] = request.NewOwner,
                    ["Status"] = request.Status.ToString(),
                    ["IsEmergency"] = request.IsEmergency.ToString(),
                    ["StrategyId"] = request.StrategyId,
                    ["InitiatedAt"] = request.InitiatedAt.ToString("O"),
                    ["ExpiresAt"] = request.ExpiresAt.ToString("O")
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting recovery request {RecoveryId}", request.RecoveryId);
        }
    }

    /// <summary>
    /// Persists trust relations for an address.
    /// </summary>
    private async Task PersistTrustRelationsAsync(string address, List<TrustRelation> relations)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{TRUST_RELATION_PREFIX}{address}";
            var data = JsonSerializer.SerializeToUtf8Bytes(relations);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                Metadata = new Dictionary<string, object>
                {
                    ["Type"] = "TrustRelations",
                    ["Address"] = address,
                    ["RelationCount"] = relations.Count.ToString(),
                    ["UpdatedAt"] = DateTime.UtcNow.ToString("O")
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting trust relations for {Address}", address);
        }
    }

    /// <summary>
    /// Persists authentication factors for an account.
    /// </summary>
    private async Task PersistAuthFactorsAsync(string accountAddress, List<AuthFactor> factors)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{AUTH_FACTOR_PREFIX}{accountAddress}";
            var data = JsonSerializer.SerializeToUtf8Bytes(factors);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                Metadata = new Dictionary<string, object>
                {
                    ["Type"] = "AuthFactors",
                    ["AccountAddress"] = accountAddress,
                    ["FactorCount"] = factors.Count.ToString(),
                    ["UpdatedAt"] = DateTime.UtcNow.ToString("O")
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting auth factors for {Account}", accountAddress);
        }
    }

    /// <summary>
    /// Persists account recovery configuration.
    /// </summary>
    private async Task PersistAccountConfigAsync(AccountRecoveryConfig config)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{ACCOUNT_CONFIG_PREFIX}{config.AccountAddress}";
            var data = JsonSerializer.SerializeToUtf8Bytes(config);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                Metadata = new Dictionary<string, object>
                {
                    ["Type"] = "AccountConfig",
                    ["AccountAddress"] = config.AccountAddress,
                    ["PreferredStrategy"] = config.PreferredStrategy,
                    ["RecoveryThreshold"] = config.RecoveryThreshold.ToString(),
                    ["EmergencyEnabled"] = config.EmergencyRecoveryEnabled.ToString(),
                    ["TrustedGuardianCount"] = config.TrustedGuardians.Count.ToString(),
                    ["CreatedAt"] = config.CreatedAt.ToString("O"),
                    ["ModifiedAt"] = config.ModifiedAt.ToString("O")
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting account config for {Account}", config.AccountAddress);
        }
    }

    /// <summary>
    /// Records an audit event.
    /// </summary>
    private async Task RecordAuditEventAsync(string eventType, Dictionary<string, object> eventData)
    {
        if (_persistentStorage == null) return;

        try
        {
            var auditEvent = new
            {
                EventType = eventType,
                Timestamp = DateTime.UtcNow,
                Data = eventData,
                ServiceVersion = Version
            };

            var eventId = $"{DateTime.UtcNow.Ticks}_{Guid.NewGuid():N}";
            var key = $"{AUDIT_LOG_PREFIX}{eventId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(auditEvent);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false, // Audit logs don't need encryption
                Compress = true,
                TimeToLive = TimeSpan.FromDays(365), // Keep audit logs for 1 year
                Metadata = new Dictionary<string, object>
                {
                    ["Type"] = "AuditEvent",
                    ["EventType"] = eventType,
                    ["Timestamp"] = DateTime.UtcNow.ToString("O")
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error recording audit event {EventType}", eventType);
        }
    }

    /// <summary>
    /// Persists service metrics.
    /// </summary>
    private async Task PersistMetricsAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            SocialRecoveryMetrics metrics;
            lock (_metricsLock)
            {
                metrics = new SocialRecoveryMetrics
                {
                    TotalRecoveries = _totalRecoveries,
                    SuccessfulRecoveries = _successfulRecoveries,
                    FailedRecoveries = _failedRecoveries,
                    TotalGuardians = _totalGuardians,
                    LastRecoveryTime = _lastRecoveryTime,
                    UpdatedAt = DateTime.UtcNow
                };
            }

            var data = JsonSerializer.SerializeToUtf8Bytes(metrics);

            await _persistentStorage.StoreAsync(METRICS_KEY, data, new StorageOptions
            {
                Encrypt = false,
                Compress = false,
                Metadata = new Dictionary<string, object>
                {
                    ["Type"] = "Metrics",
                    ["UpdatedAt"] = DateTime.UtcNow.ToString("O")
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting social recovery metrics");
        }
    }

    /// <summary>
    /// Loads account configuration from storage.
    /// </summary>
    private async Task<AccountRecoveryConfig?> LoadAccountConfigFromStorageAsync(string accountAddress)
    {
        if (_persistentStorage == null) return null;

        try
        {
            var key = $"{ACCOUNT_CONFIG_PREFIX}{accountAddress}";
            var data = await _persistentStorage.RetrieveAsync(key);

            if (data != null)
            {
                return JsonSerializer.Deserialize<AccountRecoveryConfig>(data);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading account config for {Account}", accountAddress);
        }

        return null;
    }

    /// <summary>
    /// Performs periodic cleanup of old data.
    /// </summary>
    private async Task CleanupOldDataAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            // Clean up expired recovery requests
            var recoveryKeys = await _persistentStorage.ListKeysAsync(RECOVERY_REQUEST_PREFIX);
            var cutoffDate = DateTime.UtcNow.AddDays(-30); // Remove requests older than 30 days

            foreach (var key in recoveryKeys)
            {
                try
                {
                    var metadata = await _persistentStorage.GetMetadataAsync(key);
                    if (metadata?.CreatedAt < cutoffDate)
                    {
                        await _persistentStorage.DeleteAsync(key);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Error cleaning up recovery request {Key}", key);
                }
            }

            // Clean up old audit logs (older than 1 year)
            var auditKeys = await _persistentStorage.ListKeysAsync(AUDIT_LOG_PREFIX);
            var auditCutoff = DateTime.UtcNow.AddDays(-365);

            foreach (var key in auditKeys)
            {
                try
                {
                    var metadata = await _persistentStorage.GetMetadataAsync(key);
                    if (metadata?.CreatedAt < auditCutoff)
                    {
                        await _persistentStorage.DeleteAsync(key);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Error cleaning up audit log {Key}", key);
                }
            }

            Logger.LogInformation("Completed cleanup of old social recovery data");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during social recovery data cleanup");
        }
    }
}

/// <summary>
/// Social recovery metrics for persistence.
/// </summary>
internal class SocialRecoveryMetrics
{
    public int TotalRecoveries { get; set; }
    public int SuccessfulRecoveries { get; set; }
    public int FailedRecoveries { get; set; }
    public int TotalGuardians { get; set; }
    public DateTime LastRecoveryTime { get; set; }
    public DateTime UpdatedAt { get; set; }
}
