using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.ProofOfReserve.Models;
using System.Collections.Concurrent;

namespace NeoServiceLayer.Services.ProofOfReserve;

/// <summary>
/// Resilience patterns implementation for the Proof of Reserve Service.
/// </summary>
public partial class ProofOfReserveService
{
    private readonly ConcurrentDictionary<string, ProofOfReserveCircuitBreaker> _circuitBreakers = new();
    private readonly object _circuitBreakerLock = new();

    /// <summary>
    /// Gets or creates a circuit breaker for a specific operation.
    /// </summary>
    /// <param name="operationKey">The operation key.</param>
    /// <param name="failureThreshold">The failure threshold.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>The circuit breaker instance.</returns>
    private ProofOfReserveCircuitBreaker GetOrCreateCircuitBreaker(string operationKey, int failureThreshold = 5, TimeSpan? timeout = null)
    {
        return _circuitBreakers.GetOrAdd(operationKey, _ => new ProofOfReserveCircuitBreaker(failureThreshold, timeout));
    }

    /// <summary>
    /// Registers an asset with resilience patterns.
    /// </summary>
    /// <param name="request">The asset registration request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The asset ID.</returns>
    private async Task<string> RegisterAssetWithResilienceAsync(AssetRegistrationRequest request, BlockchainType blockchainType)
    {
        var circuitBreaker = GetOrCreateCircuitBreaker($"RegisterAsset_{blockchainType}", 3, TimeSpan.FromMinutes(2));
        
        var result = await ProofOfReserveResilienceHelper.ExecuteWithRetryAndCircuitBreakerAsync(
            async () =>
            {
                return await ProofOfReserveResilienceHelper.ExecuteWithTimeoutAsync(
                    () => RegisterAssetInternalAsync(request, blockchainType),
                    TimeSpan.FromSeconds(30),
                    Logger,
                    "RegisterAsset");
            },
            circuitBreaker,
            Logger,
            maxRetries: 3,
            baseDelay: TimeSpan.FromMilliseconds(200),
            operationName: $"RegisterAsset_{blockchainType}");

        // Invalidate cache after successful asset registration
        InvalidateGlobalCache();
        
        return result;
    }

    /// <summary>
    /// Internal asset registration method (extracted for resilience wrapping).
    /// </summary>
    /// <param name="request">The asset registration request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The asset ID.</returns>
    private async Task<string> RegisterAssetInternalAsync(AssetRegistrationRequest request, BlockchainType blockchainType)
    {
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

            // Perform initial reserve check with resilience
            await UpdateReserveStatusWithResilienceAsync(assetId, request.ReserveAddresses, blockchainType);

            Logger.LogInformation("Registered asset {AssetId} ({Symbol}) for reserve monitoring on {Blockchain}",
                assetId, request.AssetSymbol, blockchainType);

            return assetId;
        });
    }

    /// <summary>
    /// Generates a proof with resilience patterns.
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The proof of reserve.</returns>
    private async Task<Core.ProofOfReserve> GenerateProofWithResilienceAsync(string assetId, BlockchainType blockchainType)
    {
        var circuitBreaker = GetOrCreateCircuitBreaker($"GenerateProof_{blockchainType}", 3, TimeSpan.FromMinutes(3));
        
        return await ProofOfReserveResilienceHelper.ExecuteWithRetryAndCircuitBreakerAsync(
            async () =>
            {
                return await ProofOfReserveResilienceHelper.ExecuteWithTimeoutAsync(
                    () => GenerateProofInternalAsync(assetId, blockchainType),
                    TimeSpan.FromMinutes(2),
                    Logger,
                    "GenerateProof");
            },
            circuitBreaker,
            Logger,
            maxRetries: 2,
            baseDelay: TimeSpan.FromMilliseconds(500),
            operationName: $"GenerateProof_{blockchainType}");
    }

    /// <summary>
    /// Internal proof generation method (extracted for resilience wrapping).
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The proof of reserve.</returns>
    private async Task<Core.ProofOfReserve> GenerateProofInternalAsync(string assetId, BlockchainType blockchainType)
    {
        return await ExecuteInEnclaveAsync(async () =>
        {
            var asset = GetMonitoredAsset(assetId);
            var latestSnapshot = GetLatestSnapshot(assetId);

            if (latestSnapshot == null)
            {
                throw new InvalidOperationException($"No reserve data available for asset {assetId}");
            }

            var proofId = Guid.NewGuid().ToString();

            // Generate cryptographic proof within the enclave with resilience
            var merkleRoot = await ProofOfReserveResilienceHelper.ExecuteEnclaveOperationAsync(
                () => GenerateMerkleRootAsync(latestSnapshot.ReserveAddresses, latestSnapshot.ReserveBalances),
                Logger,
                operationName: "GenerateMerkleRoot");

            var reserveProofs = await ProofOfReserveResilienceHelper.ExecuteEnclaveOperationAsync(
                () => GenerateReserveProofsAsync(latestSnapshot.ReserveAddresses, latestSnapshot.ReserveBalances),
                Logger,
                operationName: "GenerateReserveProofs");

            // Sign the proof with resilience
            var proofData = $"{assetId}:{latestSnapshot.TotalSupply}:{latestSnapshot.TotalReserves}:{Convert.ToBase64String(merkleRoot)}";
            var proofHash = await ProofOfReserveResilienceHelper.ExecuteEnclaveOperationAsync(
                () => ComputeHashAsync(System.Text.Encoding.UTF8.GetBytes(proofData)),
                Logger,
                operationName: "ComputeProofHash");

            var signature = await ProofOfReserveResilienceHelper.ExecuteEnclaveOperationAsync(
                () => SignProofAsync(proofHash),
                Logger,
                operationName: "SignProof");

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

            // Store proof with resilience
            await StoreProofWithResilienceAsync(proof, blockchainType);

            Logger.LogInformation("Generated proof of reserve {ProofId} for asset {AssetId} on {Blockchain}",
                proofId, assetId, blockchainType);

            return proof;
        });
    }

    /// <summary>
    /// Verifies a proof with resilience patterns.
    /// </summary>
    /// <param name="proofId">The proof ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the proof is valid.</returns>
    private async Task<bool> VerifyProofWithResilienceAsync(string proofId, BlockchainType blockchainType)
    {
        var circuitBreaker = GetOrCreateCircuitBreaker($"VerifyProof_{blockchainType}", 3, TimeSpan.FromMinutes(1));
        
        return await ProofOfReserveResilienceHelper.ExecuteWithRetryAndCircuitBreakerAsync(
            async () =>
            {
                return await ProofOfReserveResilienceHelper.ExecuteWithTimeoutAsync(
                    () => VerifyProofInternalAsync(proofId, blockchainType),
                    TimeSpan.FromSeconds(45),
                    Logger,
                    "VerifyProof");
            },
            circuitBreaker,
            Logger,
            maxRetries: 2,
            baseDelay: TimeSpan.FromMilliseconds(300),
            operationName: $"VerifyProof_{blockchainType}");
    }

    /// <summary>
    /// Internal proof verification method (extracted for resilience wrapping).
    /// </summary>
    /// <param name="proofId">The proof ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the proof is valid.</returns>
    private async Task<bool> VerifyProofInternalAsync(string proofId, BlockchainType blockchainType)
    {
        return await ExecuteInEnclaveAsync(async () =>
        {
            try
            {
                Logger.LogDebug("Verifying proof {ProofId} on {Blockchain}", proofId, blockchainType);

                // Retrieve the proof from storage with resilience
                var proof = await RetrieveProofWithResilienceAsync(proofId, blockchainType);

                if (proof == null)
                {
                    Logger.LogWarning("Proof {ProofId} not found", proofId);
                    return false;
                }

                // Verify cryptographic signatures with resilience
                var proofData = $"{proof.AssetId}:{proof.TotalSupply}:{proof.TotalReserves}:{proof.MerkleRoot}";
                var proofHash = await ProofOfReserveResilienceHelper.ExecuteEnclaveOperationAsync(
                    () => ComputeHashAsync(System.Text.Encoding.UTF8.GetBytes(proofData)),
                    Logger,
                    operationName: "ComputeVerificationHash");

                var signature = Convert.FromBase64String(proof.Signature);
                var signatureValid = await ProofOfReserveResilienceHelper.ExecuteEnclaveOperationAsync(
                    () => VerifyProofSignatureAsync(proofHash, signature),
                    Logger,
                    operationName: "VerifySignature");

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

                // Validate merkle root with resilience
                var merkleValid = await ProofOfReserveResilienceHelper.ExecuteEnclaveOperationAsync(
                    () => ValidateMerkleRootAsync(proof),
                    Logger,
                    operationName: "ValidateMerkleRoot");

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

    /// <summary>
    /// Updates reserve data with resilience patterns.
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <param name="reserveData">The reserve update request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if update was successful.</returns>
    private async Task<bool> UpdateReserveDataWithResilienceAsync(string assetId, ReserveUpdateRequest reserveData, BlockchainType blockchainType)
    {
        var circuitBreaker = GetOrCreateCircuitBreaker($"UpdateReserveData_{blockchainType}", 5, TimeSpan.FromMinutes(1));
        
        return await ProofOfReserveResilienceHelper.ExecuteWithRetryAndCircuitBreakerAsync(
            async () =>
            {
                return await ProofOfReserveResilienceHelper.ExecuteWithTimeoutAsync(
                    () => UpdateReserveDataInternalAsync(assetId, reserveData, blockchainType),
                    TimeSpan.FromSeconds(30),
                    Logger,
                    "UpdateReserveData");
            },
            circuitBreaker,
            Logger,
            maxRetries: 3,
            baseDelay: TimeSpan.FromMilliseconds(100),
            operationName: $"UpdateReserveData_{blockchainType}");
    }

    /// <summary>
    /// Internal reserve data update method (extracted for resilience wrapping).
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <param name="reserveData">The reserve update request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if update was successful.</returns>
    private async Task<bool> UpdateReserveDataInternalAsync(string assetId, ReserveUpdateRequest reserveData, BlockchainType blockchainType)
    {
        return await ExecuteInEnclaveAsync(async () =>
        {
            var asset = GetMonitoredAsset(assetId);

            // Verify audit signature if provided with resilience
            if (!string.IsNullOrEmpty(reserveData.AuditSignature))
            {
                var auditVerificationSuccessful = await ProofOfReserveResilienceHelper.ExecuteEnclaveOperationAsync(
                    async () =>
                    {
                        var auditData = System.Text.Json.JsonSerializer.Serialize(reserveData.AuditData);
                        var auditHash = await ComputeHashAsync(System.Text.Encoding.UTF8.GetBytes(auditData));
                        var auditSignature = Convert.FromBase64String(reserveData.AuditSignature);
                        return await VerifyAuditSignatureAsync(auditHash, auditSignature);
                    },
                    Logger,
                    operationName: "VerifyAuditSignature");

                if (!auditVerificationSuccessful)
                {
                    Logger.LogWarning("Invalid audit signature for asset {AssetId} reserve update", assetId);
                    return false;
                }
            }

            // Calculate totals
            var totalReserves = reserveData.ReserveBalances.Sum();
            var totalSupply = await ProofOfReserveResilienceHelper.ExecuteBlockchainOperationAsync(
                () => GetTotalSupplyAsync(assetId, blockchainType),
                Logger,
                operationName: "GetTotalSupply");

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

            // Check for alerts with resilience
            await ProofOfReserveResilienceHelper.ExecuteWithRetryAsync(
                () => CheckAlertsAsync(assetId, snapshot),
                Logger,
                maxRetries: 2,
                operationName: "CheckAlerts");

            Logger.LogInformation("Updated reserve data for asset {AssetId}: Ratio {Ratio:P2}, Health {Health}",
                assetId, reserveRatio, snapshot.Health);

            return true;
        });
    }

    /// <summary>
    /// Updates reserve status with resilience patterns.
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <param name="reserveAddresses">The reserve addresses.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    private async Task UpdateReserveStatusWithResilienceAsync(string assetId, string[] reserveAddresses, BlockchainType blockchainType)
    {
        try
        {
            // Fetch reserve balances from blockchain with resilience
            var balances = await ProofOfReserveResilienceHelper.ExecuteBlockchainOperationAsync(
                () => FetchReserveBalancesAsync(reserveAddresses, blockchainType),
                Logger,
                operationName: "FetchReserveBalances");

            var updateRequest = new ReserveUpdateRequest
            {
                ReserveAddresses = reserveAddresses,
                ReserveBalances = balances,
                AuditSource = "NeoServiceLayer",
                AuditTimestamp = DateTime.UtcNow
            };

            var updateSuccessful = await UpdateReserveDataWithResilienceAsync(assetId, updateRequest, blockchainType);
            
            // Invalidate cache after successful update
            if (updateSuccessful)
            {
                InvalidateAssetCache(assetId);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating reserve status for asset {AssetId} with resilience", assetId);
        }
    }

    /// <summary>
    /// Stores a proof with resilience patterns.
    /// </summary>
    /// <param name="proof">The proof to store.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    private async Task StoreProofWithResilienceAsync(Core.ProofOfReserve proof, BlockchainType blockchainType)
    {
        var circuitBreaker = GetOrCreateCircuitBreaker("ProofStorage", 5, TimeSpan.FromMinutes(1));

        await ProofOfReserveResilienceHelper.ExecuteWithRetryAndCircuitBreakerAsync<bool>(
            async () =>
            {
                try
                {
                    if (_enclaveManager != null)
                    {
                        var storageKey = $"proof_{proof.ProofId}_{blockchainType}";
                        var proofJson = System.Text.Json.JsonSerializer.Serialize(proof);
                        await _enclaveManager.StorageStoreDataAsync(
                            storageKey, 
                            proofJson, 
                            GetProofEncryptionKey(), 
                            CancellationToken.None);
                    }

                    Logger.LogDebug("Stored proof {ProofId} securely", proof.ProofId);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to store proof {ProofId}", proof.ProofId);
                    throw;
                }
            },
            circuitBreaker,
            Logger,
            maxRetries: 3,
            baseDelay: TimeSpan.FromMilliseconds(100),
            operationName: "StoreProof");
    }

    /// <summary>
    /// Retrieves a proof with resilience patterns.
    /// </summary>
    /// <param name="proofId">The proof ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The retrieved proof.</returns>
    private async Task<Core.ProofOfReserve?> RetrieveProofWithResilienceAsync(string proofId, BlockchainType blockchainType)
    {
        var circuitBreaker = GetOrCreateCircuitBreaker("ProofRetrieval", 5, TimeSpan.FromMinutes(1));

        return await ProofOfReserveResilienceHelper.ExecuteWithRetryAndCircuitBreakerAsync(
            async () =>
            {
                try
                {
                    if (_enclaveManager != null)
                    {
                        var storageKey = $"proof_{proofId}_{blockchainType}";
                        var proofJson = await _enclaveManager.StorageRetrieveDataAsync(
                            storageKey, 
                            GetProofEncryptionKey(), 
                            CancellationToken.None);

                        if (!string.IsNullOrEmpty(proofJson))
                        {
                            var proof = System.Text.Json.JsonSerializer.Deserialize<Core.ProofOfReserve>(proofJson);
                            Logger.LogDebug("Retrieved proof {ProofId} from secure storage", proofId);
                            return proof;
                        }
                    }

                    Logger.LogWarning("Proof {ProofId} not found in secure storage", proofId);
                    return null;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to retrieve proof {ProofId} from storage", proofId);
                    throw;
                }
            },
            circuitBreaker,
            Logger,
            maxRetries: 3,
            baseDelay: TimeSpan.FromMilliseconds(100),
            operationName: "RetrieveProof");
    }

    /// <summary>
    /// Gets circuit breaker status for monitoring.
    /// </summary>
    /// <returns>Circuit breaker status information.</returns>
    public Dictionary<string, object> GetCircuitBreakerStatus()
    {
        var status = new Dictionary<string, object>();

        foreach (var kvp in _circuitBreakers)
        {
            status[kvp.Key] = kvp.Value.GetStatus();
        }

        return status;
    }

    /// <summary>
    /// Resets all circuit breakers (for administrative purposes).
    /// </summary>
    public void ResetCircuitBreakers()
    {
        lock (_circuitBreakerLock)
        {
            foreach (var circuitBreaker in _circuitBreakers.Values)
            {
                circuitBreaker.Reset();
            }
            
            Logger.LogInformation("All circuit breakers have been reset");
        }
    }
}