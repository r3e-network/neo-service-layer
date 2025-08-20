using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace NeoServiceLayer.AI.PatternRecognition.Models
{
    /// <summary>
    /// Result of AI inference operation.
    /// </summary>
    public class AIInferenceResult
    {
        public string ModelId { get; set; } = string.Empty;
        public double[] Predictions { get; set; } = Array.Empty<double>();
        public double Confidence { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool Success { get; set; }
        public string? Error { get; set; }
        
        // Additional properties needed by Pattern Recognition service
        public object Result { get; set; } = new();
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
        public double ExecutionTimeMs { get; set; }
    }

    /// <summary>
    /// Types of AI models supported.
    /// </summary>
    public enum AIModelType
    {
        Classification,
        Regression,
        Clustering,
        AnomalyDetection,
        TimeSeries,
        NeuralNetwork,
        DeepLearning,
        ReinforcementLearning,
        DecisionTree,
        LinearRegression,
        RandomForest,
        SVM,
        Prediction
    }

    /// <summary>
    /// AI model metadata and configuration.
    /// </summary>
    public class AIModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public AIModelType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public Dictionary<string, object> Configuration { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastTrainedAt { get; set; }
        public double? Accuracy { get; set; }
        public Dictionary<string, double> Metrics { get; set; } = new();
        
        // Additional properties needed by Pattern Recognition service
        public string ModelId 
        {
            get => Id;
            set => Id = value;
        }
        public DateTime LoadedAt { get; set; } = DateTime.UtcNow;
        public bool IsLoaded { get; set; }
        public bool IsActive { get; set; } = true;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public Dictionary<string, object> OutputFeatures { get; set; } = new();
        public TimeSpan ExecutionTimeMs { get; set; }
        public List<string> InputFeatures { get; set; } = new();
    }

    /// <summary>
    /// AI model definition for training and configuration.
    /// </summary>
    public class AIModelDefinition
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public AIModelType Type { get; set; }
        public string Version { get; set; } = "1.0.0";
        public Dictionary<string, object> Parameters { get; set; } = new();
        public List<string> InputFeatures { get; set; } = new();
        public List<string> OutputFeatures { get; set; } = new();
        public byte[] TrainingData { get; set; } = Array.Empty<byte>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// AI model performance metrics.
    /// </summary>
    public class AIModelMetrics
    {
        public string ModelId { get; set; } = string.Empty;
        public double Accuracy { get; set; }
        public double Precision { get; set; }
        public double Recall { get; set; }
        public double F1Score { get; set; }
        public DateTime LastEvaluated { get; set; } = DateTime.UtcNow;
        public Dictionary<string, double> CustomMetrics { get; set; } = new();
    }

}