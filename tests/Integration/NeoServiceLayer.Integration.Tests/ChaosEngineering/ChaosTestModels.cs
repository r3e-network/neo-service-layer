using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.Integration.Tests.ChaosEngineering
{
    #region Core Chaos Test Models

    public class ChaosTestScenario
    {
        public string Name { get; set; } = string.Empty;
        public List<string> TargetServices { get; set; } = new();
        public List<FailureDefinition> Failures { get; set; } = new();
        public TimeSpan MonitoringDuration { get; set; }
        public ExpectedBehavior ExpectedBehavior { get; set; } = new();
        public List<ValidationRule> ValidationRules { get; set; } = new();
        public SuccessCriteria SuccessCriteria { get; set; } = new();
    }

    public class ChaosTestResult
    {
        public string TestId { get; set; } = string.Empty;
        public string ScenarioName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public BaselineMetrics BaselineMetrics { get; set; } = new();
        public List<InjectedFailure> FailuresInjected { get; set; } = new();
        public Dictionary<string, ServiceBehavior> ServiceBehaviors { get; set; } = new();
        public RecoveryMetrics RecoveryMetrics { get; set; } = new();
        public SystemValidation SystemValidation { get; set; } = new();
    }

    public class FailureDefinition
    {
        public string Type { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public Dictionary<string, object> Configuration { get; set; } = new();
        public TimeSpan? Duration { get; set; }
        public double Severity { get; set; } = 1.0;
    }

    public class InjectedFailure
    {
        public string FailureId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public DateTime InjectedAt { get; set; }
        public DateTime? RemovedAt { get; set; }
        public bool Active { get; set; }
        public Dictionary<string, object> Configuration { get; set; } = new();
    }

    #endregion

    #region Metrics and Behavior Models

    public class BaselineMetrics
    {
        public DateTime MeasuredAt { get; set; }
        public Dictionary<string, ServiceMetrics> ServiceMetrics { get; set; } = new();
    }

    public class ServiceMetrics
    {
        public TimeSpan ResponseTime { get; set; }
        public int Throughput { get; set; }
        public double ErrorRate { get; set; }
        public double CpuUsage { get; set; }
        public double MemoryUsageMB { get; set; }
        public Dictionary<string, double> CustomMetrics { get; set; } = new();
    }

    public class ServiceBehavior
    {
        public string ServiceName { get; set; } = string.Empty;
        public DateTime ObservedAt { get; set; }
        public bool IsResponding { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public int ErrorCount { get; set; }
        public int SuccessCount { get; set; }
        public string CircuitBreakerState { get; set; } = string.Empty;
        public double ResilienceScore { get; set; }
        public List<string> ObservedErrors { get; set; } = new();

        public void AggregateWith(ServiceBehavior other)
        {
            ErrorCount += other.ErrorCount;
            SuccessCount += other.SuccessCount;
            ResponseTime = TimeSpan.FromMilliseconds(
                (ResponseTime.TotalMilliseconds + other.ResponseTime.TotalMilliseconds) / 2);
            ObservedErrors.AddRange(other.ObservedErrors);
        }
    }

    public class RecoveryMetrics
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan TotalRecoveryTime { get; set; }
        public Dictionary<string, TimeSpan> ServiceRecoveryTimes { get; set; } = new();
        public bool FullyRecovered { get; set; }
        public List<string> UnrecoveredServices { get; set; } = new();
    }

    #endregion

    #region Network Chaos Models

    public enum NetworkChaosType
    {
        Latency,
        PacketLoss,
        Bandwidth,
        Partition,
        Corruption,
        Reordering
    }

    public class NetworkChaosResult
    {
        public string ServiceName { get; set; } = string.Empty;
        public NetworkChaosType ChaosType { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool Success { get; set; }
        public bool Active { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> Configuration { get; set; } = new();
        public Dictionary<string, double> ImpactMetrics { get; set; } = new();
    }

    public class NetworkPartitionResult
    {
        public List<string> Partition1 { get; set; } = new();
        public List<string> Partition2 { get; set; } = new();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IntraPartitionCommunicationSuccess { get; set; }
        public int CrossPartitionFailures { get; set; }
        public RecoveryMetrics RecoveryMetrics { get; set; } = new();
    }

    #endregion

    #region Service Chaos Models

    public enum ServiceChaosType
    {
        InstanceFailure,
        SlowResponse,
        ErrorResponse,
        Crash,
        Restart,
        ConfigChange
    }

    public class ServiceChaosResult
    {
        public string ServiceName { get; set; } = string.Empty;
        public ServiceChaosType ChaosType { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int InstancesAffected { get; set; }
        public bool AllowRestart { get; set; }
        public Dictionary<string, object> Impact { get; set; } = new();
    }

    #endregion

    #region Resource Chaos Models

    public enum ResourceType
    {
        CPU,
        Memory,
        Disk,
        Network,
        FileDescriptors
    }

    public class ResourceChaosResult
    {
        public string ServiceName { get; set; } = string.Empty;
        public ResourceType ResourceType { get; set; }
        public int StressLevel { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public double ActualUsage { get; set; }
        public Dictionary<string, double> PerformanceImpact { get; set; } = new();
    }

    #endregion

    #region Circuit Breaker Models

    public class CircuitBreakerTestResult
    {
        public string ServiceName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan TestDuration { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int PlannedFailures { get; set; }
        public int FailuresBeforeOpen { get; set; }
        public bool CircuitOpened { get; set; }
        public DateTime? CircuitOpenedAt { get; set; }
        public DateTime? CircuitClosedAt { get; set; }
        public TimeSpan? RecoveryTime { get; set; }
        public bool StillOpen { get; set; }
    }

    #endregion

    #region Cascading Failure Models

    public class CascadingFailureResult
    {
        public string InitialService { get; set; } = string.Empty;
        public List<string> DependentServices { get; set; } = new();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<ServiceFailurePropagation> FailurePropagation { get; set; } = new();
        public int TotalServicesAffected { get; set; }
        public bool CascadeContained { get; set; }
    }

    public class ServiceFailurePropagation
    {
        public string ServiceName { get; set; } = string.Empty;
        public DateTime FailureTime { get; set; }
        public string FailureType { get; set; } = string.Empty;
        public TimeSpan? PropagationDelay { get; set; }
        public List<string> AffectedDependencies { get; set; } = new();
    }

    #endregion

    #region Validation Models

    public class ExpectedBehavior
    {
        public double MinAvailability { get; set; } = 0.99;
        public TimeSpan MaxResponseTime { get; set; } = TimeSpan.FromSeconds(1);
        public double MaxErrorRate { get; set; } = 0.01;
        public bool ShouldDegrade { get; set; } = true;
        public bool ShouldRecover { get; set; } = true;
    }

    public class ValidationRule
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public string ExpectedResult { get; set; } = string.Empty;
    }

    public class ValidationRuleResult
    {
        public string RuleName { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? ActualValue { get; set; }
        public object? ExpectedValue { get; set; }
    }

    public class SystemValidation
    {
        public DateTime ValidationTime { get; set; }
        public List<ValidationRuleResult> RuleResults { get; set; } = new();
        public bool AllPassed { get; set; }
        public Dictionary<string, object> SystemState { get; set; } = new();
    }

    public class SuccessCriteria
    {
        public double MinResilienceScore { get; set; } = 0.7;
        public TimeSpan MaxRecoveryTime { get; set; } = TimeSpan.FromMinutes(5);
        public double MaxDataLoss { get; set; } = 0.0;
        public bool RequireFullRecovery { get; set; } = true;
    }

    #endregion

    #region Support Models

    public class ServiceHealth
    {
        public string ServiceName { get; set; } = string.Empty;
        public bool IsHealthy { get; set; }
        public DateTime CheckedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> HealthMetrics { get; set; } = new();
    }

    #endregion
}
