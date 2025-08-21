using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Services.Oracle;

namespace NeoServiceLayer.Services.Oracle.HealthChecks;

/// <summary>
/// Health check for Oracle service availability and basic functionality.
/// </summary>
public class OracleServiceHealthCheck : IHealthCheck
{
    private readonly IOracleService _oracleService;
    private readonly ILogger<OracleServiceHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OracleServiceHealthCheck"/> class.
    /// </summary>
    /// <param name="oracleService">The Oracle service.</param>
    /// <param name="logger">The logger.</param>
    public OracleServiceHealthCheck(
        IOracleService oracleService,
        ILogger<OracleServiceHealthCheck> logger)
    {
        _oracleService = oracleService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if service is running
            if (!_oracleService.IsRunning)
            {
                return HealthCheckResult.Unhealthy("Oracle service is not running");
            }

            // Check service metrics
            var metrics = _oracleService.GetMetrics();
            var lastErrorMessage = metrics.TryGetValue("LastErrorMessage", out var errorMessage) 
                ? errorMessage?.ToString() 
                : null;

            if (!string.IsNullOrEmpty(lastErrorMessage))
            {
                var lastFailureTime = metrics.TryGetValue("LastFailureTime", out var failureTime) 
                    ? (DateTime?)failureTime 
                    : null;

                // If last failure was within the last 5 minutes, consider unhealthy
                if (lastFailureTime.HasValue && DateTime.UtcNow - lastFailureTime.Value < TimeSpan.FromMinutes(5))
                {
                    return HealthCheckResult.Degraded(
                        $"Recent failure detected: {lastErrorMessage}",
                        data: metrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
                }
            }

            // Basic functionality test - get supported data sources
            try
            {
                var dataSources = await _oracleService.GetSupportedDataSourcesAsync(NeoServiceLayer.Core.BlockchainType.Neo);
                
                return HealthCheckResult.Healthy(
                    $"Oracle service is healthy with {dataSources.Count()} data sources",
                    data: metrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Oracle service basic functionality test failed");
                return HealthCheckResult.Degraded(
                    $"Oracle service running but functionality impaired: {ex.Message}",
                    data: metrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Oracle service health check failed");
            return HealthCheckResult.Unhealthy(
                $"Oracle service health check failed: {ex.Message}");
        }
    }
}