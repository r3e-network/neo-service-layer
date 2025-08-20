using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NeoServiceLayer.Services.Monitoring
{
    /// <summary>
    /// Interface for health check service.
    /// </summary>
    public interface IHealthCheckService
    {
        /// <summary>
        /// Performs a health check.
        /// </summary>
        Task<HealthCheckResult> CheckHealthAsync(string serviceName);

        /// <summary>
        /// Performs all health checks.
        /// </summary>
        Task<Dictionary<string, HealthCheckResult>> CheckAllHealthAsync();

        /// <summary>
        /// Registers a health check.
        /// </summary>
        Task RegisterHealthCheckAsync(string name, Func<Task<HealthCheckResult>> check);

        /// <summary>
        /// Unregisters a health check.
        /// </summary>
        Task UnregisterHealthCheckAsync(string name);

        /// <summary>
        /// Gets the overall health status.
        /// </summary>
        Task<HealthStatus> GetOverallHealthAsync();
    }

    /// <summary>
    /// Represents a health check result.
    /// </summary>
    public class HealthCheckResult
    {
        public HealthStatus Status { get; set; }
        public string? Description { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();
        public TimeSpan? ResponseTime { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Exception? Exception { get; set; }
    }

    /// <summary>
    /// Health status levels.
    /// </summary>
    public enum HealthStatus
    {
        Healthy,
        Warning,
        Degraded,
        Unhealthy,
        Unknown
    }
}