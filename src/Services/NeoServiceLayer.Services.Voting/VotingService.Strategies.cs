using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Voting;

/// <summary>
/// Strategy management operations for the Voting Service.
/// </summary>
public partial class VotingService
{
    /// <inheritdoc/>
    public async Task<string> CreateVotingStrategyAsync(VotingStrategyRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrEmpty(request.OwnerAddress);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            var strategyId = Guid.NewGuid().ToString();
            var strategy = new VotingStrategy
            {
                Id = strategyId,
                Name = request.Name,
                Description = request.Description,
                OwnerAddress = request.OwnerAddress,
                StrategyType = request.StrategyType,
                Rules = request.Rules,
                PreferredCandidates = request.PreferredCandidates,
                FallbackCandidates = request.FallbackCandidates,
                AutoExecute = request.AutoExecute,
                ExecutionInterval = request.ExecutionInterval,
                CreatedAt = DateTime.UtcNow,
                NextExecution = request.AutoExecute ? DateTime.UtcNow.Add(request.ExecutionInterval) : null,
                Parameters = request.Parameters
            };

            lock (_strategiesLock)
            {
                _votingStrategies[strategyId] = strategy;
            }

            Logger.LogInformation("Created voting strategy {StrategyId} for owner {OwnerAddress} on {Blockchain}",
                strategyId, request.OwnerAddress, blockchainType);

            // Persist to storage
            await PersistVotingStrategiesAsync();

            return strategyId;
        });
    }

    /// <inheritdoc/>
    public Task<IEnumerable<VotingStrategy>> GetVotingStrategiesAsync(string ownerAddress, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(ownerAddress);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        lock (_strategiesLock)
        {
            return Task.FromResult<IEnumerable<VotingStrategy>>(_votingStrategies.Values
                .Where(s => s.OwnerAddress.Equals(ownerAddress, StringComparison.OrdinalIgnoreCase))
                .ToList());
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateVotingStrategyAsync(string strategyId, VotingStrategyUpdate update, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(strategyId);
        ArgumentNullException.ThrowIfNull(update);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        bool updated;
        lock (_strategiesLock)
        {
            if (_votingStrategies.TryGetValue(strategyId, out var strategy))
            {
                // Update strategy properties (only if provided)
                if (update.Name != null) strategy.Name = update.Name;
                if (update.Description != null) strategy.Description = update.Description;
                if (update.Rules != null) strategy.Rules = update.Rules;
                if (update.PreferredCandidates != null) strategy.PreferredCandidates = update.PreferredCandidates;
                if (update.FallbackCandidates != null) strategy.FallbackCandidates = update.FallbackCandidates;
                if (update.AutoExecute.HasValue) strategy.AutoExecute = update.AutoExecute.Value;
                if (update.ExecutionInterval.HasValue) strategy.ExecutionInterval = update.ExecutionInterval.Value;

                // Update next execution if auto-execute is enabled
                if (strategy.AutoExecute && update.ExecutionInterval.HasValue)
                {
                    strategy.NextExecution = DateTime.UtcNow.Add(strategy.ExecutionInterval);
                }

                Logger.LogInformation("Updated voting strategy {StrategyId} on {Blockchain}", strategyId, blockchainType);
                updated = true;
            }
            else
            {
                updated = false;
            }
        }

        if (updated)
        {
            // Persist changes to storage
            await PersistVotingStrategiesAsync();
        }
        else
        {
            Logger.LogWarning("Voting strategy {StrategyId} not found for update on {Blockchain}", strategyId, blockchainType);
        }

        return updated;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteVotingStrategyAsync(string strategyId, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(strategyId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        bool removed;
        lock (_strategiesLock)
        {
            removed = _votingStrategies.Remove(strategyId);
            if (removed)
            {
                Logger.LogInformation("Deleted voting strategy {StrategyId} on {Blockchain}", strategyId, blockchainType);
            }
        }

        if (removed)
        {
            // Persist changes to storage
            await PersistVotingStrategiesAsync();
        }

        return removed;
    }

    /// <summary>
    /// Executes automatic voting strategies.
    /// </summary>
    /// <param name="state">Timer state (unused).</param>
    private async void ExecuteAutoStrategies(object? state)
    {
        try
        {
            Logger.LogDebug("Checking for automatic voting strategies to execute");

            var strategiesToExecute = new List<(string strategyId, VotingStrategy strategy)>();

            lock (_strategiesLock)
            {
                var now = DateTime.UtcNow;
                foreach (var kvp in _votingStrategies)
                {
                    var strategy = kvp.Value;
#pragma warning disable IDE0055 // Fix formatting
                    if (strategy.AutoExecute &&
                        strategy.NextExecution.HasValue &&
                        strategy.NextExecution.Value <= now)
                    {
                        strategiesToExecute.Add((kvp.Key, strategy));
                    }
#pragma warning restore IDE0055 // Fix formatting
                }
            }

            foreach (var (strategyId, strategy) in strategiesToExecute)
            {
                try
                {
                    Logger.LogInformation("Auto-executing voting strategy {StrategyId} for owner {OwnerAddress}",
                        strategyId, strategy.OwnerAddress);

                    await ExecuteVotingAsync(strategyId, strategy.OwnerAddress, BlockchainType.NeoN3);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to auto-execute voting strategy {StrategyId}", strategyId);
                }
            }

            if (strategiesToExecute.Count > 0)
            {
                Logger.LogInformation("Auto-executed {StrategyCount} voting strategies", strategiesToExecute.Count);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during automatic strategy execution");
        }
    }
}
