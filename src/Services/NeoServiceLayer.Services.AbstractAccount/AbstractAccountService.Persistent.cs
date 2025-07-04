using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.AbstractAccount.Models;
using NeoServiceLayer.Tee.Host.Services;

namespace NeoServiceLayer.Services.AbstractAccount;

/// <summary>
/// Persistent storage implementation for AbstractAccountService.
/// </summary>
public partial class AbstractAccountService
{
    private readonly IPersistentStorageProvider? _persistentStorage;
    private Timer? _cleanupTimer;

    // Storage key prefixes
    private const string ACCOUNT_PREFIX = "account:";
    private const string TRANSACTION_HISTORY_PREFIX = "account:tx:";
    private const string GUARDIAN_PREFIX = "account:guardian:";
    private const string SESSION_KEY_PREFIX = "account:session:";
    private const string RECOVERY_PREFIX = "account:recovery:";
    private const string ACCOUNT_INDEX_PREFIX = "account:index:";
    private const string STATS_PREFIX = "account:stats:";

    /// <summary>
    /// Initializes a new instance of the <see cref="AbstractAccountService"/> class with persistent storage.
    /// </summary>
    public AbstractAccountService(
        ILogger<AbstractAccountService> logger,
        IEnclaveManager enclaveManager,
        IPersistentStorageProvider? persistentStorage)
        : this(logger, enclaveManager)
    {
        _persistentStorage = persistentStorage;

        if (_persistentStorage != null)
        {
            // Initialize cleanup timer
            _cleanupTimer = new Timer(
                async _ => await CleanupExpiredDataAsync(),
                null,
                TimeSpan.FromHours(24),
                TimeSpan.FromHours(24));
        }
    }

    /// <summary>
    /// Loads persistent accounts on service initialization.
    /// </summary>
    private async Task LoadPersistentAccountsAsync()
    {
        if (_persistentStorage == null)
        {
            Logger.LogDebug("No persistent storage configured for AbstractAccountService");
            return;
        }

        try
        {
            Logger.LogInformation("Loading persistent abstract accounts");

            // Load all accounts
            var pattern = $"{ACCOUNT_PREFIX}*";
            var keys = await _persistentStorage.ListKeysAsync(pattern);

            int loadedCount = 0;
            foreach (var key in keys)
            {
                // Skip non-account keys (like transaction history)
                if (key.Contains(":tx:") || key.Contains(":guardian:") || key.Contains(":session:"))
                    continue;

                try
                {
                    var data = await _persistentStorage.RetrieveAsync(key);
                    if (data != null)
                    {
                        var account = JsonSerializer.Deserialize<AbstractAccountInfo>(data);
                        if (account != null)
                        {
                            lock (_accountsLock)
                            {
                                _accounts[account.AccountId] = account;
                            }

                            // Load transaction history for this account
                            await LoadAccountTransactionHistoryAsync(account.AccountId);

                            loadedCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to load account from {Key}", key);
                }
            }

            Logger.LogInformation("Loaded {Count} abstract accounts from persistent storage", loadedCount);

            // Load service statistics
            await LoadStatisticsAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading persistent accounts");
        }
    }

    /// <summary>
    /// Loads transaction history for a specific account.
    /// </summary>
    private async Task LoadAccountTransactionHistoryAsync(string accountId)
    {
        if (_persistentStorage == null) return;

        try
        {
            var pattern = $"{TRANSACTION_HISTORY_PREFIX}{accountId}:*";
            var keys = await _persistentStorage.ListKeysAsync(pattern);

            var transactions = new List<TransactionHistoryItem>();

            foreach (var key in keys)
            {
                try
                {
                    var data = await _persistentStorage.RetrieveAsync(key);
                    if (data != null)
                    {
                        var transaction = JsonSerializer.Deserialize<TransactionHistoryItem>(data);
                        if (transaction != null)
                        {
                            transactions.Add(transaction);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to load transaction from {Key}", key);
                }
            }

            lock (_accountsLock)
            {
                _transactionHistory[accountId] = transactions.OrderByDescending(t => t.ExecutedAt).ToList();
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load transaction history for account {AccountId}", accountId);
        }
    }

    /// <summary>
    /// Persists an abstract account.
    /// </summary>
    private async Task PersistAccountAsync(AbstractAccountInfo account)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{ACCOUNT_PREFIX}{account.AccountId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(account);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                TimeToLive = null // Accounts should not expire
            });

            // Update indexes
            await UpdateAccountIndexesAsync(account);

            Logger.LogDebug("Persisted abstract account {AccountId}", account.AccountId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist account {AccountId}", account.AccountId);
        }
    }

    /// <summary>
    /// Persists a transaction history item.
    /// </summary>
    private async Task PersistTransactionAsync(string accountId, TransactionHistoryItem transaction)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{TRANSACTION_HISTORY_PREFIX}{accountId}:{transaction.ExecutedAt:yyyyMMddHHmmss}:{transaction.TransactionHash}";
            var data = JsonSerializer.SerializeToUtf8Bytes(transaction);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(365) // Keep transaction history for 1 year
            });
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to persist transaction {TransactionHash} for account {AccountId}",
                transaction.TransactionHash, accountId);
        }
    }

    /// <summary>
    /// Updates account indexes for querying.
    /// </summary>
    private async Task UpdateAccountIndexesAsync(AbstractAccountInfo account)
    {
        if (_persistentStorage == null) return;

        try
        {
            // Update status index
            var statusKey = $"{ACCOUNT_INDEX_PREFIX}status:{account.Status}";
            await AddToIndexAsync(statusKey, account.AccountId);

            // Update address index
            var addressKey = $"{ACCOUNT_INDEX_PREFIX}address:{account.AccountAddress}";
            var addressData = JsonSerializer.SerializeToUtf8Bytes(account.AccountId);
            await _persistentStorage.StoreAsync(addressKey, addressData, new StorageOptions
            {
                Encrypt = false,
                Compress = false
            });

            // Update guardian count index (for analytics)
            var guardianCountKey = $"{ACCOUNT_INDEX_PREFIX}guardians:{account.Guardians.Count}";
            await AddToIndexAsync(guardianCountKey, account.AccountId);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to update indexes for account {AccountId}", account.AccountId);
        }
    }

    /// <summary>
    /// Adds an account ID to an index.
    /// </summary>
    private async Task AddToIndexAsync(string indexKey, string accountId)
    {
        if (_persistentStorage == null) return;

        var existingData = await _persistentStorage.RetrieveAsync(indexKey);

        HashSet<string> accountIds;
        if (existingData != null)
        {
            accountIds = JsonSerializer.Deserialize<HashSet<string>>(existingData) ?? new HashSet<string>();
        }
        else
        {
            accountIds = new HashSet<string>();
        }

        accountIds.Add(accountId);

        var data = JsonSerializer.SerializeToUtf8Bytes(accountIds);
        await _persistentStorage.StoreAsync(indexKey, data, new StorageOptions
        {
            Encrypt = false,
            Compress = true
        });
    }

    /// <summary>
    /// Persists recovery information.
    /// </summary>
    private async Task PersistRecoveryInfoAsync(string recoveryId, RecoveryInfo recovery)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{RECOVERY_PREFIX}{recoveryId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(recovery);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = false,
                TimeToLive = TimeSpan.FromDays(30) // Recovery info expires after 30 days
            });
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to persist recovery info {RecoveryId}", recoveryId);
        }
    }

    /// <summary>
    /// Loads service statistics from persistent storage.
    /// </summary>
    private async Task LoadStatisticsAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{STATS_PREFIX}current";
            var data = await _persistentStorage.RetrieveAsync(key);

            if (data != null)
            {
                var stats = JsonSerializer.Deserialize<ServiceStatistics>(data);
                if (stats != null)
                {
                    Logger.LogInformation("Loaded service statistics: TotalAccounts={TotalAccounts}, TotalTransactions={TotalTransactions}",
                        stats.TotalAccounts, stats.TotalTransactions);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load service statistics");
        }
    }

    /// <summary>
    /// Persists service statistics.
    /// </summary>
    private async Task PersistStatisticsAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            int totalAccounts;
            int totalTransactions;
            int activeAccounts;

            lock (_accountsLock)
            {
                totalAccounts = _accounts.Count;
                activeAccounts = _accounts.Values.Count(a => a.Status == AccountStatus.Active);
                totalTransactions = _transactionHistory.Values.Sum(h => h.Count);
            }

            var stats = new ServiceStatistics
            {
                TotalAccounts = totalAccounts,
                ActiveAccounts = activeAccounts,
                TotalTransactions = totalTransactions,
                Timestamp = DateTime.UtcNow
            };

            var key = $"{STATS_PREFIX}current";
            var data = JsonSerializer.SerializeToUtf8Bytes(stats);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false,
                Compress = false
            });

            // Also store historical stats
            var historyKey = $"{STATS_PREFIX}history:{stats.Timestamp:yyyyMMddHHmmss}";
            await _persistentStorage.StoreAsync(historyKey, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(90) // Keep history for 90 days
            });
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to persist service statistics");
        }
    }

    /// <summary>
    /// Cleans up expired session keys and other time-sensitive data.
    /// </summary>
    private async Task CleanupExpiredDataAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            Logger.LogDebug("Starting cleanup of expired data");

            List<string> accountsToUpdate = new();

            lock (_accountsLock)
            {
                foreach (var account in _accounts.Values)
                {
                    // Clean up expired session keys
                    var expiredKeys = account.SessionKeys
                        .Where(sk => sk.ExpiresAt < DateTime.UtcNow)
                        .ToList();

                    if (expiredKeys.Any())
                    {
                        var activeKeys = account.SessionKeys
                            .Where(sk => !expiredKeys.Contains(sk))
                            .ToList();

                        account.SessionKeys = activeKeys;
                        accountsToUpdate.Add(account.AccountId);

                        Logger.LogInformation("Cleaned up {Count} expired session keys for account {AccountId}",
                            expiredKeys.Count, account.AccountId);
                    }
                }
            }

            // Persist updated accounts
            foreach (var accountId in accountsToUpdate)
            {
                AbstractAccountInfo? account = null;
                lock (_accountsLock)
                {
                    _accounts.TryGetValue(accountId, out account);
                }

                if (account != null)
                {
                    await PersistAccountAsync(account);
                }
            }

            // Clean up old transaction history (older than 1 year)
            var cutoffDate = DateTime.UtcNow.AddYears(-1);
            var txPattern = $"{TRANSACTION_HISTORY_PREFIX}*";
            var txKeys = await _persistentStorage.ListKeysAsync(txPattern);

            int deletedCount = 0;
            foreach (var key in txKeys)
            {
                // Extract timestamp from key
                var parts = key.Split(':');
                if (parts.Length >= 3)
                {
                    if (DateTime.TryParseExact(parts[2], "yyyyMMddHHmmss", null,
                        System.Globalization.DateTimeStyles.None, out var timestamp))
                    {
                        if (timestamp < cutoffDate)
                        {
                            await _persistentStorage.DeleteAsync(key);
                            deletedCount++;
                        }
                    }
                }
            }

            if (deletedCount > 0)
            {
                Logger.LogInformation("Cleaned up {Count} old transaction history entries", deletedCount);
            }

            // Persist updated statistics
            await PersistStatisticsAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during cleanup of expired data");
        }
    }

    /// <summary>
    /// Disposes persistence resources.
    /// </summary>
    private void DisposePersistenceResources()
    {
        _cleanupTimer?.Dispose();
        _persistentStorage?.Dispose();
    }

}

/// <summary>
/// Service statistics model.
/// </summary>
internal class ServiceStatistics
{
    public int TotalAccounts { get; set; }
    public int ActiveAccounts { get; set; }
    public int TotalTransactions { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Recovery information model.
/// </summary>
internal class RecoveryInfo
{
    public string RecoveryId { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string InitiatedBy { get; set; } = string.Empty;
    public RecoveryStatus Status { get; set; }
    public int RequiredApprovals { get; set; }
    public int CurrentApprovals { get; set; }
    public DateTime InitiatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
