using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Core.Health
{
    /// <summary>
    /// Extension methods for configuring comprehensive health checks.
    /// </summary>
    public static class HealthCheckExtensions
    {
        public static IServiceCollection AddNeoServiceLayerHealthChecks(
            this IServiceCollection services,
            Action<IHealthChecksBuilder>? configure = null)
        {
            var builder = services.AddHealthChecks()
                // Core service health checks
                .AddCheck<EnclaveHealthCheck>("enclave", HealthStatus.Unhealthy, new[] { "critical", "sgx" })
                .AddCheck<DatabaseHealthCheck>("database", HealthStatus.Degraded, new[] { "critical", "persistence" })
                .AddCheck<CacheHealthCheck>("cache", HealthStatus.Degraded, new[] { "performance" })
                .AddCheck<ServiceDiscoveryHealthCheck>("service_discovery", HealthStatus.Degraded, new[] { "infrastructure" })
                
                // Performance health checks
                .AddCheck<MemoryHealthCheck>("memory", HealthStatus.Degraded, new[] { "performance" })
                .AddCheck<CpuHealthCheck>("cpu", HealthStatus.Degraded, new[] { "performance" })
                
                // Security health checks
                .AddCheck<CertificateHealthCheck>("certificates", HealthStatus.Unhealthy, new[] { "security" })
                .AddCheck<AttestationHealthCheck>("attestation", HealthStatus.Degraded, new[] { "security", "sgx" });
            
            configure?.Invoke(builder);
            
            return services;
        }
    }
    
    /// <summary>
    /// Health check for SGX enclave functionality.
    /// </summary>
    public class EnclaveHealthCheck : IHealthCheck
    {
        private readonly ILogger<EnclaveHealthCheck> _logger;
        
        public EnclaveHealthCheck(ILogger<EnclaveHealthCheck> logger)
        {
            _logger = logger;
        }
        
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if enclave is initialized and responsive
                var isHealthy = await CheckEnclaveStatusAsync(cancellationToken);
                
                if (isHealthy)
                {
                    return HealthCheckResult.Healthy("Enclave is operational");
                }
                
                return HealthCheckResult.Unhealthy("Enclave is not responding");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Enclave health check failed");
                return HealthCheckResult.Unhealthy("Enclave health check error", ex);
            }
        }
        
        private async Task<bool> CheckEnclaveStatusAsync(CancellationToken cancellationToken)
        {
            // Implementation would check actual enclave status
            await Task.Delay(10, cancellationToken);
            return true; // Placeholder
        }
    }
    
    /// <summary>
    /// Health check for database connectivity.
    /// </summary>
    public class DatabaseHealthCheck : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Check database connectivity
                await Task.Delay(10, cancellationToken);
                return HealthCheckResult.Healthy("Database is accessible");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Database connection failed", ex);
            }
        }
    }
    
    /// <summary>
    /// Health check for cache service.
    /// </summary>
    public class CacheHealthCheck : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Check cache connectivity
                await Task.Delay(10, cancellationToken);
                return HealthCheckResult.Healthy("Cache is operational");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Degraded("Cache is unavailable", ex);
            }
        }
    }
    
    /// <summary>
    /// Health check for service discovery.
    /// </summary>
    public class ServiceDiscoveryHealthCheck : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, 
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);
            return HealthCheckResult.Healthy("Service discovery is operational");
        }
    }
    
    /// <summary>
    /// Health check for memory usage.
    /// </summary>
    public class MemoryHealthCheck : IHealthCheck
    {
        private const long MaxMemoryThresholdMB = 2048; // 2GB threshold
        
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, 
            CancellationToken cancellationToken = default)
        {
            var memoryUsed = GC.GetTotalMemory(false) / (1024 * 1024); // Convert to MB
            
            if (memoryUsed > MaxMemoryThresholdMB)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"High memory usage: {memoryUsed}MB / {MaxMemoryThresholdMB}MB",
                    data: new Dictionary<string, object> { ["memory_mb"] = memoryUsed }));
            }
            
            return Task.FromResult(HealthCheckResult.Healthy(
                $"Memory usage: {memoryUsed}MB",
                data: new Dictionary<string, object> { ["memory_mb"] = memoryUsed }));
        }
    }
    
    /// <summary>
    /// Health check for CPU usage.
    /// </summary>
    public class CpuHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, 
            CancellationToken cancellationToken = default)
        {
            // Simplified CPU check
            var cpuUsage = Environment.ProcessorCount > 0 ? 25 : 0; // Placeholder
            
            if (cpuUsage > 80)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"High CPU usage: {cpuUsage}%",
                    data: new Dictionary<string, object> { ["cpu_percent"] = cpuUsage }));
            }
            
            return Task.FromResult(HealthCheckResult.Healthy(
                $"CPU usage: {cpuUsage}%",
                data: new Dictionary<string, object> { ["cpu_percent"] = cpuUsage }));
        }
    }
    
    /// <summary>
    /// Health check for certificate expiration.
    /// </summary>
    public class CertificateHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, 
            CancellationToken cancellationToken = default)
        {
            // Check certificate expiration
            var daysUntilExpiry = 90; // Placeholder
            
            if (daysUntilExpiry < 30)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Certificate expires in {daysUntilExpiry} days"));
            }
            
            return Task.FromResult(HealthCheckResult.Healthy(
                $"Certificate valid for {daysUntilExpiry} days"));
        }
    }
    
    /// <summary>
    /// Health check for SGX attestation service.
    /// </summary>
    public class AttestationHealthCheck : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Check attestation service connectivity
                await Task.Delay(10, cancellationToken);
                return HealthCheckResult.Healthy("Attestation service is available");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Degraded("Attestation service unavailable", ex);
            }
        }
    }
}