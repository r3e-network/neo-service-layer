using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Services.Oracle;

namespace NeoServiceLayer.Services.Oracle.HealthChecks;

/// <summary>
/// Health check for Oracle service SGX enclave functionality.
/// </summary>
public class OracleEnclaveHealthCheck : IHealthCheck
{
    private readonly IOracleService _oracleService;
    private readonly ILogger<OracleEnclaveHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OracleEnclaveHealthCheck"/> class.
    /// </summary>
    /// <param name="oracleService">The Oracle service.</param>
    /// <param name="logger">The logger.</param>
    public OracleEnclaveHealthCheck(
        IOracleService oracleService,
        ILogger<OracleEnclaveHealthCheck> logger)
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
            // Check if service is initialized
            if (!_oracleService.IsEnclaveInitialized)
            {
                return HealthCheckResult.Degraded("Oracle enclave is not initialized - service running in fallback mode");
            }

            // Test enclave functionality with a simple operation
            try
            {
                var testRequest = new NeoServiceLayer.Services.Oracle.Models.OracleRequest
                {
                    RequestId = Guid.NewGuid().ToString(),
                    Url = "https://httpbin.org/json", // Simple test endpoint
                    Path = "slideshow.title",
                    Timestamp = DateTime.UtcNow
                };

                // Attempt to fetch data through enclave
                var response = await _oracleService.FetchDataAsync(testRequest, NeoServiceLayer.Core.BlockchainType.Neo);
                
                if (response == null)
                {
                    return HealthCheckResult.Degraded("Oracle enclave responded but returned null data");
                }

                // Verify the response has expected properties
                if (string.IsNullOrEmpty(response.RequestId) || string.IsNullOrEmpty(response.Proof))
                {
                    return HealthCheckResult.Degraded("Oracle enclave response missing required fields");
                }

                var healthData = new Dictionary<string, object>
                {
                    ["enclave_initialized"] = _oracleService.IsEnclaveInitialized,
                    ["test_request_id"] = response.RequestId,
                    ["response_timestamp"] = response.Timestamp,
                    ["has_proof"] = !string.IsNullOrEmpty(response.Proof),
                    ["has_signature"] = !string.IsNullOrEmpty(response.Signature),
                    ["blockchain_type"] = response.BlockchainType.ToString(),
                    ["block_height"] = response.BlockHeight
                };

                return HealthCheckResult.Healthy("Oracle enclave is functioning correctly", healthData);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Oracle enclave functionality test failed");
                return HealthCheckResult.Degraded(
                    $"Oracle enclave initialized but functionality test failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Oracle enclave health check failed");
            return HealthCheckResult.Unhealthy(
                $"Oracle enclave health check failed: {ex.Message}");
        }
    }
}