using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.Integration.Tests.ServiceHealth
{
    #region Health Check Models

    public enum HealthStatus
    {
        Healthy,
        Degraded,
        Unhealthy
    }

    public class ServiceHealthCheckResult
    {
        public string ServiceName { get; set; } = string.Empty;
        public DateTime CheckedAt { get; set; }
        public bool IsHealthy { get; set; }
        public HealthStatus Status { get; set; }
        public double HealthScore { get; set; }
        public List<HealthCheck> Checks { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public class HealthCheck
    {
        public string Name { get; set; } = string.Empty;
        public HealthStatus Status { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ServiceHealthStatus
    {
        public string ServiceName { get; set; } = string.Empty;
        public bool IsHealthy { get; set; }
        public DateTime LastChecked { get; set; }
        public double HealthScore { get; set; }
        public int ConsecutiveFailures { get; set; }
        public TimeSpan? Downtime { get; set; }
    }

    public class HealthSnapshot
    {
        public DateTime Timestamp { get; set; }
        public bool IsHealthy { get; set; }
        public double HealthScore { get; set; }
        public HealthStatus Status { get; set; }
        public Dictionary<string, double> Metrics { get; set; } = new();
    }

    #endregion

    #region Dependency Models

    public class DependencyTestResult
    {
        public string ServiceName { get; set; } = string.Empty;
        public DateTime TestedAt { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public DependencyGraph DependencyGraph { get; set; } = new();
        public List<DependencyHealth> Dependencies { get; set; } = new();
        public int HealthyDependencies { get; set; }
        public int UnhealthyDependencies { get; set; }
        public double DependencyHealthScore { get; set; }
        public List<string> CircularDependencies { get; set; } = new();
        public bool HasCircularDependencies { get; set; }
        public CascadeImpactAnalysis CascadeImpact { get; set; } = new();
    }

    public class DependencyGraph
    {
        public string ServiceName { get; set; } = string.Empty;
        public List<ServiceDependency> DirectDependencies { get; set; } = new();
        public List<ServiceDependency> TransitiveDependencies { get; set; } = new();
        public Dictionary<string, int> DependencyDepth { get; set; } = new();
    }

    public class ServiceDependency
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Required, Optional
        public string Version { get; set; } = string.Empty;
        public string Protocol { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
    }

    public class DependencyHealth
    {
        public string DependencyName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsTransitive { get; set; }
        public bool IsHealthy { get; set; }
        public string Status { get; set; } = string.Empty;
        public TimeSpan ResponseTime { get; set; }
        public DateTime TestedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class CascadeImpactAnalysis
    {
        public string ImpactLevel { get; set; } = string.Empty; // Low, Medium, High, Critical
        public int AffectedServices { get; set; }
        public TimeSpan EstimatedDowntime { get; set; }
        public string MitigationStrategy { get; set; } = string.Empty;
        public List<string> AffectedOperations { get; set; } = new();
    }

    #endregion

    #region Monitoring Models

    public class ServiceMonitoringResult
    {
        public string ServiceName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public TimeSpan CheckInterval { get; set; }
        public int TotalChecks { get; set; }
        public List<HealthSnapshot> HealthSnapshots { get; set; } = new();
        public double AverageHealthScore { get; set; }
        public double MinHealthScore { get; set; }
        public double MaxHealthScore { get; set; }
        public double Availability { get; set; }
        public bool DegradationDetected { get; set; }
        public DateTime? DegradationTime { get; set; }
        public string? ErrorMessage { get; set; }
    }

    #endregion

    #region Startup/Shutdown Models

    public class ServiceStartupTestResult
    {
        public string ServiceName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public TimeSpan ColdStartTime { get; set; }
        public TimeSpan TimeToReady { get; set; }
        public TimeSpan TotalStartupTime { get; set; }
        public List<InitializationStep> InitializationSteps { get; set; } = new();
        public ResourceAllocationResult ResourceAllocation { get; set; } = new();
        public ConfigurationLoadResult ConfigurationLoading { get; set; } = new();
        public DependencyInitResult DependencyInitialization { get; set; } = new();
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class InitializationStep
    {
        public string Name { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class ResourceAllocationResult
    {
        public int MemoryAllocated { get; set; } // MB
        public int CpuCores { get; set; }
        public int ThreadPoolSize { get; set; }
        public bool Success { get; set; }
    }

    public class ConfigurationLoadResult
    {
        public int ConfigFilesLoaded { get; set; }
        public int EnvironmentVariablesRead { get; set; }
        public int SecretsLoaded { get; set; }
        public bool Success { get; set; }
    }

    public class DependencyInitResult
    {
        public int DependenciesInitialized { get; set; }
        public int FailedDependencies { get; set; }
        public List<string> FailedDependencyNames { get; set; } = new();
        public bool Success { get; set; }
    }

    public class ServiceShutdownTestResult
    {
        public string ServiceName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public bool ShutdownInitiated { get; set; }
        public bool RequestsDrained { get; set; }
        public bool ConnectionsClosed { get; set; }
        public bool StatePersisted { get; set; }
        public bool ResourcesCleaned { get; set; }
        public TimeSpan ShutdownTime { get; set; }
        public bool GracefulShutdown { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    #endregion
}
