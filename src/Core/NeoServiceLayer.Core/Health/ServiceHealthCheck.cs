using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Core.Health
{
    /// <summary>
    /// Health check for service registrations and dependencies
    /// </summary>
    public class ServiceHealthCheck : IHealthCheck
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ServiceHealthCheck> _logger;
        private readonly Type[] _criticalServices;

        /// <summary>
        /// Initializes a new instance of ServiceHealthCheck
        /// </summary>
        /// <param name="serviceProvider">The service provider</param>
        /// <param name="logger">The logger</param>
        /// <param name="criticalServices">Critical services to check</param>
        public ServiceHealthCheck(
            IServiceProvider serviceProvider, 
            ILogger<ServiceHealthCheck> logger,
            params Type[] criticalServices)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _criticalServices = criticalServices ?? Array.Empty<Type>();
        }

        /// <inheritdoc />
        public string Name => "Services";

        /// <inheritdoc />
        public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var errors = new List<string>();
            var warnings = new List<string>();

            try
            {
                // Test critical service resolution
                foreach (var serviceType in _criticalServices)
                {
                    try
                    {
                        var service = _serviceProvider.GetRequiredService(serviceType);
                        if (service == null)
                        {
                            errors.Add($"Service {serviceType.Name} resolved to null");
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Failed to resolve service {serviceType.Name}: {ex.Message}");
                        _logger.LogError(ex, "Failed to resolve critical service {ServiceType}", serviceType.Name);
                    }
                }

                // Test scope creation
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    // Verify scope can be created without issues
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to create service scope: {ex.Message}");
                    _logger.LogError(ex, "Failed to create service scope");
                }

                stopwatch.Stop();
                var duration = stopwatch.Elapsed;

                if (errors.Any())
                {
                    return HealthCheckResult.Unhealthy(
                        $"Service health check failed: {string.Join(", ", errors)}",
                        data: new { Errors = errors, Warnings = warnings },
                        duration: duration);
                }

                if (warnings.Any())
                {
                    return HealthCheckResult.Degraded(
                        $"Service health check has warnings: {string.Join(", ", warnings)}",
                        data: new { Warnings = warnings },
                        duration: duration);
                }

                return HealthCheckResult.Healthy(
                    $"All services are healthy ({_criticalServices.Length} critical services checked)",
                    data: new { CriticalServicesCount = _criticalServices.Length },
                    duration: duration);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Service health check failed unexpectedly");
                
                return HealthCheckResult.Unhealthy(
                    "Service health check failed unexpectedly",
                    ex,
                    duration: stopwatch.Elapsed);
            }
        }
    }
}