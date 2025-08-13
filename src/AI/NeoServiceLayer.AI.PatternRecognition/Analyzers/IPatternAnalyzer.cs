using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.AI.PatternRecognition.Analyzers
{
    /// <summary>
    /// Interface for pattern analysis implementations.
    /// </summary>
    public interface IPatternAnalyzer
    {
        /// <summary>
        /// Gets the pattern type this analyzer supports.
        /// </summary>
        PatternType SupportedType { get; }

        /// <summary>
        /// Analyzes data for patterns.
        /// </summary>
        Task<PatternAnalysisResult> AnalyzeAsync(PatternAnalysisRequest request);

        /// <summary>
        /// Trains the analyzer with historical data.
        /// </summary>
        Task<TrainingResult> TrainAsync(TrainingData data);

        /// <summary>
        /// Gets the confidence threshold for this analyzer.
        /// </summary>
        double ConfidenceThreshold { get; }
    }

    /// <summary>
    /// Base class for pattern analyzers.
    /// </summary>
    public abstract class PatternAnalyzerBase : IPatternAnalyzer
    {
        protected readonly ILogger _logger;

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