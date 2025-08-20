using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using System.Threading;
using System;


namespace NeoServiceLayer.Web.Controllers;

/// <summary>
/// Controller for service health check endpoints.
/// </summary>
[ApiController]
[Route("[controller]")]
[Route("api/service-health")]
public class ServiceHealthController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ServiceHealthController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceHealthController"/> class.
    /// </summary>
    public ServiceHealthController(
        IServiceProvider serviceProvider,
        ILogger<ServiceHealthController> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the overall system health status.
    /// </summary>
    /// <returns>Health status.</returns>
    [HttpGet]
    [HttpGet("status")]
    public IActionResult GetSystemHealth()
    {
        return Ok(new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            application = "Neo Service Layer",
            version = "1.0.0"
        });
    }

    /// <summary>
    /// Gets health status for all registered services.
    /// </summary>
    /// <returns>Service health statuses.</returns>
    [HttpGet("services")]
    public async Task<IActionResult> GetServicesHealth()
    {
        var serviceHealthList = new List<object>();

        // Check each registered service type
        var serviceTypes = new[]
        {
            typeof(NeoServiceLayer.Services.Statistics.IStatisticsService),
            typeof(NeoServiceLayer.Services.Health.IHealthService),
            typeof(NeoServiceLayer.Services.Storage.IStorageService),
            typeof(NeoServiceLayer.Services.KeyManagement.IKeyManagementService),
            typeof(NeoServiceLayer.Services.AbstractAccount.IAbstractAccountService),
            typeof(NeoServiceLayer.Services.Voting.IVotingService),
            typeof(NeoServiceLayer.Services.SocialRecovery.ISocialRecoveryService),
            typeof(NeoServiceLayer.Services.SmartContracts.ISmartContractsService),
            typeof(NeoServiceLayer.Services.Oracle.IOracleService),
            typeof(NeoServiceLayer.Services.Notification.INotificationService)
        };

        foreach (var serviceType in serviceTypes)
        {
            try
            {
                var service = _serviceProvider.GetService(serviceType) as IService;
                if (service != null)
                {
                    var health = await service.GetHealthAsync();
                    
                    serviceHealthList.Add(new
                    {
                        name = serviceType.Name.Replace("I", "").Replace("Service", ""),
                        type = serviceType.FullName,
                        health = health.ToString(),
                        status = "Active",
                        timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    serviceHealthList.Add(new
                    {
                        name = serviceType.Name.Replace("I", "").Replace("Service", ""),
                        type = serviceType.FullName,
                        health = "Unknown",
                        status = "NotRegistered",
                        timestamp = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking health for service type {ServiceType}", serviceType.Name);
                serviceHealthList.Add(new
                {
                    name = serviceType.Name.Replace("I", "").Replace("Service", ""),
                    type = serviceType.FullName,
                    health = "Error",
                    status = "Error",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        var healthyCount = serviceHealthList.Count(s => ((dynamic)s).health == "Healthy");
        var totalCount = serviceHealthList.Count;

        return Ok(new
        {
            summary = new
            {
                total = totalCount,
                healthy = healthyCount,
                unhealthy = totalCount - healthyCount,
                healthPercentage = totalCount > 0 ? (healthyCount * 100.0 / totalCount) : 0
            },
            services = serviceHealthList,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Simple health check endpoint.
    /// </summary>
    /// <returns>OK</returns>
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok("pong");
    }
}
