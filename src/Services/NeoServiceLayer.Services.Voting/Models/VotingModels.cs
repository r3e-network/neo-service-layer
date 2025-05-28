using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Voting.Models;

/// <summary>
/// Voting risk assessment result.
/// </summary>
public class VotingRiskAssessment
{
    /// <summary>
    /// Gets or sets the overall risk score (0-1).
    /// </summary>
    public double OverallRisk { get; set; }

    /// <summary>
    /// Gets or sets the concentration risk score (0-1).
    /// </summary>
    public double ConcentrationRisk { get; set; }

    /// <summary>
    /// Gets or sets the performance risk score (0-1).
    /// </summary>
    public double PerformanceRisk { get; set; }

    /// <summary>
    /// Gets or sets the reward risk score (0-1).
    /// </summary>
    public double RewardRisk { get; set; }

    /// <summary>
    /// Gets or sets the risk factors.
    /// </summary>
    public string[] RiskFactors { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets detailed risk metrics.
    /// </summary>
    public Dictionary<string, double> DetailedRisks { get; set; } = new();

    /// <summary>
    /// Gets or sets the risk level.
    /// </summary>
    public RiskLevel RiskLevel => OverallRisk switch
    {
        >= 0.8 => RiskLevel.Critical,
        >= 0.6 => RiskLevel.High,
        >= 0.4 => RiskLevel.Medium,
        >= 0.2 => RiskLevel.Low,
        _ => RiskLevel.Minimal
    };

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
