using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.ProofOfReserve.Models;

namespace NeoServiceLayer.Services.ProofOfReserve;

/// <summary>
/// Implementation of the Proof of Reserve Service that provides asset backing verification capabilities.
/// </summary>
public partial class ProofOfReserveService : EnclaveBlockchainServiceBase, IProofOfReserveService
{
    private readonly Dictionary<string, MonitoredAsset> _monitoredAssets = new();
    private readonly Dictionary<string, List<ReserveSnapshot>> _reserveHistory = new();
    private readonly Dictionary<string, NeoServiceLayer.Core.ReserveAlertConfig> _alertConfigs = new();
    private readonly Dictionary<string, List<ReserveSubscription>> _subscriptions = new();
    private readonly object _assetsLock = new();
    private readonly Timer _monitoringTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProofOfReserveService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configuration">The service configuration.</param>
    public ProofOfReserveService(ILogger<ProofOfReserveService> logger, IServiceConfiguration? configuration = null)
        : base("ProofOfReserve", "Asset backing verification and reserve monitoring service", "1.0.0", logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX })
    {
        Configuration = configuration;

        // Initialize monitoring timer (runs every hour)
        _monitoringTimer = new Timer(MonitorReserves, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));

        AddCapability<IProofOfReserveService>();
        AddDependency(ServiceDependency.Required("OracleService", "1.0.0"));
        AddDependency(ServiceDependency.Required("KeyManagementService", "1.0.0"));
        AddDependency(ServiceDependency.Required("EnclaveManager", "1.0.0"));
    }

    /// <summary>
    /// Gets the service configuration.
    /// </summary>
    protected new IServiceConfiguration? Configuration { get; }

    /// <inheritdoc/>
    public async Task<string> RegisterAssetAsync(AssetRegistrationRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            var assetId = Guid.NewGuid().ToString();

            var monitoredAsset = new MonitoredAsset
            {
                AssetId = assetId,
                AssetSymbol = request.AssetSymbol,
                AssetName = request.AssetName,
                Type = request.Type,
                Health = ReserveHealthStatus.Unknown,
                CurrentReserveRatio = 0m,
                MinReserveRatio = request.MinReserveRatio,
                RegisteredAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                Owner = request.Owner,
                IsActive = true
            };

            lock (_assetsLock)
            {
                _monitoredAssets[assetId] = monitoredAsset;
                _reserveHistory[assetId] = new List<ReserveSnapshot>();
            }

            // Perform initial reserve check
            await UpdateReserveStatusAsync(assetId, request.ReserveAddresses, blockchainType);

            Logger.LogInformation("Registered asset {AssetId} ({Symbol}) for reserve monitoring on {Blockchain}",
                assetId, request.AssetSymbol, blockchainType);

            return assetId;
        });
    }

    /// <inheritdoc/>
    public async Task<ReserveStatusInfo> GetReserveStatusAsync(string assetId, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(assetId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        await Task.CompletedTask;

        lock (_assetsLock)
        {
            if (_monitoredAssets.TryGetValue(assetId, out var asset))
            {
                var latestSnapshot = _reserveHistory[assetId].LastOrDefault();

                return new ReserveStatusInfo
                {
                    AssetId = assetId,
                    AssetSymbol = asset.AssetSymbol,
                    TotalSupply = latestSnapshot?.TotalSupply ?? 0m,
                    TotalReserves = latestSnapshot?.TotalReserves ?? 0m,
                    ReserveRatio = asset.CurrentReserveRatio,
                    Health = asset.Health,
                    LastUpdated = asset.LastUpdated,
                    LastAudit = latestSnapshot?.Timestamp ?? DateTime.MinValue,
                    ReserveBreakdown = latestSnapshot?.ReserveAddresses ?? Array.Empty<string>(),
                    IsCompliant = asset.CurrentReserveRatio >= asset.MinReserveRatio,
                    ComplianceNotes = asset.CurrentReserveRatio < asset.MinReserveRatio ?
                        "Reserve ratio below minimum threshold" : "Compliant"
                };
            }
        }

        throw new ArgumentException($"Asset {assetId} not found", nameof(assetId));
    }

    /// <inheritdoc/>
    public async Task<Core.ProofOfReserve> GenerateProofAsync(string assetId, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(assetId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            var asset = GetMonitoredAsset(assetId);
            var latestSnapshot = GetLatestSnapshot(assetId);

            if (latestSnapshot == null)
            {
                throw new InvalidOperationException($"No reserve data available for asset {assetId}");
            }

            var proofId = Guid.NewGuid().ToString();

            // Generate cryptographic proof within the enclave
            var merkleRoot = await GenerateMerkleRootAsync(latestSnapshot.ReserveAddresses, latestSnapshot.ReserveBalances);
            var reserveProofs = await GenerateReserveProofsAsync(latestSnapshot.ReserveAddresses, latestSnapshot.ReserveBalances);

            // Sign the proof
            var proofData = $"{assetId}:{latestSnapshot.TotalSupply}:{latestSnapshot.TotalReserves}:{merkleRoot}";
            var proofHash = await ComputeHashAsync(System.Text.Encoding.UTF8.GetBytes(proofData));
            var signature = await SignProofAsync(proofHash);

            var proof = new Core.ProofOfReserve
            {
                ProofId = proofId,
                AssetId = assetId,
                GeneratedAt = DateTime.UtcNow,
                TotalSupply = latestSnapshot.TotalSupply,
                TotalReserves = latestSnapshot.TotalReserves,
                ReserveRatio = latestSnapshot.ReserveRatio,
                MerkleRoot = Convert.ToBase64String(merkleRoot),
                Signature = Convert.ToBase64String(signature),
                BlockHeight = latestSnapshot.BlockHeight,
                BlockHash = latestSnapshot.BlockHash
            };

            Logger.LogInformation("Generated proof of reserve {ProofId} for asset {AssetId} on {Blockchain}",
                proofId, assetId, blockchainType);

            return proof;
        });
    }

    /// <inheritdoc/>
    public async Task<bool> VerifyProofAsync(string proofId, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(proofId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            try
            {
                Logger.LogDebug("Verifying proof {ProofId} on {Blockchain}", proofId, blockchainType);

                // Retrieve the proof from storage using the enclave manager
                Core.ProofOfReserve? proof = null;
                
                try
                {
                    // Query the actual proof storage system
                    var storageKey = $"proof_{proofId}_{blockchainType}";
                    var proofJson = await _enclaveManager.StorageRetrieveDataAsync(
                        storageKey, 
                        GetProofEncryptionKey(), 
                        CancellationToken.None);

                    if (!string.IsNullOrEmpty(proofJson))
                    {
                        proof = System.Text.Json.JsonSerializer.Deserialize<Core.ProofOfReserve>(proofJson);
                        Logger.LogDebug("Retrieved proof {ProofId} from secure storage", proofId);
                    }
                    else
                    {
                        Logger.LogWarning("Proof {ProofId} not found in secure storage", proofId);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to retrieve proof {ProofId} from storage", proofId);
                    return false;
                }

                if (proof == null)
                {
                    Logger.LogWarning("Proof {ProofId} not found", proofId);
                    return false;
                }

                // Verify cryptographic signatures
                var proofData = $"{proof.AssetId}:{proof.TotalSupply}:{proof.TotalReserves}:{proof.MerkleRoot}";
                var proofHash = await ComputeHashAsync(System.Text.Encoding.UTF8.GetBytes(proofData));
                var signature = Convert.FromBase64String(proof.Signature);
                var signatureValid = await VerifyProofSignatureAsync(proofHash, signature);

                if (!signatureValid)
                {
                    Logger.LogWarning("Invalid signature for proof {ProofId}", proofId);
                    return false;
                }

                // Validate reserve ratio calculation
                var expectedRatio = proof.TotalSupply > 0 ? proof.TotalReserves / proof.TotalSupply : 0;
                var ratioValid = Math.Abs(proof.ReserveRatio - expectedRatio) < 0.0001m;

                if (!ratioValid)
                {
                    Logger.LogWarning("Invalid reserve ratio for proof {ProofId}", proofId);
                    return false;
                }

                // Validate merkle root
                var merkleValid = await ValidateMerkleRootAsync(proof);

                Logger.LogDebug("Proof verification for {ProofId} on {Blockchain}: {IsValid}",
                    proofId, blockchainType, signatureValid && ratioValid && merkleValid);

                return signatureValid && ratioValid && merkleValid;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error verifying proof {ProofId} on {Blockchain}", proofId, blockchainType);
                return false;
            }
        });
    }

    /// <inheritdoc/>
    public async Task<ReserveSnapshot[]> GetReserveHistoryAsync(string assetId, DateTime from, DateTime to, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(assetId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        await Task.CompletedTask;

        lock (_assetsLock)
        {
            if (_reserveHistory.TryGetValue(assetId, out var history))
            {
                return history.Where(s => s.Timestamp >= from && s.Timestamp <= to).ToArray();
            }
        }

        return Array.Empty<ReserveSnapshot>();
    }

    /// <inheritdoc/>
    public async Task<ReserveHealthStatus> GetReserveHealthAsync(string assetId, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(assetId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        await Task.CompletedTask;

        lock (_assetsLock)
        {
            if (_monitoredAssets.TryGetValue(assetId, out var asset))
            {
                var latestSnapshot = _reserveHistory[assetId].LastOrDefault();
                if (latestSnapshot != null)
                {
                    return latestSnapshot.Health;
                }
                else
                {
                    return asset.Health;
                }
            }
        }

        throw new ArgumentException($"Asset {assetId} not found", nameof(assetId));
    }

    /// <inheritdoc/>
    public async Task<string> SubscribeToReserveUpdatesAsync(string assetId, string callbackUrl, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(assetId);
        ArgumentException.ThrowIfNullOrEmpty(callbackUrl);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        var subscriptionId = Guid.NewGuid().ToString();

        // Store subscription details
        var subscription = new ReserveSubscription
        {
            SubscriptionId = subscriptionId,
            AssetId = assetId,
            CallbackUrl = callbackUrl,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        lock (_assetsLock)
        {
            if (!_subscriptions.ContainsKey(assetId))
            {
                _subscriptions[assetId] = new List<ReserveSubscription>();
            }
            _subscriptions[assetId].Add(subscription);
        }

        Logger.LogInformation("Created subscription {SubscriptionId} for asset {AssetId} on {Blockchain}",
            subscriptionId, assetId, blockchainType);

        await Task.CompletedTask;
        return subscriptionId;
    }

    /// <inheritdoc/>
    public async Task<bool> UnsubscribeFromReserveUpdatesAsync(string subscriptionId, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(subscriptionId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        // Remove the subscription
        bool removed = false;
        lock (_assetsLock)
        {
            foreach (var subscriptionList in _subscriptions.Values)
            {
                var subscription = subscriptionList.FirstOrDefault(s => s.SubscriptionId == subscriptionId);
                if (subscription != null)
                {
                    subscription.IsActive = false;
                    subscriptionList.Remove(subscription);
                    removed = true;
                    break;
                }
            }
        }

        if (removed)
        {
            Logger.LogInformation("Removed subscription {SubscriptionId} on {Blockchain}",
                subscriptionId, blockchainType);
        }
        else
        {
            Logger.LogWarning("Subscription {SubscriptionId} not found on {Blockchain}",
                subscriptionId, blockchainType);
        }

        await Task.CompletedTask;
        return removed;
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateReserveDataAsync(string assetId, ReserveUpdateRequest reserveData, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(assetId);
        ArgumentNullException.ThrowIfNull(reserveData);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            var asset = GetMonitoredAsset(assetId);

            // Verify audit signature if provided
            if (!string.IsNullOrEmpty(reserveData.AuditSignature))
            {
                var auditData = System.Text.Json.JsonSerializer.Serialize(reserveData.AuditData);
                var auditHash = await ComputeHashAsync(System.Text.Encoding.UTF8.GetBytes(auditData));
                var auditSignature = Convert.FromBase64String(reserveData.AuditSignature);

                if (!await VerifyAuditSignatureAsync(auditHash, auditSignature))
                {
                    Logger.LogWarning("Invalid audit signature for asset {AssetId} reserve update", assetId);
                    return false;
                }
            }

            // Calculate totals
            var totalReserves = reserveData.ReserveBalances.Sum();
            var totalSupply = await GetTotalSupplyAsync(assetId, blockchainType);
            var reserveRatio = totalSupply > 0 ? totalReserves / totalSupply : 0;

            // Create snapshot
            var snapshot = new ReserveSnapshot
            {
                SnapshotId = Guid.NewGuid().ToString(),
                AssetId = assetId,
                Timestamp = DateTime.UtcNow,
                TotalSupply = totalSupply,
                TotalReserves = totalReserves,
                ReserveRatio = reserveRatio,
                Health = CalculateHealthStatus(reserveRatio, asset.MinReserveRatio),
                ReserveAddresses = reserveData.ReserveAddresses,
                ReserveBalances = reserveData.ReserveBalances,
                ProofHash = Convert.ToBase64String(await ComputeHashAsync(System.Text.Encoding.UTF8.GetBytes($"{assetId}:{totalSupply}:{totalReserves}")))
            };

            // Update asset and history
            lock (_assetsLock)
            {
                asset.CurrentReserveRatio = reserveRatio;
                asset.Health = snapshot.Health;
                asset.LastUpdated = DateTime.UtcNow;

                _reserveHistory[assetId].Add(snapshot);

                // Keep only last 1000 snapshots
                if (_reserveHistory[assetId].Count > 1000)
                {
                    _reserveHistory[assetId].RemoveAt(0);
                }
            }

            // Check for alerts
            await CheckAlertsAsync(assetId, snapshot);

            Logger.LogInformation("Updated reserve data for asset {AssetId}: Ratio {Ratio:P2}, Health {Health}",
                assetId, reserveRatio, snapshot.Health);

            return true;
        });
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<MonitoredAsset>> GetRegisteredAssetsAsync(BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        await Task.CompletedTask;

        lock (_assetsLock)
        {
            return _monitoredAssets.Values.Where(a => a.IsActive).ToList();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> SetAlertThresholdAsync(string assetId, decimal threshold, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(assetId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            var alertConfig = new Core.ReserveAlertConfig
            {
                AlertId = Guid.NewGuid().ToString(),
                AssetId = assetId,
                AlertName = $"Reserve Threshold Alert for {assetId}",
                Type = ReserveAlertType.LowReserveRatio,
                Threshold = threshold,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow
            };

            lock (_assetsLock)
            {
                _alertConfigs[alertConfig.AlertId] = alertConfig;
            }

            Logger.LogInformation("Set alert threshold {Threshold:P2} for asset {AssetId} on {Blockchain}",
                threshold, assetId, blockchainType);

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to set alert threshold for asset {AssetId}", assetId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ReserveAlert>> GetActiveAlertsAsync(BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        await Task.CompletedTask;

        lock (_assetsLock)
        {
            var activeAlerts = new List<ReserveAlert>();

            foreach (var alertConfig in _alertConfigs.Values.Where(a => a.IsEnabled))
            {
                if (_monitoredAssets.TryGetValue(alertConfig.AssetId, out var asset))
                {
                    var shouldAlert = alertConfig.Type switch
                    {
                        ReserveAlertType.LowReserveRatio => asset.CurrentReserveRatio < alertConfig.Threshold,
                        ReserveAlertType.ComplianceViolation => asset.Health == ReserveHealthStatus.Undercollateralized,
                        _ => false
                    };

                    if (shouldAlert)
                    {
                        activeAlerts.Add(new ReserveAlert
                        {
                            AlertId = alertConfig.AlertId,
                            AssetId = alertConfig.AssetId,
                            AlertType = alertConfig.Type,
                            Message = $"Alert: {alertConfig.AlertName} - Current ratio: {asset.CurrentReserveRatio:P2}",
                            Severity = asset.Health == ReserveHealthStatus.Undercollateralized ? AlertSeverity.Critical : AlertSeverity.Warning,
                            TriggeredAt = DateTime.UtcNow,
                            IsActive = true
                        });
                    }
                }
            }

            return activeAlerts;
        }
    }

    /// <inheritdoc/>
    public async Task<AuditReport> GenerateAuditReportAsync(string assetId, DateTime from, DateTime to, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(assetId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            var asset = GetMonitoredAsset(assetId);
            var snapshots = await GetReserveHistoryAsync(assetId, from, to, blockchainType);

            var report = new AuditReport
            {
                ReportId = Guid.NewGuid().ToString(),
                AssetId = assetId,
                AssetSymbol = asset.AssetSymbol,
                PeriodStart = from,
                PeriodEnd = to,
                GeneratedAt = DateTime.UtcNow,
                TotalSnapshots = snapshots.Length,
                AverageReserveRatio = snapshots.Length > 0 ? snapshots.Average(s => s.ReserveRatio) : 0,
                MinReserveRatio = snapshots.Length > 0 ? snapshots.Min(s => s.ReserveRatio) : 0,
                MaxReserveRatio = snapshots.Length > 0 ? snapshots.Max(s => s.ReserveRatio) : 0,
                CompliancePercentage = snapshots.Length > 0 ?
                    (decimal)snapshots.Count(s => s.ReserveRatio >= asset.MinReserveRatio) / snapshots.Length * 100 : 0,
                Recommendations = GenerateAuditRecommendations(asset, snapshots)
            };

            Logger.LogInformation("Generated audit report {ReportId} for asset {AssetId} covering period {From} to {To}",
                report.ReportId, assetId, from, to);

            return report;
        });
    }

    /// <inheritdoc/>
    public async Task<string> SetupAlertAsync(string assetId, Core.ReserveAlertConfig alertConfig, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(assetId);
        ArgumentNullException.ThrowIfNull(alertConfig);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        await Task.CompletedTask;

        var alertId = Guid.NewGuid().ToString();

        lock (_assetsLock)
        {
            _alertConfigs[alertId] = alertConfig;
        }

        Logger.LogInformation("Setup alert {AlertId} for asset {AssetId}: {AlertType} threshold {Threshold}",
            alertId, assetId, alertConfig.Type, alertConfig.Threshold);

        return alertId;
    }

    /// <summary>
    /// Validates the merkle root of a proof.
    /// </summary>
    /// <param name="proof">The proof to validate.</param>
    /// <returns>True if the merkle root is valid.</returns>
    private async Task<bool> ValidateMerkleRootAsync(Core.ProofOfReserve proof)
    {
        try
        {
            // Get the asset's reserve data
            List<byte[]>? reserveData = null;
            lock (_assetsLock)
            {
                if (_monitoredAssets.TryGetValue(proof.AssetId, out var asset))
                {
                    var latestSnapshot = _reserveHistory[proof.AssetId].LastOrDefault();
                    if (latestSnapshot != null)
                    {
                        // Prepare merkle root data from current reserve data
                        reserveData = new List<byte[]>
                        {
                            System.Text.Encoding.UTF8.GetBytes($"supply:{proof.TotalSupply}"),
                            System.Text.Encoding.UTF8.GetBytes($"reserves:{proof.TotalReserves}"),
                            System.Text.Encoding.UTF8.GetBytes($"ratio:{proof.ReserveRatio}"),
                            System.Text.Encoding.UTF8.GetBytes($"timestamp:{proof.GeneratedAt:O}")
                        };
                    }
                }
            }

            if (reserveData != null)
            {
                var computedMerkleRoot = await ComputeMerkleRootAsync(reserveData);
                var expectedMerkleRoot = Convert.FromBase64String(proof.MerkleRoot);
                return computedMerkleRoot.SequenceEqual(expectedMerkleRoot);
            }

            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error validating merkle root for proof {ProofId}", proof.ProofId);
            return false;
        }
    }

    /// <summary>
    /// Gets active monitored assets for health monitoring.
    /// </summary>
    /// <returns>List of active monitored assets.</returns>
    private List<MonitoredAsset> GetActiveMonitoredAssets()
    {
        lock (_assetsLock)
        {
            return _monitoredAssets.Values.Where(a => a.IsActive).ToList();
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        Logger.LogInformation("Initializing Proof of Reserve Service");

        // Initialize proof of reserve specific components
        await Task.CompletedTask;

        Logger.LogInformation("Proof of Reserve Service initialized successfully");
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        Logger.LogInformation("Starting Proof of Reserve Service");
        await Task.CompletedTask;
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStopAsync()
    {
        Logger.LogInformation("Stopping Proof of Reserve Service");

        // Dispose timer
        _monitoringTimer?.Dispose();

        await Task.CompletedTask;
        return true;
    }

    /// <inheritdoc/>
    protected override Task<ServiceHealth> OnGetHealthAsync()
    {
        // Check if service is properly initialized
        if (!IsRunning)
        {
            return Task.FromResult(ServiceHealth.Unhealthy);
        }

        // Check proof of reserve specific health
        var activeAssets = GetActiveMonitoredAssets();
        var activeAssetCount = activeAssets.Count;
        var healthyAssetCount = activeAssets.Count(a => a.IsActive);

        if (activeAssetCount > 0 && healthyAssetCount < activeAssetCount * 0.8)
        {
            Logger.LogWarning("Less than 80% of monitored assets are healthy");
            return Task.FromResult(ServiceHealth.Degraded);
        }

        Logger.LogDebug("Proof of Reserve Service health check: {HealthyAssets}/{TotalAssets} assets healthy",
            healthyAssetCount, activeAssetCount);

        return Task.FromResult(ServiceHealth.Healthy);
    }

    /// <inheritdoc/>
    public new void Dispose()
    {
        _monitoringTimer?.Dispose();
        base.Dispose();
    }

    // Helper methods for Merkle root computation
    private async Task<byte[]> ComputeMerkleRootAsync(List<byte[]> data)
    {
        // Implementation for computing Merkle root
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var combined = data.SelectMany(d => d).ToArray();
        return await Task.FromResult(sha256.ComputeHash(combined));
    }

    /// <summary>
    /// Gets the encryption key for proof storage operations.
    /// </summary>
    /// <returns>The proof encryption key.</returns>
    private string GetProofEncryptionKey()
    {
        // In production, this would derive from enclave identity or configuration
        return "proof-storage-encryption-key-v1";
    }
}

// Extension method for standard deviation calculation
public static class EnumerableExtensions
{
    public static decimal StandardDeviation(this IEnumerable<decimal> values)
    {
        var enumerable = values as decimal[] ?? values.ToArray();
        var avg = enumerable.Average();
        var sum = enumerable.Sum(d => (d - avg) * (d - avg));
        return (decimal)Math.Sqrt((double)(sum / enumerable.Length));
    }
}
