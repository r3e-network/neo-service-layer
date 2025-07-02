using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Storage;

namespace NeoServiceLayer.Services.Voting.Storage;

/// <summary>
/// Helper class for managing Voting Service persistent storage operations.
/// </summary>
public class VotingStorageHelper
{
    private readonly IStorageService _storageService;
    private readonly ILogger<VotingStorageHelper> _logger;

    // Storage keys
    private const string StrategiesStorageKey = "voting:strategies";
    private const string ResultsStorageKey = "voting:results";
    private const string CandidatesStorageKey = "voting:candidates";

    /// <summary>
    /// Initializes a new instance of the <see cref="VotingStorageHelper"/> class.
    /// </summary>
    /// <param name="storageService">The storage service.</param>
    /// <param name="logger">The logger.</param>
    public VotingStorageHelper(IStorageService storageService, ILogger<VotingStorageHelper> logger)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Loads voting strategies from storage.
    /// </summary>
    /// <returns>Dictionary of voting strategies.</returns>
    public async Task<Dictionary<string, VotingStrategy>> LoadVotingStrategiesAsync()
    {
        try
        {
            var data = await _storageService.GetDataAsync(StrategiesStorageKey, BlockchainType.NeoN3);
            if (data != null && data.Length > 0)
            {
                var json = System.Text.Encoding.UTF8.GetString(data);
                var strategies = JsonSerializer.Deserialize<Dictionary<string, VotingStrategy>>(json);
                return strategies ?? new Dictionary<string, VotingStrategy>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load voting strategies from storage");
        }

        return new Dictionary<string, VotingStrategy>();
    }

    /// <summary>
    /// Loads voting results from storage.
    /// </summary>
    /// <returns>Dictionary of voting results.</returns>
    public async Task<Dictionary<string, VotingResult>> LoadVotingResultsAsync()
    {
        try
        {
            var data = await _storageService.GetDataAsync(ResultsStorageKey, BlockchainType.NeoN3);
            if (data != null && data.Length > 0)
            {
                var json = System.Text.Encoding.UTF8.GetString(data);
                var results = JsonSerializer.Deserialize<Dictionary<string, VotingResult>>(json);
                return results ?? new Dictionary<string, VotingResult>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load voting results from storage");
        }

        return new Dictionary<string, VotingResult>();
    }

    /// <summary>
    /// Loads candidates from storage.
    /// </summary>
    /// <returns>Dictionary of candidates.</returns>
    public async Task<Dictionary<string, CandidateInfo>> LoadCandidatesAsync()
    {
        try
        {
            var data = await _storageService.GetDataAsync(CandidatesStorageKey, BlockchainType.NeoN3);
            if (data != null && data.Length > 0)
            {
                var json = System.Text.Encoding.UTF8.GetString(data);
                var candidates = JsonSerializer.Deserialize<Dictionary<string, CandidateInfo>>(json);
                return candidates ?? new Dictionary<string, CandidateInfo>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load candidates from storage");
        }

        return new Dictionary<string, CandidateInfo>();
    }

    /// <summary>
    /// Persists voting strategies to storage.
    /// </summary>
    /// <param name="strategies">The strategies to persist.</param>
    public async Task PersistVotingStrategiesAsync(Dictionary<string, VotingStrategy> strategies)
    {
        try
        {
            var json = JsonSerializer.Serialize(strategies);
            var data = System.Text.Encoding.UTF8.GetBytes(json);

            var options = new StorageOptions
            {
                Encrypt = true,
                Compress = true
            };

            await _storageService.StoreDataAsync(StrategiesStorageKey, data, options, BlockchainType.NeoN3);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error persisting voting strategies");
        }
    }

    /// <summary>
    /// Persists voting results to storage.
    /// </summary>
    /// <param name="results">The results to persist.</param>
    public async Task PersistVotingResultsAsync(Dictionary<string, VotingResult> results)
    {
        try
        {
            var json = JsonSerializer.Serialize(results);
            var data = System.Text.Encoding.UTF8.GetBytes(json);

            var options = new StorageOptions
            {
                Encrypt = true,
                Compress = true
            };

            await _storageService.StoreDataAsync(ResultsStorageKey, data, options, BlockchainType.NeoN3);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error persisting voting results");
        }
    }

    /// <summary>
    /// Persists candidates to storage.
    /// </summary>
    /// <param name="candidates">The candidates to persist.</param>
    public async Task PersistCandidatesAsync(Dictionary<string, CandidateInfo> candidates)
    {
        try
        {
            var json = JsonSerializer.Serialize(candidates);
            var data = System.Text.Encoding.UTF8.GetBytes(json);

            var options = new StorageOptions
            {
                Encrypt = true,
                Compress = true
            };

            await _storageService.StoreDataAsync(CandidatesStorageKey, data, options, BlockchainType.NeoN3);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error persisting candidates");
        }
    }
}
