using Microsoft.Extensions.Diagnostics.HealthChecks;
using Neo.Compute.Service.Models;
using Neo.Compute.Service.Services;

namespace Neo.Compute.Service.HealthChecks;

public class EnclaveHealthCheck : IHealthCheck
{
    private readonly ISgxEnclaveService _enclaveService;
    private readonly IAttestationService _attestationService;
    private readonly ILogger<EnclaveHealthCheck> _logger;

    public EnclaveHealthCheck(
        ISgxEnclaveService enclaveService,
        IAttestationService attestationService,
        ILogger<EnclaveHealthCheck> logger)
    {
        _enclaveService = enclaveService;
        _attestationService = attestationService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var healthData = new Dictionary<string, object>();

            // Get all enclaves
            var allEnclaves = await _enclaveService.GetEnclavesAsync();
            var totalEnclaves = allEnclaves.Count;
            
            healthData["total_enclaves"] = totalEnclaves;

            if (totalEnclaves == 0)
            {
                return HealthCheckResult.Degraded(
                    "No enclaves available - compute service running without SGX capabilities",
                    data: healthData);
            }

            // Count enclaves by status
            var enclavesByStatus = allEnclaves.GroupBy(e => e.Status)
                .ToDictionary(g => g.Key.ToLower(), g => g.Count());

            foreach (var (status, count) in enclavesByStatus)
            {
                healthData[$"enclaves_{status}"] = count;
            }

            // Check ready enclaves
            var readyEnclaves = allEnclaves.Where(e => e.Status == "Ready").ToList();
            var availableEnclaves = readyEnclaves.Count;
            healthData["available_enclaves"] = availableEnclaves;

            // Check running enclaves
            var runningEnclaves = allEnclaves.Where(e => e.Status == "Running" || e.Status == "Busy").ToList();
            healthData["active_enclaves"] = runningEnclaves.Count;

            // Check for problematic enclaves
            var errorEnclaves = allEnclaves.Where(e => e.Status == "Error" || e.Status == "Crashed").ToList();
            var problemEnclaves = errorEnclaves.Count;
            healthData["problem_enclaves"] = problemEnclaves;

            // Check attestation status
            var attestedEnclaves = 0;
            var expiredAttestations = 0;

            foreach (var enclave in allEnclaves)
            {
                var enclaveId = Guid.Parse(enclave.Id);
                var isAttested = await _attestationService.IsEnclaveAttestedAsync(enclaveId);
                
                if (isAttested)
                {
                    attestedEnclaves++;
                }

                // Check if attestation is expiring soon
                var latestAttestation = await _attestationService.GetLatestAttestationAsync(enclaveId);
                if (latestAttestation != null && 
                    latestAttestation.ExpiresAt <= DateTime.UtcNow.AddHours(2))
                {
                    expiredAttestations++;
                }
            }

            healthData["attested_enclaves"] = attestedEnclaves;
            healthData["expiring_attestations"] = expiredAttestations;

            // Check enclave capacity
            var totalCapacity = allEnclaves.Sum(e => e.MaxConcurrentJobs);
            var usedCapacity = allEnclaves.Sum(e => e.ActiveJobs);
            var capacityUtilization = totalCapacity > 0 ? (double)usedCapacity / totalCapacity * 100 : 0;
            
            healthData["total_job_capacity"] = totalCapacity;
            healthData["used_job_capacity"] = usedCapacity;
            healthData["capacity_utilization_percent"] = Math.Round(capacityUtilization, 2);

            // Check for stale enclaves (no heartbeat recently)
            var staleEnclaves = allEnclaves.Where(e => 
                e.LastHeartbeat.HasValue && 
                DateTime.UtcNow - e.LastHeartbeat.Value > TimeSpan.FromMinutes(10)).Count();
            
            healthData["stale_enclaves"] = staleEnclaves;

            // Determine overall health status
            if (availableEnclaves == 0 && runningEnclaves.Count == 0)
            {
                return HealthCheckResult.Unhealthy(
                    "No enclaves are available or running",
                    data: healthData);
            }

            if (problemEnclaves > totalEnclaves / 2)
            {
                return HealthCheckResult.Unhealthy(
                    $"Majority of enclaves ({problemEnclaves}/{totalEnclaves}) are in error state",
                    data: healthData);
            }

            if (attestedEnclaves < totalEnclaves / 2)
            {
                return HealthCheckResult.Degraded(
                    $"Many enclaves ({totalEnclaves - attestedEnclaves}/{totalEnclaves}) are not properly attested",
                    data: healthData);
            }

            if (capacityUtilization > 90)
            {
                return HealthCheckResult.Degraded(
                    $"Enclave capacity highly utilized ({capacityUtilization:F1}%)",
                    data: healthData);
            }

            if (staleEnclaves > 0)
            {
                return HealthCheckResult.Degraded(
                    $"{staleEnclaves} enclaves have stale heartbeats",
                    data: healthData);
            }

            if (expiredAttestations > 0)
            {
                return HealthCheckResult.Degraded(
                    $"{expiredAttestations} enclaves have expiring attestations",
                    data: healthData);
            }

            return HealthCheckResult.Healthy(
                $"All {totalEnclaves} enclaves are healthy ({availableEnclaves} ready, {runningEnclaves.Count} active)",
                data: healthData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking enclave health");
            
            return HealthCheckResult.Unhealthy(
                "Error checking enclave status",
                ex,
                new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["error_type"] = ex.GetType().Name
                });
        }
    }
}