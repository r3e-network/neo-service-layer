using NeoServiceLayer.Core;

namespace NeoServiceLayer.Advanced.FairOrdering.Models;

/// <summary>
/// Represents gas pattern analysis results.
/// </summary>
public class GasAnalysisResult
{
    /// <summary>
    /// Gets or sets whether this is a high priority transaction.
    /// </summary>
    public bool IsHighPriority { get; set; }

    /// <summary>
    /// Gets or sets the estimated MEV exposure.
    /// </summary>
    public decimal EstimatedMevExposure { get; set; }

    /// <summary>
    /// Gets or sets the gas price percentile.
    /// </summary>
    public double GasPricePercentile { get; set; }

    /// <summary>
    /// Gets or sets additional analysis details.
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Represents transaction timing analysis results.
/// </summary>
public class TimingAnalysisResult
{
    /// <summary>
    /// Gets or sets whether the timing is suspicious.
    /// </summary>
    public bool IsSuspicious { get; set; }

    /// <summary>
    /// Gets or sets the timing score (0-1, higher is more suspicious).
    /// </summary>
    public double TimingScore { get; set; }

    /// <summary>
    /// Gets or sets the detected timing patterns.
    /// </summary>
    public List<string> DetectedPatterns { get; set; } = new();

    /// <summary>
    /// Gets or sets the recommended delay in milliseconds.
    /// </summary>
    public int RecommendedDelayMs { get; set; }
}

/// <summary>
/// Represents contract interaction analysis results.
/// </summary>
public class ContractAnalysisResult
{
    /// <summary>
    /// Gets or sets whether there is MEV risk.
    /// </summary>
    public bool HasMevRisk { get; set; }

    /// <summary>
    /// Gets or sets the risk level.
    /// </summary>
    public string RiskLevel { get; set; } = "Low";

    /// <summary>
    /// Gets or sets the estimated MEV.
    /// </summary>
    public decimal EstimatedMev { get; set; }

    /// <summary>
    /// Gets or sets the detected risk factors.
    /// </summary>
    public List<string> RiskFactors { get; set; } = new();

    /// <summary>
    /// Gets or sets the recommendations.
    /// </summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Gets or sets the contract type.
    /// </summary>
    public string ContractType { get; set; } = "Unknown";

    /// <summary>
    /// Gets or sets whether this is a DEX interaction.
    /// </summary>
    public bool IsDexInteraction { get; set; }

    /// <summary>
    /// Gets or sets whether this is a lending protocol interaction.
    /// </summary>
    public bool IsLendingInteraction { get; set; }

    /// <summary>
    /// Gets or sets whether this is an NFT interaction.
    /// </summary>
    public bool IsNftInteraction { get; set; }
}

/// <summary>
/// Represents a fairness risk assessment.
/// </summary>
public class FairnessRiskAssessment
{
    /// <summary>
    /// Gets or sets the overall fairness score (0-1, higher is better).
    /// </summary>
    public double FairnessScore { get; set; }

    /// <summary>
    /// Gets or sets the detected issues.
    /// </summary>
    public List<string> DetectedIssues { get; set; } = new();

    /// <summary>
    /// Gets or sets the recommendations.
    /// </summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Gets or sets the risk factors.
    /// </summary>
    public Dictionary<string, double> RiskFactors { get; set; } = new();

    /// <summary>
    /// Gets or sets the protection strategies.
    /// </summary>
    public List<string> ProtectionStrategies { get; set; } = new();
}
