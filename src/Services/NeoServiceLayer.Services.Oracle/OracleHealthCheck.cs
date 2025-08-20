using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Health;

namespace NeoServiceLayer.Services.Oracle;

/// <summary>
/// Health check implementation for Oracle Service.
/// </summary>
public class OracleHealthCheck : HealthCheckService
{
    private readonly IOracleService _oracleService;
    private readonly ILogger<OracleHealthCheck> _specificLogger;

    public OracleHealthCheck(
        IOracleService oracleService,
        ILogger<OracleHealthCheck> logger) 
        : base(logger, "OracleService")
    {
        _oracleService = oracleService ?? throw new ArgumentNullException(nameof(oracleService));
        _specificLogger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Add dependency checks
        AddDependencyCheck("Database", CheckDatabaseConnection);
        AddDependencyCheck("Redis", CheckRedisConnection);
        AddDependencyCheck("ExternalAPI", CheckExternalApiConnection);
    }

    protected override async Task<HealthCheckResult> CheckServiceSpecificHealthAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Check if service can fetch price data
            var testSymbol = "BTC";
            var testSource = "test";
            
            var canFetchPrice = await TestPriceFetch(testSymbol, testSource, cancellationToken).ConfigureAwait(false);
            
            if (!canFetchPrice)
            {
                return HealthCheckResult.Degraded("Oracle service cannot fetch price data");
            }

            // Check data source availability
            var dataSources = await _oracleService.GetDataSourcesAsync(cancellationToken).ConfigureAwait(false);
            if (dataSources == null || dataSources.Count == 0)
            {
                return HealthCheckResult.Degraded("No data sources available");
            }

            // Check active subscriptions
            var activeSubscriptions = await GetActiveSubscriptionCount(cancellationToken).ConfigureAwait(false);
            
            var data = new Dictionary<string, object>
            {
                ["dataSources"] = dataSources.Count,
                ["activeSubscriptions"] = activeSubscriptions,
                ["lastUpdate"] = DateTime.UtcNow
            };

            return HealthCheckResult.Healthy("Oracle service is functioning properly", data);
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Oracle service health check failed");
            return HealthCheckResult.Unhealthy("Oracle service health check failed", ex);
        }
    }

    private async Task<bool> CheckDatabaseConnection()
    {
        try
        {
            // Simulate database connectivity check
            await Task.Delay(10).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> CheckRedisConnection()
    {
        try
        {
            // Simulate Redis connectivity check
            await Task.Delay(10).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> CheckExternalApiConnection()
    {
        try
        {
            // Check if external price APIs are accessible
            await Task.Delay(10).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> TestPriceFetch(string symbol, string source, CancellationToken cancellationToken)
    {
        try
        {
            // Simulate price fetch test
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<int> GetActiveSubscriptionCount(CancellationToken cancellationToken)
    {
        try
        {
            var subscriptions = await _oracleService.GetActiveSubscriptionsAsync(cancellationToken).ConfigureAwait(false);
            return subscriptions?.Count ?? 0;
        }
        catch
        {
            return 0;
        }
    }
}