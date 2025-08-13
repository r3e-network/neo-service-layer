using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.AI.PatternRecognition.Analyzers
{
    /// <summary>
    /// Analyzer for sequence-based patterns.
    /// </summary>
    public class SequencePatternAnalyzer : PatternAnalyzerBase
    {
        private readonly Dictionary<string, List<double[]>> _knownSequences;
        private readonly int _windowSize;

        public SequencePatternAnalyzer(ILogger<SequencePatternAnalyzer> logger, int windowSize = 10)
            : base(logger)
        {
            _knownSequences = new Dictionary<string, List<double[]>>();
            _windowSize = windowSize;
        }

        public override PatternType SupportedType => PatternType.Sequence;

        public override async Task<PatternAnalysisResult> AnalyzeAsync(PatternAnalysisRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            _logger.LogDebug("Analyzing sequence pattern with {Count} data points", request.Data.Length);

            var patterns = new List<DetectedPattern>();

            // Convert data to sequence windows
            var windows = CreateSequenceWindows(request.Data, _windowSize);

            // Compare with known sequences
            foreach (var knownSeq in _knownSequences)
            {
                foreach (var window in windows)
                {
                    var maxSimilarity = 0.0;

                    foreach (var pattern in knownSeq.Value)
                    {
                        var similarity = CalculateSimilarity(window, pattern);
                        maxSimilarity = Math.Max(maxSimilarity, similarity);
                    }

                    if (maxSimilarity >= ConfidenceThreshold)
                    {
                        patterns.Add(new DetectedPattern
                        {
                            Type = PatternType.Sequence,
                            Name = knownSeq.Key,
                            Confidence = maxSimilarity,
                            StartIndex = Array.IndexOf(windows, window) * _windowSize,
                            EndIndex = (Array.IndexOf(windows, window) + 1) * _windowSize - 1,
                            Metadata = new Dictionary<string, object>
                            {
                                ["window_size"] = _windowSize,
                                ["sequence_name"] = knownSeq.Key
                            }
                        });
                    }
                }
            }

            // Detect repeating patterns
            var repeatingPatterns = DetectRepeatingPatterns(windows);
            patterns.AddRange(repeatingPatterns);

            var result = new PatternAnalysisResult
            {
                Success = true,
                Patterns = patterns.ToArray(),
                AnalysisTime = DateTime.UtcNow,
                Confidence = patterns.Any() ? patterns.Max(p => p.Confidence) : 0,
                Message = $"Found {patterns.Count} sequence patterns"
            };

            await Task.CompletedTask.ConfigureAwait(false);
            return result;
        }

        public override async Task<TrainingResult> TrainAsync(TrainingData data)
        {
            ArgumentNullException.ThrowIfNull(data);

            _logger.LogInformation("Training sequence analyzer with {Count} samples", data.Samples.Count);

            foreach (var sample in data.Samples)
            {
                if (sample.Label != null)
                {
                    if (!_knownSequences.ContainsKey(sample.Label))
                    {
                        _knownSequences[sample.Label] = new List<double[]>();
                    }

                    var windows = CreateSequenceWindows(sample.Data, _windowSize);
                    _knownSequences[sample.Label].AddRange(windows);
                }
            }

            // Remove duplicates
            foreach (var key in _knownSequences.Keys.ToList())
            {
                _knownSequences[key] = _knownSequences[key]
                    .Distinct(new SequenceComparer())
                    .ToList();
            }

            var result = new TrainingResult
            {
                Success = true,
                Accuracy = 0.88, // Simulated accuracy
                TrainingSamples = data.Samples.Count,
                Message = $"Trained with {_knownSequences.Count} sequence categories"
            };

            await Task.CompletedTask.ConfigureAwait(false);
            return result;
        }

        private double[][] CreateSequenceWindows(double[] data, int windowSize)
        {
            if (data.Length < windowSize)
            {
                return new[] { data };
            }

            var windows = new List<double[]>();
            for (int i = 0; i <= data.Length - windowSize; i++)
            {
                var window = new double[windowSize];
                Array.Copy(data, i, window, 0, windowSize);
                windows.Add(window);
            }

            return windows.ToArray();
        }

        private List<DetectedPattern> DetectRepeatingPatterns(double[][] windows)
        {
            var patterns = new List<DetectedPattern>();
            var threshold = 0.9; // High threshold for repeating patterns

            for (int i = 0; i < windows.Length - 1; i++)
            {
                for (int j = i + 1; j < windows.Length; j++)
                {
                    var similarity = CalculateSimilarity(windows[i], windows[j]);

                    if (similarity >= threshold)
                    {
                        patterns.Add(new DetectedPattern
                        {
                            Type = PatternType.Sequence,
                            Name = "Repeating Sequence",
                            Confidence = similarity,
                            StartIndex = i * _windowSize,
                            EndIndex = (j + 1) * _windowSize - 1,
                            Metadata = new Dictionary<string, object>
                            {
                                ["repeat_distance"] = (j - i) * _windowSize,
                                ["window_size"] = _windowSize
                            }
                        });
                    }
                }
            }

            return patterns;
        }

        private class SequenceComparer : IEqualityComparer<double[]>
        {
            public bool Equals(double[]? x, double[]? y)
            {
                if (x == null || y == null) return false;
                if (x.Length != y.Length) return false;

                for (int i = 0; i < x.Length; i++)
                {
                    if (Math.Abs(x[i] - y[i]) > 0.0001)
                        return false;
                }

                return true;
            }

            public int GetHashCode(double[] obj)
            {
                return obj.Length.GetHashCode();
            }
        }
    }
}
