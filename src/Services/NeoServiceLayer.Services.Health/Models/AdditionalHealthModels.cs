using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Services.Health.Models;

/// <summary>
/// Represents the health status of consensus nodes.
/// </summary>
public class ConsensusHealthReport
{
    /// <summary>
    /// Gets or sets the report ID.
    /// </summary>
    public string ReportId { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Gets or sets the total number of consensus nodes.
    /// </summary>
    public int TotalNodes { get; set; }
    
    /// <summary>
    /// Gets or sets the number of healthy nodes.
    /// </summary>
    public int HealthyNodes { get; set; }
    
    /// <summary>
    /// Gets or sets the number of unhealthy nodes.
    /// </summary>
    public int UnhealthyNodes { get; set; }
    
    /// <summary>
    /// Gets or sets the consensus participation rate.
    /// </summary>
    public decimal ParticipationRate { get; set; }
    
    /// <summary>
    /// Gets or sets the average block time.
    /// </summary>
    public double AverageBlockTime { get; set; }
    
    /// <summary>
    /// Gets or sets the last consensus round.
    /// </summary>
    public long LastConsensusRound { get; set; }
    
    /// <summary>
    /// Gets or sets when the report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets the node details.
    /// </summary>
    public List<NodeHealthInfo> NodeDetails { get; set; } = new();
}

/// <summary>
/// Represents a node registration request.
/// </summary>
public class NodeRegistrationRequest
{
    /// <summary>
    /// Gets or sets the node ID.
    /// </summary>
    public string NodeId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the node name.
    /// </summary>
    public string NodeName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the node endpoint.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the node type.
    /// </summary>
    public string NodeType { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the node version.
    /// </summary>
    public string Version { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    // Additional properties for compatibility
    /// <summary>
    /// Gets or sets the node address.
    /// </summary>
    public string NodeAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the public key.
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets whether this is a consensus node.
    /// </summary>
    public bool IsConsensusNode { get; set; }
    
    /// <summary>
    /// Gets or sets the health thresholds.
    /// </summary>
    public HealthThreshold Thresholds { get; set; } = new();
}

/// <summary>
/// Represents node health information.
/// </summary>
public class NodeHealthInfo
{
    /// <summary>
    /// Gets or sets the node ID.
    /// </summary>
    public string NodeId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the node name.
    /// </summary>
    public string NodeName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the node status.
    /// </summary>
    public NodeStatus Status { get; set; }
    
    /// <summary>
    /// Gets or sets the last ping time.
    /// </summary>
    public DateTime LastPing { get; set; }
    
    /// <summary>
    /// Gets or sets the response time in milliseconds.
    /// </summary>
    public double ResponseTime { get; set; }
}

/// <summary>
/// Node status enumeration.
/// </summary>
public enum NodeStatus
{
    /// <summary>
    /// Node is healthy and operational.
    /// </summary>
    Healthy,
    
    /// <summary>
    /// Node is degraded but operational.
    /// </summary>
    Degraded,
    
    /// <summary>
    /// Node is unhealthy.
    /// </summary>
    Unhealthy,
    
    /// <summary>
    /// Node is offline.
    /// </summary>
    Offline,
    
    /// <summary>
    /// Node status is unknown.
    /// </summary>
    Unknown,
    
    /// <summary>
    /// Node is online (alias for Healthy).
    /// </summary>
    Online = Healthy
}

/// <summary>
/// Health alert severity levels.
/// </summary>
public enum HealthAlertSeverity
{
    /// <summary>
    /// Informational alert.
    /// </summary>
    Info,
    
    /// <summary>
    /// Warning alert.
    /// </summary>
    Warning,
    
    /// <summary>
    /// Error alert.
    /// </summary>
    Error,
    
    /// <summary>
    /// Critical alert.
    /// </summary>
    Critical
}