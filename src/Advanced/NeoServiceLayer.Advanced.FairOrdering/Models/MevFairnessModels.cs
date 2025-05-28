using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Advanced.FairOrdering.Models;

/// <summary>
/// Represents the result of fair ordering.
/// </summary>
public class FairOrderingResult
{
    /// <summary>
    /// Gets or sets the transaction ID.
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ordering position.
    /// </summary>
    public int OrderingPosition { get; set; }

    /// <summary>
    /// Gets or sets the estimated execution time.
    /// </summary>
    public DateTime EstimatedExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the fairness score.
    /// </summary>
    public double FairnessScore { get; set; }

    /// <summary>
    /// Gets or sets the MEV protection score.
    /// </summary>
    public double MevProtectionScore { get; set; }

    /// <summary>
    /// Gets or sets whether the ordering was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if unsuccessful.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the ordering timestamp.
    /// </summary>
    public DateTime OrderedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents MEV analysis request.
/// </summary>
public class MevAnalysisRequest
{
    /// <summary>
    /// Gets or sets the transaction to analyze.
    /// </summary>
    [Required]
    public PendingTransaction Transaction { get; set; } = new();

    /// <summary>
    /// Gets or sets the pool context.
    /// </summary>
    public List<PendingTransaction> PoolContext { get; set; } = new();

    /// <summary>
    /// Gets or sets the analysis depth.
    /// </summary>
    public MevAnalysisDepth Depth { get; set; } = MevAnalysisDepth.Standard;

    /// <summary>
    /// Gets or sets additional analysis parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Represents MEV protection result.
/// </summary>
public class MevProtectionResult
{
    /// <summary>
    /// Gets or sets the transaction ID.
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MEV risk score.
    /// </summary>
    public double RiskScore { get; set; }

    /// <summary>
    /// Gets or sets the protection level applied.
    /// </summary>
    public MevProtectionLevel ProtectionLevel { get; set; }

    /// <summary>
    /// Gets or sets the detected MEV opportunities.
    /// </summary>
    public List<MevOpportunity> DetectedOpportunities { get; set; } = new();

    /// <summary>
    /// Gets or sets the protection measures applied.
    /// </summary>
    public List<string> ProtectionMeasures { get; set; } = new();

    /// <summary>
    /// Gets or sets whether protection was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the analysis timestamp.
    /// </summary>
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents fairness metrics.
/// </summary>
public class FairnessMetrics
{
    /// <summary>
    /// Gets or sets the overall fairness score.
    /// </summary>
    public double OverallFairnessScore { get; set; }

    /// <summary>
    /// Gets or sets the ordering fairness score.
    /// </summary>
    public double OrderingFairness { get; set; }

    /// <summary>
    /// Gets or sets the temporal fairness score.
    /// </summary>
    public double TemporalFairness { get; set; }

    /// <summary>
    /// Gets or sets the economic fairness score.
    /// </summary>
    public double EconomicFairness { get; set; }

    /// <summary>
    /// Gets or sets the MEV protection effectiveness.
    /// </summary>
    public double MevProtectionEffectiveness { get; set; }

    /// <summary>
    /// Gets or sets the transaction throughput.
    /// </summary>
    public double TransactionThroughput { get; set; }

    /// <summary>
    /// Gets or sets the average confirmation time.
    /// </summary>
    public TimeSpan AverageConfirmationTime { get; set; }

    /// <summary>
    /// Gets or sets the measurement period.
    /// </summary>
    public TimeSpan MeasurementPeriod { get; set; }

    /// <summary>
    /// Gets or sets the measurement timestamp.
    /// </summary>
    public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents an MEV opportunity.
/// </summary>
public class MevOpportunity
{
    /// <summary>
    /// Gets or sets the opportunity ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the opportunity type.
    /// </summary>
    public MevOpportunityType Type { get; set; }

    /// <summary>
    /// Gets or sets the potential profit.
    /// </summary>
    public decimal PotentialProfit { get; set; }

    /// <summary>
    /// Gets or sets the risk level.
    /// </summary>
    public double RiskLevel { get; set; }

    /// <summary>
    /// Gets or sets the affected transactions.
    /// </summary>
    public List<string> AffectedTransactions { get; set; } = new();

    /// <summary>
    /// Gets or sets the detection confidence.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets the detection timestamp.
    /// </summary>
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional details.
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Represents a processed batch of transactions.
/// </summary>
public class ProcessedBatch
{
    /// <summary>
    /// Gets or sets the batch ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the batch number.
    /// </summary>
    public int BatchNumber { get; set; }

    /// <summary>
    /// Gets or sets the transactions in the batch.
    /// </summary>
    public List<string> TransactionIds { get; set; } = new();

    /// <summary>
    /// Gets or sets the batch processing start time.
    /// </summary>
    public DateTime ProcessingStarted { get; set; }

    /// <summary>
    /// Gets or sets the batch processing completion time.
    /// </summary>
    public DateTime ProcessingCompleted { get; set; }

    /// <summary>
    /// Gets or sets the batch fairness score.
    /// </summary>
    public double FairnessScore { get; set; }

    /// <summary>
    /// Gets or sets the MEV protection effectiveness.
    /// </summary>
    public double MevProtectionEffectiveness { get; set; }

    /// <summary>
    /// Gets or sets the batch status.
    /// </summary>
    public BatchStatus Status { get; set; }

    /// <summary>
    /// Gets or sets batch processing metrics.
    /// </summary>
    public Dictionary<string, object> Metrics { get; set; } = new();
}

/// <summary>
/// Represents batch processing statistics.
/// </summary>
public class BatchStatistics
{
    /// <summary>
    /// Gets or sets the total number of batches processed.
    /// </summary>
    public int TotalBatches { get; set; }

    /// <summary>
    /// Gets or sets the average batch size.
    /// </summary>
    public double AverageBatchSize { get; set; }

    /// <summary>
    /// Gets or sets the average processing time.
    /// </summary>
    public TimeSpan AverageProcessingTime { get; set; }

    /// <summary>
    /// Gets or sets the average fairness score.
    /// </summary>
    public double AverageFairnessScore { get; set; }

    /// <summary>
    /// Gets or sets the MEV protection rate.
    /// </summary>
    public double MevProtectionRate { get; set; }

    /// <summary>
    /// Gets or sets the transaction throughput.
    /// </summary>
    public double TransactionThroughput { get; set; }

    /// <summary>
    /// Gets or sets the statistics period.
    /// </summary>
    public TimeSpan Period { get; set; }

    /// <summary>
    /// Gets or sets the statistics generation timestamp.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
