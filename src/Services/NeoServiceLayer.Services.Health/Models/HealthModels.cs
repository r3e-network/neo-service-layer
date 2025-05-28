using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Health;

/// <summary>
/// Node statistics summary.
/// </summary>
public class NodeStatistics
{
    public int TotalNodes { get; set; }
    public int OnlineNodes { get; set; }
    public int OfflineNodes { get; set; }
    public int ConsensusNodes { get; set; }
    public int OnlineConsensusNodes { get; set; }
    public double AverageUptimePercentage { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public double NetworkHealthScore { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Health summary for the entire network.
/// </summary>
public class HealthSummary
{
    public NodeStatistics NodeStatistics { get; set; } = new();
    public ConsensusHealthReport ConsensusHealth { get; set; } = new();
    public HealthMetrics NetworkMetrics { get; set; } = new();
    public double NetworkHealthScore { get; set; }
    public int ActiveAlertCount { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Alert statistics summary.
/// </summary>
public class AlertStatistics
{
    public int TotalAlerts { get; set; }
    public int ActiveAlerts { get; set; }
    public int ResolvedAlerts { get; set; }
    public int CriticalAlerts { get; set; }
    public int ErrorAlerts { get; set; }
    public int WarningAlerts { get; set; }
    public DateTime LastUpdated { get; set; }
}




