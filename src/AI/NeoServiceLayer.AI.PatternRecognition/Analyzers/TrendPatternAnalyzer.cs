using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.AI.PatternRecognition.Analyzers
{
    /// <summary>
    /// Analyzer for detecting trend patterns in time series data.
    /// </summary>
    public class TrendPatternAnalyzer : PatternAnalyzerBase
    {
        private readonly int _trendWindowSize;
        private readonly double _trendStrengthThreshold;
        private readonly int _smoothingWindow;

        public TrendPatternAnalyzer(
            ILogger<TrendPatternAnalyzer> logger,
            int trendWindowSize = 20,
            double trendStrengthThreshold = 0.7,
            int smoothingWindow = 5)
            : base(logger)
        {
            _trendWindowSize = trendWindowSize;
            _trendStrengthThreshold = trendStrengthThreshold;
            _smoothingWindow = smoothingWindow;
        }

        public override PatternType SupportedType => PatternType.Trend;

        public override async Task<PatternAnalysisResult> AnalyzeAsync(PatternAnalysisRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            _logger.LogDebug("Analyzing trends in {Count} data points", request.Data.Length);

            var patterns = new List<DetectedPattern>();

            // Apply smoothing to reduce noise
            var smoothedData = ApplyMovingAverage(request.Data, _smoothingWindow);

            // Detect linear trends
            var linearTrends = DetectLinearTrends(smoothedData);
            patterns.AddRange(linearTrends);

            // Detect exponential trends
            var exponentialTrends = DetectExponentialTrends(smoothedData);
            patterns.AddRange(exponentialTrends);

            // Detect cyclical patterns
            var cyclicalPatterns = DetectCyclicalPatterns(request.Data);
            patterns.AddRange(cyclicalPatterns);

            // Detect support and resistance levels
            var supportResistance = DetectSupportResistanceLevels(request.Data);
            patterns.AddRange(supportResistance);

            var result = new PatternAnalysisResult
            {
                Success = true,
                Patterns = patterns.ToArray(),
                AnalysisTime = DateTime.UtcNow,
                Confidence = patterns.Any() ? patterns.Max(p => p.Confidence) : 0,
                Message = $"Identified {patterns.Count} trend patterns"
            };

            await Task.CompletedTask.ConfigureAwait(false);
            return result;
        }

        private double[] ApplyMovingAverage(double[] data, int windowSize)
        {
            if (data.Length < windowSize)
                return data;

            var smoothed = new double[data.Length];

            for (int i = 0; i < data.Length; i++)
            {
                var start = Math.Max(0, i - windowSize / 2);
                var end = Math.Min(data.Length, i + windowSize / 2 + 1);
                var window = data.Skip(start).Take(end - start);
                smoothed[i] = window.Average();
            }

            return smoothed;
        }

        private List<DetectedPattern> DetectLinearTrends(double[] data)
        {
            var patterns = new List<DetectedPattern>();

            for (int i = 0; i <= data.Length - _trendWindowSize; i++)
            {
                var window = data.Skip(i).Take(_trendWindowSize).ToArray();
                var trendInfo = CalculateLinearTrend(window);

                if (Math.Abs(trendInfo.Correlation) >= _trendStrengthThreshold)
                {
                    var trendType = trendInfo.Slope > 0 ? "Uptrend" : "Downtrend";

                    patterns.Add(new DetectedPattern
                    {
                        Type = PatternType.Trend,
                        Name = $"Linear {trendType}",
                        Confidence = Math.Abs(trendInfo.Correlation),
                        StartIndex = i,
                        EndIndex = i + _trendWindowSize - 1,
                        Metadata = new Dictionary<string, object>
                        {
                            ["slope"] = trendInfo.Slope,
                            ["intercept"] = trendInfo.Intercept,
                            ["correlation"] = trendInfo.Correlation,
                            ["trend_strength"] = Math.Abs(trendInfo.Slope) * Math.Abs(trendInfo.Correlation)
                        }
                    });
                }
            }

            return MergeOverlappingTrends(patterns);
        }

        private List<DetectedPattern> DetectExponentialTrends(double[] data)
        {
            var patterns = new List<DetectedPattern>();

            for (int i = 0; i <= data.Length - _trendWindowSize; i++)
            {
                var window = data.Skip(i).Take(_trendWindowSize).ToArray();

                // Convert to log scale to detect exponential trends
                var logWindow = window.Where(v => v > 0).Select(v => Math.Log(v)).ToArray();

                if (logWindow.Length < _trendWindowSize * 0.8)
                    continue; // Skip if too many non-positive values

                var trendInfo = CalculateLinearTrend(logWindow);

                if (Math.Abs(trendInfo.Correlation) >= _trendStrengthThreshold)
                {
                    var growthRate = Math.Exp(trendInfo.Slope) - 1;
                    var trendType = growthRate > 0 ? "Growth" : "Decay";

                    patterns.Add(new DetectedPattern
                    {
                        Type = PatternType.Trend,
                        Name = $"Exponential {trendType}",
                        Confidence = Math.Abs(trendInfo.Correlation) * 0.9,
                        StartIndex = i,
                        EndIndex = i + _trendWindowSize - 1,
                        Metadata = new Dictionary<string, object>
                        {
                            ["growth_rate"] = growthRate,
                            ["doubling_time"] = Math.Log(2) / trendInfo.Slope,
                            ["correlation"] = trendInfo.Correlation
                        }
                    });
                }
            }

            return patterns;
        }

        private List<DetectedPattern> DetectCyclicalPatterns(double[] data)
        {
            var patterns = new List<DetectedPattern>();

            // Detect cycles using autocorrelation
            var maxLag = Math.Min(data.Length / 2, 100);
            var autocorrelations = new double[maxLag];

            for (int lag = 1; lag < maxLag; lag++)
            {
                autocorrelations[lag] = CalculateAutocorrelation(data, lag);
            }

            // Find peaks in autocorrelation
            for (int i = 2; i < maxLag - 1; i++)
            {
                if (autocorrelations[i] > autocorrelations[i - 1] &&
                    autocorrelations[i] > autocorrelations[i + 1] &&
                    autocorrelations[i] > 0.5)
                {
                    patterns.Add(new DetectedPattern
                    {
                        Type = PatternType.Trend,
                        Name = "Cyclical Pattern",
                        Confidence = autocorrelations[i],
                        StartIndex = 0,
                        EndIndex = data.Length - 1,
                        Metadata = new Dictionary<string, object>
                        {
                            ["period"] = i,
                            ["frequency"] = 1.0 / i,
                            ["autocorrelation"] = autocorrelations[i]
                        }
                    });
                }
            }

            return patterns;
        }

        private List<DetectedPattern> DetectSupportResistanceLevels(double[] data)
        {
            var patterns = new List<DetectedPattern>();

            // Find local minima and maxima
            var localMinima = new List<(int Index, double Value)>();
            var localMaxima = new List<(int Index, double Value)>();

            for (int i = 1; i < data.Length - 1; i++)
            {
                if (data[i] < data[i - 1] && data[i] < data[i + 1])
                {
                    localMinima.Add((i, data[i]));
                }
                else if (data[i] > data[i - 1] && data[i] > data[i + 1])
                {
                    localMaxima.Add((i, data[i]));
                }
            }

            // Cluster similar levels
            var supportLevels = ClusterLevels(localMinima.Select(m => m.Value).ToList());
            var resistanceLevels = ClusterLevels(localMaxima.Select(m => m.Value).ToList());

            foreach (var level in supportLevels)
            {
                patterns.Add(new DetectedPattern
                {
                    Type = PatternType.Trend,
                    Name = "Support Level",
                    Confidence = level.Strength,
                    StartIndex = 0,
                    EndIndex = data.Length - 1,
                    Metadata = new Dictionary<string, object>
                    {
                        ["level"] = level.Value,
                        ["touches"] = level.Count,
                        ["strength"] = level.Strength
                    }
                });
            }

            foreach (var level in resistanceLevels)
            {
                patterns.Add(new DetectedPattern
                {
                    Type = PatternType.Trend,
                    Name = "Resistance Level",
                    Confidence = level.Strength,
                    StartIndex = 0,
                    EndIndex = data.Length - 1,
                    Metadata = new Dictionary<string, object>
                    {
                        ["level"] = level.Value,
                        ["touches"] = level.Count,
                        ["strength"] = level.Strength
                    }
                });
            }

            return patterns;
        }

        private TrendInfo CalculateLinearTrend(double[] data)
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

            var slope = denominatorX != 0 ? numerator / denominatorX : 0;
            var intercept = yMean - slope * xMean;
            var correlation = denominatorX != 0 && denominatorY != 0
                ? numerator / Math.Sqrt(denominatorX * denominatorY)
                : 0;

            return new TrendInfo
            {
                Slope = slope,
                Intercept = intercept,
                Correlation = correlation
            };
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

        private List<DetectedPattern> MergeOverlappingTrends(List<DetectedPattern> patterns)
        {
            var merged = new List<DetectedPattern>();
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

                    // Check for overlap
                    if (current.EndIndex >= other.StartIndex && current.StartIndex <= other.EndIndex)
                    {
                        // Check if trends are similar
                        var currentSlope = (double)current.Metadata["slope"];
                        var otherSlope = (double)other.Metadata["slope"];

                        if (Math.Sign(currentSlope) == Math.Sign(otherSlope))
                        {
                            overlapping.Add(other);
                            processed[j] = true;
                        }
                    }
                }

                // Merge overlapping trends
                if (overlapping.Count > 1)
                {
                    var mergedPattern = new DetectedPattern
                    {
                        Type = PatternType.Trend,
                        Name = current.Name,
                        Confidence = overlapping.Max(p => p.Confidence),
                        StartIndex = overlapping.Min(p => p.StartIndex),
                        EndIndex = overlapping.Max(p => p.EndIndex),
                        Metadata = current.Metadata
                    };
                    merged.Add(mergedPattern);
                }
                else
                {
                    merged.Add(current);
                }

                processed[i] = true;
            }

            return merged;
        }

        private List<LevelInfo> ClusterLevels(List<double> values)
        {
            var clusters = new List<LevelInfo>();
            var threshold = values.Any() ? values.Average() * 0.02 : 0; // 2% threshold

            foreach (var value in values)
            {
                var existingCluster = clusters.FirstOrDefault(c => Math.Abs(c.Value - value) < threshold);

                if (existingCluster != null)
                {
                    existingCluster.Count++;
                    existingCluster.Value = (existingCluster.Value * (existingCluster.Count - 1) + value) / existingCluster.Count;
                }
                else
                {
                    clusters.Add(new LevelInfo { Value = value, Count = 1 });
                }
            }

            // Calculate strength based on touch count
            foreach (var cluster in clusters)
            {
                cluster.Strength = Math.Min(0.95, 0.5 + cluster.Count * 0.1);
            }

            return clusters.Where(c => c.Count >= 2).OrderByDescending(c => c.Strength).ToList();
        }

        private class TrendInfo
        {
            public double Slope { get; set; }
            public double Intercept { get; set; }
            public double Correlation { get; set; }
        }

        private class LevelInfo
        {
            public double Value { get; set; }
            public int Count { get; set; }
            public double Strength { get; set; }
        }
    }
}
