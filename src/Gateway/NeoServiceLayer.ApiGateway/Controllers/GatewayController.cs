using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Infrastructure.ServiceDiscovery;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NeoServiceLayer.ApiGateway.Controllers
{
    [ApiController]
    [Route("api/gateway")]
    public class GatewayController : ControllerBase
    {
        private readonly IServiceRegistry _serviceRegistry;
        private readonly ILogger<GatewayController> _logger;

        public GatewayController(IServiceRegistry serviceRegistry, ILogger<GatewayController> logger)
        {
            _serviceRegistry = serviceRegistry;
            _logger = logger;
        }

        /// <summary>
        /// Get all registered services
        /// </summary>
        [HttpGet("services")]
        public async Task<ActionResult<IEnumerable<ServiceInfo>>> GetServices()
        {
            var services = await _serviceRegistry.GetAllServicesAsync();
            return Ok(services);
        }

        /// <summary>
        /// Get services by type
        /// </summary>
        [HttpGet("services/{serviceType}")]
        public async Task<ActionResult<IEnumerable<ServiceInfo>>> GetServicesByType(string serviceType)
        {
            var services = await _serviceRegistry.DiscoverServicesAsync(serviceType);
            return Ok(services);
        }

        /// <summary>
        /// Get service health summary
        /// </summary>
        [HttpGet("health")]
        public async Task<ActionResult<object>> GetHealthSummary()
        {
            var services = await _serviceRegistry.GetAllServicesAsync();
            var summary = new
            {
                timestamp = DateTime.UtcNow,
                totalServices = services.Count(),
                healthyServices = services.Count(s => s.Status == ServiceStatus.Healthy),
                unhealthyServices = services.Count(s => s.Status == ServiceStatus.Unhealthy),
                startingServices = services.Count(s => s.Status == ServiceStatus.Starting),
                services = services.GroupBy(s => s.ServiceType).Select(g => new
                {
                    type = g.Key,
                    total = g.Count(),
                    healthy = g.Count(s => s.Status == ServiceStatus.Healthy)
                })
            };
            
            return Ok(summary);
        }

        /// <summary>
        /// Get service topology
        /// </summary>
        [HttpGet("topology")]
        public async Task<ActionResult<object>> GetServiceTopology()
        {
            var services = await _serviceRegistry.GetAllServicesAsync();
            
            // Create a simplified topology view
            var topology = new
            {
                nodes = services.Select(s => new
                {
                    id = s.ServiceId,
                    label = s.ServiceName,
                    type = s.ServiceType,
                    status = s.Status.ToString(),
                    group = GetServiceGroup(s.ServiceType)
                }),
                edges = GetServiceDependencies(services)
            };
            
            return Ok(topology);
        }

        /// <summary>
        /// Get API documentation links
        /// </summary>
        [HttpGet("docs")]
        public async Task<ActionResult<object>> GetApiDocumentation()
        {
            var services = await _serviceRegistry.GetAllServicesAsync();
            var docs = services
                .Where(s => s.Status == ServiceStatus.Healthy)
                .Select(s => new
                {
                    service = s.ServiceName,
                    swaggerUrl = $"{s.Protocol}://{s.HostName}:{s.Port}/swagger",
                    healthUrl = $"{s.Protocol}://{s.HostName}:{s.Port}/health",
                    metricsUrl = $"{s.Protocol}://{s.HostName}:{s.Port}/metrics"
                });
            
            return Ok(new
            {
                gateway = new
                {
                    swaggerUrl = "/swagger",
                    healthUrl = "/health",
                    metricsUrl = "/metrics"
                },
                services = docs
            });
        }

        private string GetServiceGroup(string serviceType)
        {
            return serviceType switch
            {
                "Notification" or "Configuration" or "Backup" or "Storage" => "Core",
                "SmartContracts" or "CrossChain" or "Oracle" or "ProofOfReserve" => "Blockchain",
                "KeyManagement" or "ZeroKnowledge" or "Compliance" or "AbstractAccount" => "Security",
                "Monitoring" or "Health" or "Automation" or "EventSubscription" => "Infrastructure",
                _ => "Other"
            };
        }

        private List<object> GetServiceDependencies(IEnumerable<ServiceInfo> services)
        {
            // This is a simplified version - in reality, you would track actual dependencies
            var dependencies = new List<object>();
            
            // All services depend on Configuration
            var configService = services.FirstOrDefault(s => s.ServiceType == "Configuration");
            if (configService != null)
            {
                foreach (var service in services.Where(s => s.ServiceType != "Configuration"))
                {
                    dependencies.Add(new
                    {
                        source = service.ServiceId,
                        target = configService.ServiceId,
                        type = "config"
                    });
                }
            }
            
            // Smart contracts depend on Key Management
            var keyService = services.FirstOrDefault(s => s.ServiceType == "KeyManagement");
            var smartContractService = services.FirstOrDefault(s => s.ServiceType == "SmartContracts");
            if (keyService != null && smartContractService != null)
            {
                dependencies.Add(new
                {
                    source = smartContractService.ServiceId,
                    target = keyService.ServiceId,
                    type = "security"
                });
            }
            
            return dependencies;
        }
    }
}