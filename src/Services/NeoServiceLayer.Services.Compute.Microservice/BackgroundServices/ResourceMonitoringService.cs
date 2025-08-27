using Microsoft.Extensions.DependencyInjection;
using Neo.Compute.Service.Services;

namespace Neo.Compute.Service.BackgroundServices;

public class ResourceMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ResourceMonitoringService> _logger;
    private readonly TimeSpan _monitoringInterval = TimeSpan.FromMinutes(2);

    public ResourceMonitoringService(
        IServiceProvider serviceProvider,
        ILogger<ResourceMonitoringService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Resource Monitoring Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var resourceService = scope.ServiceProvider.GetRequiredService<IResourceAllocationService>();
                var enclaveService = scope.ServiceProvider.GetRequiredService<ISgxEnclaveService>();

                // Monitor resource utilization
                await MonitorResourceUtilizationAsync(resourceService);

                // Monitor enclave resource usage
                await MonitorEnclaveResourcesAsync(enclaveService);

                // Check for resource leaks and orphaned allocations
                await CheckResourceLeaksAsync(resourceService);

                // Generate alerts if resources are running low
                await CheckResourceAlertsAsync(resourceService);

                await Task.Delay(_monitoringInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Resource Monitoring Service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Resource Monitoring Service stopped");
    }

    private async Task MonitorResourceUtilizationAsync(IResourceAllocationService resourceService)
    {
        try
        {
            var utilization = await resourceService.GetResourceUtilizationAsync();
            
            foreach (var (resourceType, utilizationPercent) in utilization)
            {
                _logger.LogDebug("{ResourceType} utilization: {Utilization:F2}%", 
                    resourceType, utilizationPercent);

                // Log warnings for high utilization
                if (utilizationPercent > 90)
                {
                    _logger.LogWarning("HIGH UTILIZATION: {ResourceType} at {Utilization:F2}%", 
                        resourceType, utilizationPercent);
                }
                else if (utilizationPercent > 75)
                {
                    _logger.LogInformation("Moderate utilization: {ResourceType} at {Utilization:F2}%", 
                        resourceType, utilizationPercent);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring resource utilization");
        }
    }

    private async Task MonitorEnclaveResourcesAsync(ISgxEnclaveService enclaveService)
    {
        try
        {
            var enclaves = await enclaveService.GetEnclavesAsync();
            
            foreach (var enclave in enclaves)
            {
                if (enclave.Status == "Running" || enclave.Status == "Busy")
                {
                    // In a real implementation, this would query actual resource usage from the enclave
                    // For now, we'll simulate monitoring
                    
                    var resourceUsage = enclave.ResourceUsage;
                    if (resourceUsage != null)
                    {
                        _logger.LogDebug("Enclave {EnclaveId} using {MemoryMb} MB memory, running for {Duration}", 
                            enclave.Id, resourceUsage.MemoryUsedMb, resourceUsage.Duration);

                        // Check for resource limits
                        if (resourceUsage.MemoryUsedMb > 8192) // 8GB threshold
                        {
                            _logger.LogWarning("Enclave {EnclaveId} using high memory: {MemoryMb} MB", 
                                enclave.Id, resourceUsage.MemoryUsedMb);
                        }

                        // Check for long-running jobs
                        if (resourceUsage.Duration > TimeSpan.FromHours(2))
                        {
                            _logger.LogInformation("Enclave {EnclaveId} has been running for {Duration}", 
                                enclave.Id, resourceUsage.Duration);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring enclave resources");
        }
    }

    private async Task CheckResourceLeaksAsync(IResourceAllocationService resourceService)
    {
        try
        {
            var activeAllocations = await resourceService.GetActiveAllocationsAsync();
            
            // Check for allocations that have been active for too long without a job
            var suspiciousAllocations = activeAllocations.Where(a => 
                DateTime.UtcNow - a.AllocatedAt > TimeSpan.FromHours(6) && // Active for more than 6 hours
                (a.Job?.Status == Models.ComputeJobStatus.Completed || 
                 a.Job?.Status == Models.ComputeJobStatus.Failed ||
                 a.Job?.Status == Models.ComputeJobStatus.Cancelled)).ToList();

            if (suspiciousAllocations.Any())
            {
                _logger.LogWarning("Found {Count} potentially leaked resource allocations", 
                    suspiciousAllocations.Count);

                foreach (var allocation in suspiciousAllocations)
                {
                    _logger.LogWarning("Leaked allocation: {AllocationId} for job {JobId} ({ResourceType}: {Amount} {Unit})", 
                        allocation.Id, allocation.JobId, allocation.ResourceType, allocation.AllocatedAmount, allocation.Unit);

                    // Auto-release leaked resources
                    try
                    {
                        await resourceService.ReleaseResourcesAsync(allocation.JobId);
                        _logger.LogInformation("Auto-released leaked resources for job {JobId}", allocation.JobId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to auto-release leaked resources for job {JobId}", allocation.JobId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking resource leaks");
        }
    }

    private async Task CheckResourceAlertsAsync(IResourceAllocationService resourceService)
    {
        try
        {
            var utilization = await resourceService.GetResourceUtilizationAsync();
            
            foreach (var (resourceType, utilizationPercent) in utilization)
            {
                // Generate alerts based on utilization thresholds
                if (utilizationPercent > 95)
                {
                    _logger.LogCritical("CRITICAL: {ResourceType} utilization at {Utilization:F2}% - immediate action required", 
                        resourceType, utilizationPercent);
                    
                    // In a real implementation, this would trigger alerts to operations teams
                    await TriggerCriticalResourceAlert(resourceType, utilizationPercent);
                }
                else if (utilizationPercent > 85)
                {
                    _logger.LogError("ERROR: {ResourceType} utilization at {Utilization:F2}% - scaling recommended", 
                        resourceType, utilizationPercent);
                        
                    await TriggerHighResourceAlert(resourceType, utilizationPercent);
                }
                else if (utilizationPercent > 75)
                {
                    _logger.LogWarning("WARNING: {ResourceType} utilization at {Utilization:F2}% - monitor closely", 
                        resourceType, utilizationPercent);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking resource alerts");
        }
    }

    private async Task TriggerCriticalResourceAlert(string resourceType, decimal utilizationPercent)
    {
        // In a real implementation, this would:
        // 1. Send alerts to monitoring systems (Prometheus, Grafana)
        // 2. Trigger PagerDuty or similar incident management
        // 3. Send notifications to operations teams
        // 4. Potentially trigger automatic scaling if configured
        
        _logger.LogInformation("Triggering critical resource alert for {ResourceType} at {Utilization:F2}%", 
            resourceType, utilizationPercent);
        
        await Task.CompletedTask; // Placeholder
    }

    private async Task TriggerHighResourceAlert(string resourceType, decimal utilizationPercent)
    {
        // In a real implementation, this would:
        // 1. Send warnings to monitoring dashboards
        // 2. Log to metrics systems
        // 3. Send email notifications to operations teams
        // 4. Update capacity planning systems
        
        _logger.LogInformation("Triggering high resource alert for {ResourceType} at {Utilization:F2}%", 
            resourceType, utilizationPercent);
        
        await Task.CompletedTask; // Placeholder
    }
}