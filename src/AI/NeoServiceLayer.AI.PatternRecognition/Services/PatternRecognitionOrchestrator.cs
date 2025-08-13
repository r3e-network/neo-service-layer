using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.AI.PatternRecognition.Analyzers;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.AI.PatternRecognition.Services
{
    /// <summary>
    /// Orchestrates pattern recognition across multiple analyzers.
    /// </summary>
    public class PatternRecognitionOrchestrator : IPatternRecognitionService
    {
        private readonly ILogger<PatternRecognitionOrchestrator> _logger;
        private readonly Dictionary<PatternType, IPatternAnalyzer> _analyzers;
        private readonly IMetricsCollector? _metricsCollector;
        private readonly Dictionary<string, List<PatternAnalysisResult>> _analysisHistory;

        public PatternRecognitionOrchestrator(
            ILogger<PatternRecognitionOrchestrator> logger,
            IEnumerable<IPatternAnalyzer> analyzers,
            IMetricsCollector? metricsCollector = null)
        {
            _logger = logger;
            _analyzers = analyzers.ToDictionary(a => a.SupportedType);
            _metricsCollector = metricsCollector;
            _analysisHistory = new Dictionary<string, List<PatternAnalysisResult>>();

            _logger.LogInformation("Initialized pattern recognition orchestrator with {Count} analyzers",
                _analyzers.Count);
        }

        public async Task<PatternAnalysisResult> AnalyzePatternAsync(PatternAnalysisRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var startTime = DateTime.UtcNow;
            _logger.LogDebug("Starting pattern analysis with {DataPoints} data points", request.Data.Length);

            try
            {
                // Determine which analyzer to use
                var patternType = request.PreferredType ?? DeterminePatternType(request.Data);

                if (!_analyzers.TryGetValue(patternType, out var analyzer))
                {
                    _logger.LogWarning("No analyzer available for pattern type {Type}", patternType);
                    return new PatternAnalysisResult
                    {
                        Success = false,
                        Message = $"No analyzer available for pattern type {patternType}"
                    };
                }

                // Execute analysis
                var result = await analyzer.AnalyzeAsync(request).ConfigureAwait(false);

                // Track metrics
                if (_metricsCollector != null)
                {
                    await _metricsCollector.RecordMetricAsync("pattern_analysis", 1, new Dictionary<string, object>
                    {
                        ["pattern_type"] = patternType.ToString(),
                        ["patterns_found"] = result.Patterns.Length,
                        ["confidence"] = result.Confidence,
                        ["duration_ms"] = (DateTime.UtcNow - startTime).TotalMilliseconds
                    }).ConfigureAwait(false);
                }

                // Store in history
                StoreAnalysisResult(request.Context ?? "default", result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during pattern analysis");

                return new PatternAnalysisResult
                {
                    Success = false,
                    Message = $"Analysis failed: {ex.Message}"
                };
            }
        }

        public async Task<PatternAnalysisResult> AnalyzeMultiplePatternsAsync(PatternAnalysisRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            _logger.LogDebug("Starting multi-pattern analysis with {DataPoints} data points", request.Data.Length);

            var allPatterns = new List<DetectedPattern>();
            var tasks = new List<Task<PatternAnalysisResult>>();

            // Run all analyzers in parallel
            foreach (var analyzer in _analyzers.Values)
            {
                tasks.Add(analyzer.AnalyzeAsync(request));
            }

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            // Combine results
            foreach (var result in results.Where(r => r.Success))
            {
                allPatterns.AddRange(result.Patterns);
            }

            // Remove duplicates and overlapping patterns
            var dedupedPatterns = DeduplicatePatterns(allPatterns);

            return new PatternAnalysisResult
            {
                Success = true,
                Patterns = dedupedPatterns.ToArray(),
                AnalysisTime = DateTime.UtcNow,
                Confidence = dedupedPatterns.Any() ? dedupedPatterns.Max(p => p.Confidence) : 0,
                Message = $"Found {dedupedPatterns.Count} unique patterns across {_analyzers.Count} analyzers"
            };
        }

        public async Task<TrainingResult> TrainModelAsync(TrainingData data)
        {
            ArgumentNullException.ThrowIfNull(data);

            _logger.LogInformation("Starting model training with {Count} samples", data.Samples.Count);

            var results = new List<TrainingResult>();

            // Train all analyzers
            foreach (var analyzer in _analyzers.Values)
            {
                try
                {
                    var result = await analyzer.TrainAsync(data).ConfigureAwait(false);
                    results.Add(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error training {Analyzer}", analyzer.GetType().Name);
                }
            }

            // Aggregate results
            var overallAccuracy = results.Any() ? results.Average(r => r.Accuracy) : 0;
            var totalSamples = results.Sum(r => r.TrainingSamples);

            return new TrainingResult
            {
                Success = results.Any(r => r.Success),
                Accuracy = overallAccuracy,
                TrainingSamples = totalSamples,
                Message = $"Trained {results.Count(r => r.Success)}/{_analyzers.Count} analyzers successfully"
            };
        }

        public async Task<IEnumerable<DetectedPattern>> GetHistoricalPatternsAsync(string context, TimeSpan period)
        {
            var cutoff = DateTime.UtcNow - period;
            var patterns = new List<DetectedPattern>();

            if (_analysisHistory.TryGetValue(context, out var history))
            {
                var recentAnalyses = history.Where(h => h.AnalysisTime >= cutoff);

                foreach (var analysis in recentAnalyses)
                {
                    patterns.AddRange(analysis.Patterns);
                }
            }

            return await Task.FromResult(patterns).ConfigureAwait(false);
        }

        public void RegisterAnalyzer(PatternType type, IPatternAnalyzer analyzer)
        {
            ArgumentNullException.ThrowIfNull(analyzer);

            _analyzers[type] = analyzer;
            _logger.LogInformation("Registered analyzer for pattern type {Type}", type);
        }

        public IPatternAnalyzer? GetAnalyzer(PatternType type)
        {
            return _analyzers.GetValueOrDefault(type);
        }

        private PatternType DeterminePatternType(double[] data)
        {
            // Simple heuristics to determine the most likely pattern type
            if (data.Length < 10)
                return PatternType.Unknown;

            var variance = CalculateVariance(data);
            var mean = data.Average();
            var cv = variance > 0 ? Math.Sqrt(variance) / Math.Abs(mean) : 0;

            // High coefficient of variation might indicate anomalies
            if (cv > 1.5)
                return PatternType.Anomaly;

            // Check for trending
            var trend = CalculateTrendStrength(data);
            if (Math.Abs(trend) > 0.5)
                return PatternType.Trend;

            // Check for repeating patterns
            var autocorr = CalculateAutocorrelation(data, Math.Min(10, data.Length / 4));
            if (autocorr > 0.6)
                return PatternType.Sequence;

            // Default to behavioral analysis
            return PatternType.Behavioral;
        }

        private double CalculateVariance(double[] data)
        {
            var mean = data.Average();
            return data.Select(x => Math.Pow(x - mean, 2)).Average();
        }

        private double CalculateTrendStrength(double[] data)
        {
            var n = data.Length;
            var xValues = Enumerable.Range(0, n).Select(i => (double)i).ToArray();

            var xMean = xValues.Average();
            var yMean = data.Average();

            var numerator = 0.0;
            var denominatorX = 0.0;
            var denominatorY = 0.0;

            for (int i = 0; i < n; i++)
            {
                var xDiff = xValues[i] - xMean;
                var yDiff = data[i] - yMean;

                numerator += xDiff * yDiff;
                denominatorX += xDiff * xDiff;
                denominatorY += yDiff * yDiff;
            }

            if (denominatorX == 0 || denominatorY == 0)
                return 0;

            return numerator / Math.Sqrt(denominatorX * denominatorY);
        }

        private double CalculateAutocorrelation(double[] data, int lag)
        {
            var n = data.Length;
            var mean = data.Average();

            var numerator = 0.0;
            var denominator = 0.0;

            for (int i = 0; i < n - lag; i++)
            {
                numerator += (data[i] - mean) * (data[i + lag] - mean);
            }

            for (int i = 0; i < n; i++)
            {
                denominator += (data[i] - mean) * (data[i] - mean);
            }

            return denominator != 0 ? numerator / denominator : 0;
        }

        private List<DetectedPattern> DeduplicatePatterns(List<DetectedPattern> patterns)
        {
            var deduped = new List<DetectedPattern>();
            var processed = new bool[patterns.Count];

            for (int i = 0; i < patterns.Count; i++)
            {
                if (processed[i])
                    continue;

                var current = patterns[i];
                var overlapping = new List<DetectedPattern> { current };

                for (int j = i + 1; j < patterns.Count; j++)
                {
                    if (processed[j])
                        continue;

                    var other = patterns[j];

                    // Check for significant overlap
                    var overlap = CalculateOverlap(current, other);

                    if (overlap > 0.5 && current.Type == other.Type)
                    {
                        overlapping.Add(other);
                        processed[j] = true;
                    }
                }

                // Keep the pattern with highest confidence
                deduped.Add(overlapping.OrderByDescending(p => p.Confidence).First());
                processed[i] = true;
            }

            return deduped;
        }

        private double CalculateOverlap(DetectedPattern p1, DetectedPattern p2)
        {
            var start = Math.Max(p1.StartIndex, p2.StartIndex);
            var end = Math.Min(p1.EndIndex, p2.EndIndex);

            if (start > end)
                return 0;

            var overlap = end - start + 1;
            var totalSpan = Math.Max(p1.EndIndex, p2.EndIndex) - Math.Min(p1.StartIndex, p2.StartIndex) + 1;

            return (double)overlap / totalSpan;
        }

        private void StoreAnalysisResult(string context, PatternAnalysisResult result)
        {
            if (!_analysisHistory.ContainsKey(context))
            {
                _analysisHistory[context] = new List<PatternAnalysisResult>();
            }

            _analysisHistory[context].Add(result);

            // Keep only last 100 results per context
            if (_analysisHistory[context].Count > 100)
            {
                _analysisHistory[context].RemoveAt(0);
            }
        }
    }
}
