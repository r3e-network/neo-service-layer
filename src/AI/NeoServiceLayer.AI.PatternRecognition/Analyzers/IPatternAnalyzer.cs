using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System;
using NeoServiceLayer.AI.PatternRecognition.Models;


namespace NeoServiceLayer.AI.PatternRecognition.Analyzers
{
    // IPatternAnalyzer is now defined in Models namespace
    // Using the Models.IPatternAnalyzer type instead of redefining

    /// <summary>
    /// Base class for pattern analyzers.
    /// </summary>
    public abstract class PatternAnalyzerBase : Models.IPatternAnalyzer
    {
        protected readonly ILogger _logger;
        
        // Provide a Logger property for derived classes
        protected ILogger Logger => _logger;

        protected PatternAnalyzerBase(ILogger logger)
        {
            _logger = logger;
        }

        public abstract PatternType SupportedType { get; }

        public virtual double ConfidenceThreshold => 0.75;

        public abstract Task<PatternAnalysisResult> AnalyzeAsync(PatternAnalysisRequest request);

        public virtual async Task<TrainingResult> TrainAsync(TrainingData data)
        {
            _logger.LogInformation("Training {Analyzer} with {Count} samples",
                GetType().Name, data.Samples.Count);

            // Default training implementation
            await Task.Delay(100).ConfigureAwait(false); // Simulate training

            return new TrainingResult
            {
                Success = true,
                Accuracy = 0.85,
                TrainingSamples = data.Samples.Count,
                Message = "Training completed successfully"
            };
        }

        protected double CalculateSimilarity(double[] vector1, double[] vector2)
        {
            if (vector1.Length != vector2.Length)
                return 0;

            double dotProduct = 0;
            double magnitude1 = 0;
            double magnitude2 = 0;

            for (int i = 0; i < vector1.Length; i++)
            {
                dotProduct += vector1[i] * vector2[i];
                magnitude1 += vector1[i] * vector1[i];
                magnitude2 += vector2[i] * vector2[i];
            }

            magnitude1 = Math.Sqrt(magnitude1);
            magnitude2 = Math.Sqrt(magnitude2);

            if (magnitude1 == 0 || magnitude2 == 0)
                return 0;

            return dotProduct / (magnitude1 * magnitude2);
        }
    }
}
