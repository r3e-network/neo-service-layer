using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Oracle;

namespace NeoServiceLayer.Services.Oracle.HealthChecks;

/// <summary>
/// Health check for Oracle service data source availability and connectivity.
/// </summary>
public class OracleDataSourceHealthCheck : IHealthCheck
{
    private readonly IOracleService _oracleService;
    private readonly ILogger<OracleDataSourceHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OracleDataSourceHealthCheck"/> class.
    /// </summary>
    /// <param name="oracleService">The Oracle service.</param>
    /// <param name="logger">The logger.</param>
    public OracleDataSourceHealthCheck(
        IOracleService oracleService,
        ILogger<OracleDataSourceHealthCheck> logger)
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
            // Check data sources for different blockchain types
            var blockchainTypes = new[] { BlockchainType.Neo, BlockchainType.Ethereum };
            var totalDataSources = 0;
            var healthyDataSources = 0;
            var dataSourceStatus = new Dictionary<string, object>();

            foreach (var blockchainType in blockchainTypes)
            {
                try
                {
                    var dataSources = await _oracleService.GetDataSourcesAsync(blockchainType);
                    var dataSourcesList = dataSources.ToList();
                    totalDataSources += dataSourcesList.Count;

                    foreach (var dataSource in dataSourcesList)
                    {
                        var sourceHealth = await CheckDataSourceHealthAsync(dataSource);
                        if (sourceHealth)
                        {
                            healthyDataSources++;
                        }

                        dataSourceStatus[$"{blockchainType}_{dataSource.Id}"] = new
                        {
                            url = dataSource.Url,
                            healthy = sourceHealth,
                            last_accessed = dataSource.LastAccessedAt,
                            access_count = dataSource.AccessCount
                        };
                    }

                    dataSourceStatus[$"{blockchainType}_count"] = dataSourcesList.Count;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to check data sources for blockchain {BlockchainType}", blockchainType);
                    dataSourceStatus[$"{blockchainType}_error"] = ex.Message;
                }
            }

            dataSourceStatus["total_data_sources"] = totalDataSources;
            dataSourceStatus["healthy_data_sources"] = healthyDataSources;
            
            if (totalDataSources == 0)
            {
                return HealthCheckResult.Degraded("No data sources configured", dataSourceStatus);
            }

            var healthPercentage = totalDataSources > 0 ? (double)healthyDataSources / totalDataSources : 0;
            
            if (healthPercentage >= 0.8)
            {
                return HealthCheckResult.Healthy(
                    $"{healthyDataSources}/{totalDataSources} data sources healthy ({healthPercentage:P0})", 
                    dataSourceStatus);
            }
            else if (healthPercentage >= 0.5)
            {
                return HealthCheckResult.Degraded(
                    $"Only {healthyDataSources}/{totalDataSources} data sources healthy ({healthPercentage:P0})", 
                    dataSourceStatus);
            }
            else
            {
                return HealthCheckResult.Unhealthy(
                    $"Majority of data sources unhealthy: {healthyDataSources}/{totalDataSources} ({healthPercentage:P0})", 
                    dataSourceStatus);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Oracle data source health check failed");
            return HealthCheckResult.Unhealthy(
                $"Oracle data source health check failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks the health of a specific data source.
    /// </summary>
    /// <param name="dataSource">The data source to check.</param>
    /// <returns>True if the data source is healthy.</returns>
    private async Task<bool> CheckDataSourceHealthAsync(DataSource dataSource)
    {
        try
        {
            // Skip health check for disabled data sources
            if (!dataSource.Enabled)
            {
                return false;
            }

            // Check if the data source URL is accessible
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            
            var response = await httpClient.GetAsync(dataSource.Url);
            
            // Consider 2xx and 3xx status codes as healthy
            return response.IsSuccessStatusCode || (int)response.StatusCode < 400;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Data source health check failed for {DataSourceUrl}", dataSource.Url);
            return false;
        }
    }
}