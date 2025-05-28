namespace NeoServiceLayer.Core;

// Prediction Service Models
public class PredictionRequest
{
    public string ModelId { get; set; } = string.Empty;
    public object[] InputData { get; set; } = Array.Empty<object>();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string DataFormat { get; set; } = "json";
    public bool ReturnConfidence { get; set; } = true;
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
}

public class PredictionResult
{
    public string RequestId { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public object[] Predictions { get; set; } = Array.Empty<object>();
    public double[] ConfidenceScores { get; set; } = Array.Empty<double>();
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public int ProcessingTimeMs { get; set; }
    public string Proof { get; set; } = string.Empty;
}

public class SentimentAnalysisRequest
{
    public string[] TextData { get; set; } = Array.Empty<string>();
    public string Language { get; set; } = "en";
    public string[] Keywords { get; set; } = Array.Empty<string>();
    public bool IncludeEmotions { get; set; } = false;
}

public class SentimentResult
{
    public string AnalysisId { get; set; } = Guid.NewGuid().ToString();
    public double OverallSentiment { get; set; }
    public double Confidence { get; set; }
    public DateTime AnalysisTime { get; set; } = DateTime.UtcNow;
    public int SampleSize { get; set; }
    public Dictionary<string, double> KeywordSentiments { get; set; } = new();
}

public class ModelRegistration
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ModelFormat { get; set; } = "onnx";
    public byte[] ModelData { get; set; } = Array.Empty<byte>();
    public string ModelHash { get; set; } = string.Empty;
    public string[] InputSchema { get; set; } = Array.Empty<string>();
    public string[] OutputSchema { get; set; } = Array.Empty<string>();
    public string Owner { get; set; } = string.Empty;
}

// Pattern Recognition Service Models
public class FraudDetectionRequest
{
    public string TransactionId { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Features { get; set; } = new();
    public string ModelId { get; set; } = string.Empty;
    public double Threshold { get; set; } = 0.8;
}

public class FraudDetectionResult
{
    public string TransactionId { get; set; } = string.Empty;
    public bool IsFraud { get; set; }
    public double FraudScore { get; set; }
    public string[] RiskFactors { get; set; } = Array.Empty<string>();
    public double Confidence { get; set; }
    public DateTime DetectionTime { get; set; } = DateTime.UtcNow;
    public string ModelId { get; set; } = string.Empty;
    public string Proof { get; set; } = string.Empty;
}

public class AnomalyDetectionRequest
{
    public double[][] Data { get; set; } = Array.Empty<double[]>();
    public string[] FeatureNames { get; set; } = Array.Empty<string>();
    public double Threshold { get; set; } = 0.95;
    public bool ReturnScores { get; set; } = true;
    public string ModelId { get; set; } = string.Empty;
}

public class AnomalyDetectionResult
{
    public string AnalysisId { get; set; } = Guid.NewGuid().ToString();
    public bool[] IsAnomaly { get; set; } = Array.Empty<bool>();
    public double[] AnomalyScores { get; set; } = Array.Empty<double>();
    public int AnomalyCount { get; set; }
    public DateTime DetectionTime { get; set; } = DateTime.UtcNow;
    public string ModelId { get; set; } = string.Empty;
    public string Proof { get; set; } = string.Empty;
}

public class ClassificationRequest
{
    public object[] InputData { get; set; } = Array.Empty<object>();
    public string[] FeatureNames { get; set; } = Array.Empty<string>();
    public string ModelId { get; set; } = string.Empty;
    public bool ReturnProbabilities { get; set; } = true;
    public string[] ExpectedClasses { get; set; } = Array.Empty<string>();
}

public class ClassificationResult
{
    public string ClassificationId { get; set; } = Guid.NewGuid().ToString();
    public string[] PredictedClasses { get; set; } = Array.Empty<string>();
    public double[] Probabilities { get; set; } = Array.Empty<double>();
    public double Confidence { get; set; }
    public DateTime ClassificationTime { get; set; } = DateTime.UtcNow;
    public string ModelId { get; set; } = string.Empty;
    public string Proof { get; set; } = string.Empty;
}
