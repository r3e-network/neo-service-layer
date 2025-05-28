namespace NeoServiceLayer.AI.PatternRecognition.Models;

/// <summary>
/// Model metadata for AI models.
/// </summary>
internal class ModelMetadata
{
    public string ModelId { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
    public double Accuracy { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public byte[] WeightsData { get; set; } = Array.Empty<byte>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Training dataset for AI models.
/// </summary>
internal class TrainingDataset
{
    public double[][] TrainingData { get; set; } = Array.Empty<double[]>();
    public double[][] ValidationData { get; set; } = Array.Empty<double[]>();
    public double[][] TestData { get; set; } = Array.Empty<double[]>();
    public string[] FeatureNames { get; set; } = Array.Empty<string>();
    public string[] Labels { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Training result for AI models.
/// </summary>
internal class TrainingResult
{
    public byte[] ModelWeights { get; set; } = Array.Empty<byte>();
    public double Accuracy { get; set; }
    public double Loss { get; set; }
    public int Epochs { get; set; }
    public TimeSpan TrainingTime { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
}

/// <summary>
/// Inference result for AI models.
/// </summary>
internal class InferenceResult
{
    public object[] Predictions { get; set; } = Array.Empty<object>();
    public double Confidence { get; set; }
    public int ExecutionTimeMs { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Model evaluation result.
/// </summary>
internal class ModelEvaluationResult
{
    public double Accuracy { get; set; }
    public double Precision { get; set; }
    public double Recall { get; set; }
    public double F1Score { get; set; }
    public double[][] ConfusionMatrix { get; set; } = Array.Empty<double[]>();
    public Dictionary<string, object> Metrics { get; set; } = new();
}

/// <summary>
/// Frequency pattern for fraud detection.
/// </summary>
internal class FrequencyPattern
{
    public int MinTransactions { get; set; }
    public TimeSpan TimeWindow { get; set; }
    public bool IsBurstPattern { get; set; }
}

/// <summary>
/// Mixing service pattern definition.
/// </summary>
internal class MixingServicePattern
{
    public string PatternId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string[]? KnownAddresses { get; set; }
    public decimal[]? TypicalAmounts { get; set; }
    public TimeSpan[]? TypicalDelays { get; set; }
    public string[]? Characteristics { get; set; }
    public double RiskLevel { get; set; }
}