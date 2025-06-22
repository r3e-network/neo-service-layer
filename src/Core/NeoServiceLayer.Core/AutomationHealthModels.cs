namespace NeoServiceLayer.Core;

// Automation Service Models
public class AutomationJobRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OwnerAddress { get; set; } = string.Empty;
    public string TargetContract { get; set; } = string.Empty;
    public string TargetMethod { get; set; } = string.Empty;
    public object[] Parameters { get; set; } = Array.Empty<object>();
    public AutomationTrigger Trigger { get; set; } = new();
    public AutomationCondition[] Conditions { get; set; } = Array.Empty<AutomationCondition>();
    public bool IsEnabled { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class AutomationJobUpdate
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public AutomationTrigger? Trigger { get; set; }
    public AutomationCondition[]? Conditions { get; set; }
    public bool? IsEnabled { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class AutomationJob
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OwnerAddress { get; set; } = string.Empty;
    public string TargetContract { get; set; } = string.Empty;
    public string TargetMethod { get; set; } = string.Empty;
    public object[] Parameters { get; set; } = Array.Empty<object>();
    public AutomationTrigger Trigger { get; set; } = new();
    public AutomationCondition[] Conditions { get; set; } = Array.Empty<AutomationCondition>();
    public AutomationJobStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? NextExecution { get; set; }
    public DateTime? LastExecution { get; set; }
    public DateTime? LastExecuted { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
    public int ExecutionCount { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class AutomationExecution
{
    public string Id { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public bool Success { get; set; }
    public string? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public int ExecutionTimeMs { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public AutomationExecutionStatus Status { get; set; }
    public string TransactionHash { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class AutomationTrigger
{
    public AutomationTriggerType Type { get; set; }
    public string Schedule { get; set; } = string.Empty; // Cron expression for time-based
    public string EventSignature { get; set; } = string.Empty; // For event-based
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class AutomationCondition
{
    public string Type { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

// Health Service Models
public class NodeHealthReport
{
    public string NodeAddress { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public NodeStatus Status { get; set; }
    public long BlockHeight { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public double UptimePercentage { get; set; }
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    public HealthMetrics Metrics { get; set; } = new();
    public bool IsConsensusNode { get; set; }
    public int ConsensusRank { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

public class ConsensusHealthReport
{
    public int TotalConsensusNodes { get; set; }
    public int ActiveConsensusNodes { get; set; }
    public int HealthyConsensusNodes { get; set; }
    public double ConsensusEfficiency { get; set; }
    public TimeSpan AverageBlockTime { get; set; }
    public long CurrentBlockHeight { get; set; }
    public DateTime LastBlockTime { get; set; }
    public IEnumerable<NodeHealthReport> ConsensusNodes { get; set; } = Array.Empty<NodeHealthReport>();
    public Dictionary<string, object> NetworkMetrics { get; set; } = new();
}

public class NodeRegistrationRequest
{
    public string NodeAddress { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsConsensusNode { get; set; }
    public string[] MonitoringEndpoints { get; set; } = Array.Empty<string>();
    public HealthThreshold Thresholds { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class HealthAlert
{
    public string Id { get; set; } = string.Empty;
    public string NodeAddress { get; set; } = string.Empty;
    public HealthAlertSeverity Severity { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public bool IsResolved { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
}

public class HealthThreshold
{
    public TimeSpan MaxResponseTime { get; set; } = TimeSpan.FromSeconds(5);
    public double MinUptimePercentage { get; set; } = 95.0;
    public int MaxBlocksBehind { get; set; } = 10;
    public TimeSpan MaxTimeSinceLastSeen { get; set; } = TimeSpan.FromMinutes(5);
    public Dictionary<string, double> CustomThresholds { get; set; } = new();
}

public class HealthMetrics
{
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public double SuccessRate { get; set; }
    public long MemoryUsage { get; set; }
    public double CpuUsage { get; set; }
    public long NetworkBytesReceived { get; set; }
    public long NetworkBytesSent { get; set; }
    public Dictionary<string, object> CustomMetrics { get; set; } = new();
}

// Automation Enums
public enum AutomationJobStatus
{
    Created,
    Active,
    Paused,
    Completed,
    Failed,
    Cancelled,
    Expired
}

public enum AutomationTriggerType
{
    Time,
    Event,
    Condition,
    TimeBased,
    EventBased,
    Conditional,
    Manual
}

public enum AutomationExecutionStatus
{
    Pending,
    Running,
    Executing,
    Completed,
    Failed,
    Cancelled
}

// Health Enums
public enum NodeStatus
{
    Unknown,
    Online,
    Offline,
    Degraded,
    Maintenance,
    Syncing
}

public enum HealthAlertSeverity
{
    Info,
    Warning,
    Error,
    Critical
}
