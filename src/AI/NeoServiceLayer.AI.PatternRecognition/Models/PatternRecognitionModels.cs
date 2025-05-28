using NeoServiceLayer.Core;

namespace NeoServiceLayer.AI.PatternRecognition;

/// <summary>
/// Model performance metrics.
/// </summary>
public class ModelPerformanceMetrics
{
    public string ModelId { get; set; } = string.Empty;
    public double Accuracy { get; set; }
    public double Precision { get; set; }
    public double Recall { get; set; }
    public double F1Score { get; set; }
    public DateTime LastEvaluated { get; set; }
    public int TotalPredictions { get; set; }
    public int CorrectPredictions { get; set; }
}

/// <summary>
/// Model training status.
/// </summary>
public class ModelTrainingStatus
{
    public string ModelId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LastTrained { get; set; }
    public TimeSpan TrainingDuration { get; set; }
    public double CurrentAccuracy { get; set; }
    public bool IsTraining { get; set; }
    public double TrainingProgress { get; set; }
}

/// <summary>
/// Model validation result.
/// </summary>
public class ModelValidationResult
{
    public string ModelId { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public double ValidationAccuracy { get; set; }
    public DateTime ValidationDate { get; set; }
    public Dictionary<string, double> ValidationMetrics { get; set; } = new();
    public List<string> ValidationErrors { get; set; } = new();
}

/// <summary>
/// Analysis statistics.
/// </summary>
public class AnalysisStatistics
{
    public string ModelId { get; set; } = string.Empty;
    public int TotalAnalyses { get; set; }
    public int TotalAnomalies { get; set; }
    public int AnalysesThisWeek { get; set; }
    public int AnomaliesThisWeek { get; set; }
    public double AverageConfidence { get; set; }
    public double AnomalyRate { get; set; }
    public DateTime? LastAnalysis { get; set; }
    public DateTime? LastAnomaly { get; set; }
}

/// <summary>
/// Analysis trends over time.
/// </summary>
public class AnalysisTrends
{
    public string ModelId { get; set; } = string.Empty;
    public TimeSpan TimeRange { get; set; }
    public Dictionary<DateTime, int> DailyAnalysisCounts { get; set; } = new();
    public Dictionary<DateTime, int> DailyAnomalyCounts { get; set; } = new();
    public string TrendDirection { get; set; } = string.Empty;
    public string AnomalyTrendDirection { get; set; } = string.Empty;
    public DateTime? PeakAnalysisDay { get; set; }
    public DateTime? PeakAnomalyDay { get; set; }
}

/// <summary>
/// Batch classification result.
/// </summary>
public class BatchClassificationResult
{
    public string BatchId { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public int SuccessfulClassifications { get; set; }
    public int FailedClassifications { get; set; }
    public List<Models.ClassificationResult> Results { get; set; } = new();
    public DateTime ProcessedAt { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Batch risk assessment result.
/// </summary>
public class BatchRiskAssessmentResult
{
    public string BatchId { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public int SuccessfulAssessments { get; set; }
    public int FailedAssessments { get; set; }
    public List<Models.RiskAssessmentResult> Results { get; set; } = new();
    public DateTime ProcessedAt { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Classification statistics.
/// </summary>
public class ClassificationStatistics
{
    public string ModelId { get; set; } = string.Empty;
    public TimeSpan TimeRange { get; set; }
    public int TotalClassifications { get; set; }
    public int SuccessfulClassifications { get; set; }
    public double AverageConfidence { get; set; }
    public Dictionary<string, int> ClassificationDistribution { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Risk assessment statistics.
/// </summary>
public class RiskAssessmentStatistics
{
    public TimeSpan TimeRange { get; set; }
    public int TotalAssessments { get; set; }
    public int SuccessfulAssessments { get; set; }
    public double AverageRiskScore { get; set; }
    public Dictionary<string, int> RiskLevelDistribution { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Model data structure for secure storage.
/// </summary>
public class ModelData
{
    public string ModelId { get; set; } = string.Empty;
    public string ModelType { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public Dictionary<string, double> TrainingMetrics { get; set; } = new();
}

/// <summary>
/// Historical analysis result.
/// </summary>
public class HistoricalAnalysis
{
    public string EntityId { get; set; } = string.Empty;
    public TimeSpan AnalysisPeriod { get; set; }
    public int TransactionCount { get; set; }
    public double AverageTransactionValue { get; set; }
    public int UniqueCounterparties { get; set; }
    public int SuspiciousActivityCount { get; set; }
    public double ComplianceScore { get; set; }
    public double GeographicalRisk { get; set; }
    public Dictionary<string, object> TemporalPatterns { get; set; } = new();
}
