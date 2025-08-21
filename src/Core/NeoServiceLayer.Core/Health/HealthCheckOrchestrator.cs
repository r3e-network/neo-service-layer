using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Core.Health
{
    /// <summary>
    /// Orchestrator for executing health checks with our custom interfaces
    /// </summary>
    public class HealthCheckOrchestrator
    {
        private readonly IEnumerable<IHealthCheck> _healthChecks;
        private readonly ILogger<HealthCheckOrchestrator> _logger;

        /// <summary>
        /// Initializes a new instance of HealthCheckOrchestrator
        /// </summary>
        /// <param name="healthChecks">The health checks</param>
        /// <param name="logger">The logger</param>
        public HealthCheckOrchestrator(
            IEnumerable<IHealthCheck> healthChecks,
            ILogger<HealthCheckOrchestrator> logger)
        {
            _healthChecks = healthChecks ?? throw new ArgumentNullException(nameof(healthChecks));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Runs all health checks
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Overall health check result</returns>
        public async Task<OverallHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var checks = _healthChecks.ToList();
            var results = new Dictionary<string, HealthCheckResult>();

            _logger.LogDebug("Running {HealthCheckCount} health checks", checks.Count);

            // Run all health checks concurrently
            var tasks = checks.Select(async check =>
            {
                try
                {
                    _logger.LogDebug("Running health check: {HealthCheckName}", check.Name);
                    var result = await check.CheckHealthAsync(cancellationToken);
                    _logger.LogDebug("Health check {HealthCheckName} completed with status {Status}", 
                        check.Name, result.Status);
                    return new { Check = check, Result = result };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Health check {HealthCheckName} threw an exception", check.Name);
                    return new 
                    { 
                        Check = check, 
                        Result = HealthCheckResult.Unhealthy(
                            $"Health check {check.Name} threw an exception", 
                            ex) 
                    };
                }
            }).ToArray();

            var checkResults = await Task.WhenAll(tasks);
            
            // Collect results
            foreach (var checkResult in checkResults)
            {
                results[checkResult.Check.Name] = checkResult.Result;
            }

            stopwatch.Stop();

            // Determine overall status
            var overallStatus = DetermineOverallStatus(results.Values);
            
            var overallResult = new OverallHealthResult(
                overallStatus,
                results,
                stopwatch.Elapsed);

            _logger.LogInformation(
                "Health check completed in {Duration}ms with overall status {Status}", 
                stopwatch.Elapsed.TotalMilliseconds, 
                overallStatus);

            return overallResult;
        }

        /// <summary>
        /// Runs a specific health check by name
        /// </summary>
        /// <param name="checkName">The name of the health check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Health check result</returns>
        public async Task<HealthCheckResult?> CheckHealthAsync(string checkName, CancellationToken cancellationToken = default)
        {
            var check = _healthChecks.FirstOrDefault(hc => hc.Name.Equals(checkName, StringComparison.OrdinalIgnoreCase));
            
            if (check == null)
            {
                _logger.LogWarning("Health check {CheckName} not found", checkName);
                return null;
            }

            _logger.LogDebug("Running specific health check: {CheckName}", checkName);
            
            try
            {
                var result = await check.CheckHealthAsync(cancellationToken);
                _logger.LogDebug("Health check {CheckName} completed with status {Status}", 
                    checkName, result.Status);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check {CheckName} threw an exception", checkName);
                return HealthCheckResult.Unhealthy(
                    $"Health check {checkName} threw an exception", 
                    ex);
            }
        }

        /// <summary>
        /// Determines the overall health status from individual check results
        /// </summary>
        /// <param name="results">Individual health check results</param>
        /// <returns>Overall health status</returns>
        private static HealthStatus DetermineOverallStatus(IEnumerable<HealthCheckResult> results)
        {
            var resultsList = results.ToList();
            
            if (!resultsList.Any())
                return HealthStatus.Healthy;

            if (resultsList.Any(r => r.Status == HealthStatus.Unhealthy))
                return HealthStatus.Unhealthy;

            if (resultsList.Any(r => r.Status == HealthStatus.Degraded))
                return HealthStatus.Degraded;

            return HealthStatus.Healthy;
        }
    }

    /// <summary>
    /// Overall health check result
    /// </summary>
    public class OverallHealthResult
    {
        /// <summary>
        /// Gets the overall health status
        /// </summary>
        public HealthStatus Status { get; }

        /// <summary>
        /// Gets the individual health check results
        /// </summary>
        public IReadOnlyDictionary<string, HealthCheckResult> Results { get; }

        /// <summary>
        /// Gets the total duration of all health checks
        /// </summary>
        public TimeSpan TotalDuration { get; }

        /// <summary>
        /// Initializes a new instance of OverallHealthResult
        /// </summary>
        /// <param name="status">Overall status</param>
        /// <param name="results">Individual results</param>
        /// <param name="totalDuration">Total duration</param>
        public OverallHealthResult(
            HealthStatus status,
            IReadOnlyDictionary<string, HealthCheckResult> results,
            TimeSpan totalDuration)
        {
            Status = status;
            Results = results ?? throw new ArgumentNullException(nameof(results));
            TotalDuration = totalDuration;
        }

        /// <summary>
        /// Gets whether the overall health is good (healthy or degraded)
        /// </summary>
        public bool IsHealthy => Status != HealthStatus.Unhealthy;
    }
}