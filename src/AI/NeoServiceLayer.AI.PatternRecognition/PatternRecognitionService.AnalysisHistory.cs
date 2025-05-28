using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.AI.PatternRecognition.Models;

namespace NeoServiceLayer.AI.PatternRecognition;

/// <summary>
/// Analysis and history methods for the Pattern Recognition Service.
/// </summary>
public partial class PatternRecognitionService
{
    /// <inheritdoc/>
    public async Task<IEnumerable<PatternAnalysisResult>> GetAnalysisHistoryAsync(string modelId, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(modelId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await Task.Run(() =>
        {
            lock (_modelsLock)
            {
                if (_analysisHistory.TryGetValue(modelId, out var history))
                {
                    return history.ToList();
                }
            }

            return Enumerable.Empty<PatternAnalysisResult>();
        });
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Models.AnomalyDetectionResult>> GetAnomalyHistoryAsync(string modelId, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(modelId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await Task.Run(() =>
        {
            lock (_modelsLock)
            {
                if (_anomalyHistory.TryGetValue(modelId, out var history))
                {
                    return history.ToList();
                }
            }

            return Enumerable.Empty<Models.AnomalyDetectionResult>();
        });
    }

    /// <summary>
    /// Gets analysis statistics for a model.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>Analysis statistics.</returns>
    public async Task<AnalysisStatistics> GetAnalysisStatisticsAsync(string modelId, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(modelId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await Task.Run(() =>
        {
            lock (_modelsLock)
            {
                var analysisCount = _analysisHistory.TryGetValue(modelId, out var analysisHistory)
                    ? analysisHistory.Count
                    : 0;

                var anomalyCount = _anomalyHistory.TryGetValue(modelId, out var anomalyHistory)
                    ? anomalyHistory.Count
                    : 0;

                var recentAnalyses = analysisHistory?.Where(a => a.AnalyzedAt >= DateTime.UtcNow.AddDays(-7)).ToList()
                    ?? new List<PatternAnalysisResult>();

                var recentAnomalies = anomalyHistory?.Where(a => a.DetectedAt >= DateTime.UtcNow.AddDays(-7)).ToList()
                    ?? new List<Models.AnomalyDetectionResult>();

                return new AnalysisStatistics
                {
                    ModelId = modelId,
                    TotalAnalyses = analysisCount,
                    TotalAnomalies = anomalyCount,
                    AnalysesThisWeek = recentAnalyses.Count,
                    AnomaliesThisWeek = recentAnomalies.Count,
                    AverageConfidence = recentAnalyses.Any() ? recentAnalyses.Average(a => a.Confidence) : 0.0,
                    AnomalyRate = analysisCount > 0 ? (double)anomalyCount / analysisCount : 0.0,
                    LastAnalysis = analysisHistory?.LastOrDefault()?.AnalyzedAt,
                    LastAnomaly = anomalyHistory?.LastOrDefault()?.DetectedAt
                };
            }
        });
    }

    /// <summary>
    /// Gets analysis trends over time.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <param name="timeRange">The time range to analyze.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>Analysis trends.</returns>
    public async Task<AnalysisTrends> GetAnalysisTrendsAsync(string modelId, TimeSpan timeRange, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(modelId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await Task.Run(() =>
        {
            var cutoffTime = DateTime.UtcNow - timeRange;

            lock (_modelsLock)
            {
                var recentAnalyses = _analysisHistory.TryGetValue(modelId, out var analysisHistory)
                    ? analysisHistory.Where(a => a.AnalyzedAt >= cutoffTime).ToList()
                    : new List<PatternAnalysisResult>();

                var recentAnomalies = _anomalyHistory.TryGetValue(modelId, out var anomalyHistory)
                    ? anomalyHistory.Where(a => a.DetectedAt >= cutoffTime).ToList()
                    : new List<Models.AnomalyDetectionResult>();

                // Group by day for trend analysis
                var dailyAnalyses = recentAnalyses
                    .GroupBy(a => a.AnalyzedAt.Date)
                    .ToDictionary(g => g.Key, g => g.Count());

                var dailyAnomalies = recentAnomalies
                    .GroupBy(a => a.DetectedAt.Date)
                    .ToDictionary(g => g.Key, g => g.Count());

                return new AnalysisTrends
                {
                    ModelId = modelId,
                    TimeRange = timeRange,
                    DailyAnalysisCounts = dailyAnalyses,
                    DailyAnomalyCounts = dailyAnomalies,
                    TrendDirection = CalculateTrendDirection(dailyAnalyses.Values),
                    AnomalyTrendDirection = CalculateTrendDirection(dailyAnomalies.Values),
                    PeakAnalysisDay = dailyAnalyses.Any() ? dailyAnalyses.OrderByDescending(kvp => kvp.Value).First().Key : (DateTime?)null,
                    PeakAnomalyDay = dailyAnomalies.Any() ? dailyAnomalies.OrderByDescending(kvp => kvp.Value).First().Key : (DateTime?)null
                };
            }
        });
    }

    /// <summary>
    /// Clears analysis history older than specified time.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <param name="olderThan">Remove history older than this time.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>Number of records cleaned up.</returns>
    public async Task<int> CleanupAnalysisHistoryAsync(string modelId, DateTime olderThan, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(modelId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await Task.Run(() =>
        {
            var cleanedCount = 0;

            lock (_modelsLock)
            {
                // Clean analysis history
                if (_analysisHistory.TryGetValue(modelId, out var analysisHistory))
                {
                    var originalCount = analysisHistory.Count;
                    _analysisHistory[modelId] = analysisHistory.Where(a => a.AnalyzedAt >= olderThan).ToList();
                    cleanedCount += originalCount - _analysisHistory[modelId].Count;
                }

                // Clean anomaly history
                if (_anomalyHistory.TryGetValue(modelId, out var anomalyHistory))
                {
                    var originalCount = anomalyHistory.Count;
                    _anomalyHistory[modelId] = anomalyHistory.Where(a => a.DetectedAt >= olderThan).ToList();
                    cleanedCount += originalCount - _anomalyHistory[modelId].Count;
                }
            }

            if (cleanedCount > 0)
            {
                Logger.LogInformation("Cleaned up {CleanedCount} old analysis records for model {ModelId}",
                    cleanedCount, modelId);
            }

            return cleanedCount;
        });
    }

    /// <summary>
    /// Exports analysis history to a specified format.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <param name="format">The export format (json, csv, xml).</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>Exported data as byte array.</returns>
    public async Task<byte[]> ExportAnalysisHistoryAsync(string modelId, string format, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(modelId);
        ArgumentException.ThrowIfNullOrEmpty(format);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await Task.Run(() =>
        {
            lock (_modelsLock)
            {
                var analysisHistory = _analysisHistory.TryGetValue(modelId, out var history)
                    ? history
                    : new List<PatternAnalysisResult>();

                var anomalyHistory = _anomalyHistory.TryGetValue(modelId, out var anomalies)
                    ? anomalies
                    : new List<Models.AnomalyDetectionResult>();

                var exportData = new
                {
                    ModelId = modelId,
                    ExportedAt = DateTime.UtcNow,
                    AnalysisHistory = analysisHistory,
                    AnomalyHistory = anomalyHistory
                };

                return format.ToLowerInvariant() switch
                {
                    "json" => System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(exportData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })),
                    "csv" => ExportToCsv(analysisHistory, anomalyHistory),
                    "xml" => ExportToXml(exportData),
                    _ => throw new ArgumentException($"Unsupported export format: {format}")
                };
            }
        });
    }

    /// <summary>
    /// Calculates trend direction from a collection of values.
    /// </summary>
    /// <param name="values">The values to analyze.</param>
    /// <returns>Trend direction.</returns>
    private string CalculateTrendDirection(IEnumerable<int> values)
    {
        var valueList = values.ToList();
        if (valueList.Count < 2) return "Stable";

        var firstHalf = valueList.Take(valueList.Count / 2).Average();
        var secondHalf = valueList.Skip(valueList.Count / 2).Average();

        var change = (secondHalf - firstHalf) / Math.Max(firstHalf, 1);

        return change switch
        {
            > 0.1 => "Increasing",
            < -0.1 => "Decreasing",
            _ => "Stable"
        };
    }

    /// <summary>
    /// Exports data to CSV format.
    /// </summary>
    /// <param name="analysisHistory">Analysis history.</param>
    /// <param name="anomalyHistory">Anomaly history.</param>
    /// <returns>CSV data as byte array.</returns>
    private byte[] ExportToCsv(List<PatternAnalysisResult> analysisHistory, List<Models.AnomalyDetectionResult> anomalyHistory)
    {
        var csv = new System.Text.StringBuilder();

        // Analysis history CSV
        csv.AppendLine("Type,Id,Timestamp,Confidence,Result");
        foreach (var analysis in analysisHistory)
        {
            csv.AppendLine($"Analysis,{analysis.AnalysisId},{analysis.AnalyzedAt:O},{analysis.Confidence},{analysis.PatternType}");
        }

        foreach (var anomaly in anomalyHistory)
        {
            csv.AppendLine($"Anomaly,{anomaly.AnomalyId},{anomaly.DetectedAt:O},{anomaly.Confidence},{anomaly.AnomalyType}");
        }

        return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
    }

    /// <summary>
    /// Exports data to XML format.
    /// </summary>
    /// <param name="data">Data to export.</param>
    /// <returns>XML data as byte array.</returns>
    private byte[] ExportToXml(object data)
    {
        // Simple XML serialization
        var xml = $"<Export>{System.Text.Json.JsonSerializer.Serialize(data)}</Export>";
        return System.Text.Encoding.UTF8.GetBytes(xml);
    }
}
