using System;
using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Core.Health
{
    /// <summary>
    /// Interface for professional health checks
    /// </summary>
    public interface IProfessionalHealthCheck
    {
        /// <summary>
        /// Gets the name of this health check
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Performs the health check
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Health check result</returns>
        Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Health check result
    /// </summary>
    public class HealthCheckResult
    {
        /// <summary>
        /// Gets the health status
        /// </summary>
        public HealthStatus Status { get; }

        /// <summary>
        /// Gets the description of the health status
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets additional data about the health check
        /// </summary>
        public object? Data { get; }

        /// <summary>
        /// Gets the exception if the health check failed
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// Gets the duration of the health check
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Initializes a new instance of HealthCheckResult
        /// </summary>
        /// <param name="status">The health status</param>
        /// <param name="description">The description</param>
        /// <param name="exception">The exception (if any)</param>
        /// <param name="data">Additional data</param>
        /// <param name="duration">Duration of the check</param>
        public HealthCheckResult(
            HealthStatus status, 
            string description, 
            Exception? exception = null, 
            object? data = null,
            TimeSpan duration = default)
        {
            Status = status;
            Description = description ?? string.Empty;
            Exception = exception;
            Data = data;
            Duration = duration;
        }

        /// <summary>
        /// Creates a healthy result
        /// </summary>
        /// <param name="description">The description</param>
        /// <param name="data">Additional data</param>
        /// <param name="duration">Duration of the check</param>
        /// <returns>Healthy result</returns>
        public static HealthCheckResult Healthy(
            string? description = null, 
            object? data = null,
            TimeSpan duration = default) => 
            new(HealthStatus.Healthy, description ?? "Healthy", null, data, duration);

        /// <summary>
        /// Creates a degraded result
        /// </summary>
        /// <param name="description">The description</param>
        /// <param name="exception">The exception</param>
        /// <param name="data">Additional data</param>
        /// <param name="duration">Duration of the check</param>
        /// <returns>Degraded result</returns>
        public static HealthCheckResult Degraded(
            string? description = null, 
            Exception? exception = null, 
            object? data = null,
            TimeSpan duration = default) => 
            new(HealthStatus.Degraded, description ?? "Degraded", exception, data, duration);

        /// <summary>
        /// Creates an unhealthy result
        /// </summary>
        /// <param name="description">The description</param>
        /// <param name="exception">The exception</param>
        /// <param name="data">Additional data</param>
        /// <param name="duration">Duration of the check</param>
        /// <returns>Unhealthy result</returns>
        public static HealthCheckResult Unhealthy(
            string? description = null, 
            Exception? exception = null, 
            object? data = null,
            TimeSpan duration = default) => 
            new(HealthStatus.Unhealthy, description ?? "Unhealthy", exception, data, duration);
    }

    /// <summary>
    /// Health status enumeration
    /// </summary>
    public enum HealthStatus
    {
        /// <summary>
        /// The component is healthy
        /// </summary>
        Healthy = 0,

        /// <summary>
        /// The component is degraded but functional
        /// </summary>
        Degraded = 1,

        /// <summary>
        /// The component is unhealthy
        /// </summary>
        Unhealthy = 2
    }
}