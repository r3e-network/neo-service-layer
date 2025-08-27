using Microsoft.Extensions.Diagnostics.HealthChecks;
using Neo.Compute.Service.Services;

namespace Neo.Compute.Service.HealthChecks;

public class SgxHardwareHealthCheck : IHealthCheck
{
    private readonly ISgxHardwareService _sgxHardwareService;
    private readonly ILogger<SgxHardwareHealthCheck> _logger;

    public SgxHardwareHealthCheck(
        ISgxHardwareService sgxHardwareService,
        ILogger<SgxHardwareHealthCheck> logger)
    {
        _sgxHardwareService = sgxHardwareService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var healthData = new Dictionary<string, object>();

            // Check if SGX is available
            var isSgxAvailable = await _sgxHardwareService.IsSgxAvailableAsync();
            healthData["sgx_available"] = isSgxAvailable;

            if (!isSgxAvailable)
            {
                return HealthCheckResult.Degraded(
                    "SGX hardware is not available - compute service running in degraded mode",
                    data: healthData);
            }

            // Check SGX BIOS settings
            var isSgxEnabledInBios = await _sgxHardwareService.IsSgxEnabledInBiosAsync();
            healthData["sgx_bios_enabled"] = isSgxEnabledInBios;

            // Get SGX version
            var sgxVersion = await _sgxHardwareService.GetSgxVersionAsync();
            healthData["sgx_version"] = sgxVersion;

            // Check if we can create enclaves
            var canCreateEnclave = await _sgxHardwareService.CanCreateEnclaveAsync();
            healthData["can_create_enclave"] = canCreateEnclave;

            // Get resource information
            var maxEnclaves = await _sgxHardwareService.GetMaxEnclavesAsync();
            var availableEpcMemory = await _sgxHardwareService.GetAvailableEpcMemoryAsync();
            
            healthData["max_enclaves"] = maxEnclaves;
            healthData["available_epc_memory_bytes"] = availableEpcMemory;

            // Get detailed hardware info
            var hardwareInfo = await _sgxHardwareService.GetHardwareInfoAsync();
            foreach (var (key, value) in hardwareInfo)
            {
                healthData[$"hw_{key}"] = value;
            }

            // Determine overall health status
            if (!isSgxEnabledInBios)
            {
                return HealthCheckResult.Degraded(
                    "SGX is available but not enabled in BIOS",
                    data: healthData);
            }

            if (!canCreateEnclave)
            {
                return HealthCheckResult.Degraded(
                    "SGX is available but cannot create new enclaves",
                    data: healthData);
            }

            if (maxEnclaves == 0 || availableEpcMemory == 0)
            {
                return HealthCheckResult.Unhealthy(
                    "SGX hardware resources are exhausted",
                    data: healthData);
            }

            return HealthCheckResult.Healthy(
                "SGX hardware is available and ready",
                data: healthData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking SGX hardware health");
            
            return HealthCheckResult.Unhealthy(
                "Error checking SGX hardware status",
                ex,
                new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["error_type"] = ex.GetType().Name
                });
        }
    }
}