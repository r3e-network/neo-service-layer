using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace NeoServiceLayer.Core.Models;

/// <summary>
/// Represents an AI model in the system.
/// </summary>
public class AIModel
{
    /// <summary>
    /// Gets or sets the unique identifier for the model.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the model.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the model.
    /// </summary>
    public AIModelType Type { get; set; }

    /// <summary>
    /// Gets or sets the version of the model.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the description of the model.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model data.
    /// </summary>
    public byte[] ModelData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the model configuration.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last updated timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether the model is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the model ID (alias for Id).
    /// </summary>
    public string ModelId
    {
        get => Id;
        set => Id = value;
    }

    /// <summary>
    /// Gets or sets when the model was loaded.
    /// </summary>
    public DateTime LoadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether the model is loaded in memory.
    /// </summary>
    public bool IsLoaded { get; set; }

    /// <summary>
    /// Gets or sets the model accuracy.
    /// </summary>
    public double Accuracy { get; set; }

    /// <summary>
    /// Gets or sets the model parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets when the model was trained.
    /// </summary>
    public DateTime? TrainedAt { get; set; }

    /// <summary>
    /// Gets or sets the training metrics.
    /// </summary>
    public Dictionary<string, object>? TrainingMetrics { get; set; }

    /// <summary>
    /// Gets or sets the input features for the model.
    /// </summary>
    public List<string> InputFeatures { get; set; } = new();

    /// <summary>
    /// Gets or sets the output features for the model.
    /// </summary>
    public List<string> OutputFeatures { get; set; } = new();
}

/// <summary>
/// Represents the type of AI model.
/// </summary>
public enum AIModelType
{
    /// <summary>
    /// Prediction model.
    /// </summary>
    Prediction,

    /// <summary>
    /// Pattern recognition model.
    /// </summary>
    PatternRecognition,

    /// <summary>
    /// Classification model.
    /// </summary>
    Classification,

    /// <summary>
    /// Regression model.
    /// </summary>
    Regression,

    /// <summary>
    /// Neural network model.
    /// </summary>
    NeuralNetwork,

    /// <summary>
    /// Decision tree model.
    /// </summary>
    DecisionTree,

    /// <summary>
    /// Clustering model.
    /// </summary>
    Clustering,

    /// <summary>
    /// Natural language processing model.
    /// </summary>
    NLP,

    /// <summary>
    /// Computer vision model.
    /// </summary>
    ComputerVision
}

/// <summary>
/// Represents the definition of an AI model.
/// </summary>
public class AIModelDefinition
{
    /// <summary>
    /// Gets or sets the model ID.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Gets or sets the model name.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model type.
    /// </summary>
    public AIModelType Type { get; set; }

    /// <summary>
    /// Gets or sets the model version.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the model architecture.
    /// </summary>
    public string Architecture { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the input features.
    /// </summary>
    public List<string> InputFeatures { get; set; } = new();

    /// <summary>
    /// Gets or sets the output features.
    /// </summary>
    public List<string> OutputFeatures { get; set; } = new();

    /// <summary>
    /// Gets or sets the hyperparameters.
    /// </summary>
    public Dictionary<string, object> Hyperparameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the training configuration.
    /// </summary>
    public Dictionary<string, object> TrainingConfig { get; set; } = new();

    /// <summary>
    /// Gets or sets the model description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the training data.
    /// </summary>
    public Dictionary<string, object> TrainingData { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the model is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the algorithm used.
    /// </summary>
    public string Algorithm { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the training parameters.
    /// </summary>
    public Dictionary<string, object> TrainingParameters { get; set; } = new();
}

/// <summary>
/// Represents the result of AI inference.
/// </summary>
public class AIInferenceResult
{
    /// <summary>
    /// Gets or sets the model ID used for inference.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the inference results.
    /// </summary>
    public Dictionary<string, object> Results { get; set; } = new();

    /// <summary>
    /// Gets or sets the confidence score.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets the inference timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the processing time in milliseconds.
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// Gets or sets any metadata associated with the inference.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the inference was successful.
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Gets or sets the inference result (alias for Results).
    /// </summary>
    public object Result
    {
        get => Results;
        set => Results = value as Dictionary<string, object> ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Gets or sets when the inference was executed.
    /// </summary>
    public DateTime ExecutedAt
    {
        get => Timestamp;
        set => Timestamp = value;
    }

    /// <summary>
    /// Gets or sets the execution time in milliseconds.
    /// </summary>
    public long ExecutionTimeMs
    {
        get => ProcessingTimeMs;
        set => ProcessingTimeMs = value;
    }
}

/// <summary>
/// Represents AI model metrics.
/// </summary>
public class AIModelMetrics
{
    /// <summary>
    /// Gets or sets the model ID.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the accuracy score.
    /// </summary>
    public double Accuracy { get; set; }

    /// <summary>
    /// Gets or sets the precision score.
    /// </summary>
    public double Precision { get; set; }

    /// <summary>
    /// Gets or sets the recall score.
    /// </summary>
    public double Recall { get; set; }

    /// <summary>
    /// Gets or sets the F1 score.
    /// </summary>
    public double F1Score { get; set; }

    /// <summary>
    /// Gets or sets the loss value.
    /// </summary>
    public double Loss { get; set; }

    /// <summary>
    /// Gets or sets the training time in milliseconds.
    /// </summary>
    public long TrainingTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the inference time in milliseconds.
    /// </summary>
    public long InferenceTimeMs { get; set; }

    /// <summary>
    /// Gets or sets additional metrics.
    /// </summary>
    public Dictionary<string, double> AdditionalMetrics { get; set; } = new();

    /// <summary>
    /// Gets or sets the evaluation timestamp.
    /// </summary>
    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the test data size.
    /// </summary>
    public int TestDataSize { get; set; }

    /// <summary>
    /// Gets or sets the confusion matrix.
    /// </summary>
    public double[,] ConfusionMatrix { get; set; } = new double[0, 0];
}
