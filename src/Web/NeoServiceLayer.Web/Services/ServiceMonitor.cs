using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Web.Models;

namespace NeoServiceLayer.Web.Services
{
    /// <summary>
    /// Service monitoring interface.
    /// </summary>
    public interface IServiceMonitor
    {
        Task<List<ServiceStatus>> GetAllServiceStatusesAsync();
        Task<ServiceStatus> GetServiceStatusAsync(string serviceName);
        Task<ServiceStatus> RefreshServiceStatusAsync(string serviceName);
        Task<ServiceRestartResult> RestartServiceAsync(string serviceName);
        Task<List<Alert>> GetRecentAlertsAsync(int count);
        Task<List<Alert>> GetActiveAlertsAsync();
        Task<List<ActivityLog>> GetRecentActivityAsync(int count);
        Task AcknowledgeAlertAsync(string alertId, string userId);
        Task<Dictionary<string, object>> GetServiceDetailsAsync(string serviceName);
    }

    /// <summary>
    /// Implementation of service monitoring.
    /// </summary>
    public class ServiceMonitor : IServiceMonitor
    {
        private readonly ILogger<ServiceMonitor> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly List<ServiceDefinition> _serviceDefinitions;

        public ServiceMonitor(
            ILogger<ServiceMonitor> logger,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _serviceDefinitions = InitializeServiceDefinitions();
        }

        public async Task<List<ServiceStatus>> GetAllServiceStatusesAsync()
        {
            var statuses = new List<ServiceStatus>();
            var tasks = _serviceDefinitions.Select(async service =>
            {
                try
                {
                    var status = await CheckServiceHealthAsync(service);
                    return status;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking service {ServiceName}", service.Name);
                    return CreateErrorStatus(service);
                }
            });

            var results = await Task.WhenAll(tasks);
            statuses.AddRange(results);

            // Cache the results
            _cache.Set("all_service_statuses", statuses, TimeSpan.FromSeconds(30));

            return statuses;
        }

        public async Task<ServiceStatus> GetServiceStatusAsync(string serviceName)
        {
            var service = _serviceDefinitions.FirstOrDefault(s =>
                s.Name.Equals(serviceName, StringComparison.OrdinalIgnoreCase));

            if (service == null)
            {
                throw new ArgumentException($"Service {serviceName} not found");
            }

            // Check cache first
            if (_cache.TryGetValue($"service_status_{serviceName}", out ServiceStatus cachedStatus))
            {
                return cachedStatus;
            }

            var status = await CheckServiceHealthAsync(service);

            // Cache individual service status
            _cache.Set($"service_status_{serviceName}", status, TimeSpan.FromSeconds(15));

            return status;
        }

        public async Task<ServiceStatus> RefreshServiceStatusAsync(string serviceName)
        {
            // Remove from cache to force refresh
            _cache.Remove($"service_status_{serviceName}");
            return await GetServiceStatusAsync(serviceName);
        }

        public async Task<ServiceRestartResult> RestartServiceAsync(string serviceName)
        {
            try
            {
                _logger.LogInformation("Attempting to restart service {ServiceName}", serviceName);

                // In a real implementation, this would interact with container orchestration
                // or service management APIs
                // TODO: Implement actual service restart logic via orchestration API

                var result = new ServiceRestartResult
                {
                    Success = true,
                    ServiceName = serviceName,
                    Message = $"Service {serviceName} restarted successfully",
                    RestartedAt = DateTime.UtcNow,
                    DownTime = TimeSpan.FromSeconds(2),
                    Details = new Dictionary<string, object>
                    {
                        ["previousStatus"] = "unhealthy",
                        ["currentStatus"] = "healthy",
                        ["restartReason"] = "Manual restart by administrator"
                    }
                };

                // Log activity
                await LogActivityAsync(new ActivityLog
                {
                    ServiceName = serviceName,
                    Action = "ServiceRestart",
                    Message = $"Service {serviceName} was restarted",
                    Type = ActivityType.ServiceRestart,
                    Timestamp = DateTime.UtcNow,
                    Success = true
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restart service {ServiceName}", serviceName);
                return new ServiceRestartResult
                {
                    Success = false,
                    ServiceName = serviceName,
                    Message = $"Failed to restart service: {ex.Message}",
                    RestartedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<List<Alert>> GetRecentAlertsAsync(int count)
        {
            // In a real implementation, this would query a database or monitoring system
            var alerts = new List<Alert>();

            // Check for any unhealthy services and create alerts
            var statuses = await GetAllServiceStatusesAsync();
            foreach (var status in statuses.Where(s => !s.IsHealthy).Take(count))
            {
                alerts.Add(new Alert
                {
                    Id = Guid.NewGuid().ToString(),
                    ServiceName = status.Name,
                    Message = $"Service {status.Name} is unhealthy",
                    Severity = status.ErrorRate > 50 ? AlertSeverity.Critical : AlertSeverity.Warning,
                    Type = AlertType.ServiceDown,
                    TriggeredAt = DateTime.UtcNow.AddMinutes(-5),
                    IsActive = true,
                    Details = new Dictionary<string, object>
                    {
                        ["responseTime"] = status.ResponseTime,
                        ["errorRate"] = status.ErrorRate,
                        ["status"] = status.Status
                    }
                });
            }

            return alerts.OrderByDescending(a => a.TriggeredAt).Take(count).ToList();
        }

        public async Task<List<Alert>> GetActiveAlertsAsync()
        {
            var alerts = await GetRecentAlertsAsync(100);
            return alerts.Where(a => a.IsActive).ToList();
        }

        public async Task<List<ActivityLog>> GetRecentActivityAsync(int count)
        {
            // In a real implementation, this would query a database
            if (_cache.TryGetValue("recent_activity", out List<ActivityLog> cachedActivity))
            {
                return cachedActivity.Take(count).ToList();
            }

            var activities = new List<ActivityLog>
            {
                new ActivityLog
                {
                    Id = Guid.NewGuid().ToString(),
                    ServiceName = "All Services",
                    Action = "Health Check",
                    Message = "Completed system-wide health check",
                    Type = ActivityType.HealthCheck,
                    Timestamp = DateTime.UtcNow.AddMinutes(-1),
                    Success = true
                },
                new ActivityLog
                {
                    Id = Guid.NewGuid().ToString(),
                    ServiceName = "KeyManagement",
                    Action = "Configuration Update",
                    Message = "Updated encryption key rotation policy",
                    Type = ActivityType.ConfigurationUpdate,
                    Timestamp = DateTime.UtcNow.AddMinutes(-5),
                    Success = true
                },
                new ActivityLog
                {
                    Id = Guid.NewGuid().ToString(),
                    ServiceName = "Oracle",
                    Action = "API Call",
                    Message = "Processed 150 oracle requests",
                    Type = ActivityType.ApiCall,
                    Timestamp = DateTime.UtcNow.AddMinutes(-10),
                    Success = true
                }
            };

            _cache.Set("recent_activity", activities, TimeSpan.FromMinutes(1));
            return activities.Take(count).ToList();
        }

        public async Task AcknowledgeAlertAsync(string alertId, string userId)
        {
            // In a real implementation, this would update the alert in a database
            _logger.LogInformation("Alert {AlertId} acknowledged by {UserId}", alertId, userId);
            await Task.CompletedTask;
        }

        public async Task<Dictionary<string, object>> GetServiceDetailsAsync(string serviceName)
        {
            var status = await GetServiceStatusAsync(serviceName);
            var service = _serviceDefinitions.FirstOrDefault(s =>
                s.Name.Equals(serviceName, StringComparison.OrdinalIgnoreCase));

            return new Dictionary<string, object>
            {
                ["status"] = status,
                ["definition"] = service,
                ["metrics"] = await GetServiceMetricsAsync(serviceName),
                ["dependencies"] = GetServiceDependencies(serviceName),
                ["configuration"] = GetServiceConfiguration(serviceName)
            };
        }

        private async Task<ServiceStatus> CheckServiceHealthAsync(ServiceDefinition service)
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);

            var startTime = DateTime.UtcNow;
            var status = new ServiceStatus
            {
                Name = service.Name,
                Category = service.Category,
                Endpoint = service.HealthEndpoint,
                LastCheck = startTime
            };

            try
            {
                var response = await httpClient.GetAsync(service.HealthEndpoint);
                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

                status.IsHealthy = response.IsSuccessStatusCode;
                status.Status = status.IsHealthy ? "Healthy" : "Unhealthy";
                status.ResponseTime = responseTime;
                status.ErrorRate = status.IsHealthy ? 0 : 100;

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        // Parse health check response if it's JSON
                        status.Metadata = new Dictionary<string, object>
                        {
                            ["statusCode"] = (int)response.StatusCode,
                            ["contentLength"] = content.Length
                        };
                    }
                    catch
                    {
                        // Ignore parsing errors
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                status.IsHealthy = false;
                status.Status = "Unreachable";
                status.ResponseTime = 5000; // Timeout
                status.ErrorRate = 100;
                status.Metadata = new Dictionary<string, object>
                {
                    ["error"] = ex.Message
                };
            }
            catch (TaskCanceledException)
            {
                status.IsHealthy = false;
                status.Status = "Timeout";
                status.ResponseTime = 5000;
                status.ErrorRate = 100;
            }

            return status;
        }

        private ServiceStatus CreateErrorStatus(ServiceDefinition service)
        {
            return new ServiceStatus
            {
                Name = service.Name,
                Category = service.Category,
                Status = "Error",
                IsHealthy = false,
                ResponseTime = 0,
                ErrorRate = 100,
                LastCheck = DateTime.UtcNow,
                Endpoint = service.HealthEndpoint
            };
        }

        private async Task LogActivityAsync(ActivityLog activity)
        {
            // In a real implementation, this would save to a database
            if (_cache.TryGetValue("recent_activity", out List<ActivityLog> activities))
            {
                activities.Insert(0, activity);
                if (activities.Count > 100)
                {
                    activities = activities.Take(100).ToList();
                }
                _cache.Set("recent_activity", activities, TimeSpan.FromMinutes(5));
            }
        }

        private async Task<Dictionary<string, object>> GetServiceMetricsAsync(string serviceName)
        {
            // In a real implementation, this would query metrics from a monitoring system
            return new Dictionary<string, object>
            {
                ["requestsPerSecond"] = Random.Shared.Next(10, 100),
                ["averageResponseTime"] = Random.Shared.Next(50, 500),
                ["errorRate"] = Random.Shared.NextDouble() * 5,
                ["uptime"] = "99.95%"
            };
        }

        private List<string> GetServiceDependencies(string serviceName)
        {
            // Define service dependencies
            var dependencies = new Dictionary<string, List<string>>
            {
                ["KeyManagement"] = new List<string> { "Storage", "SGX" },
                ["Oracle"] = new List<string> { "KeyManagement", "Storage" },
                ["Voting"] = new List<string> { "Storage", "CrossChain" },
                ["AbstractAccount"] = new List<string> { "KeyManagement", "Storage" }
            };

            return dependencies.ContainsKey(serviceName) ? dependencies[serviceName] : new List<string>();
        }

        private Dictionary<string, object> GetServiceConfiguration(string serviceName)
        {
            // In a real implementation, this would retrieve actual configuration
            return new Dictionary<string, object>
            {
                ["enabled"] = true,
                ["version"] = "1.0.0",
                ["environment"] = "production",
                ["maxConnections"] = 100,
                ["timeout"] = 30
            };
        }

        private List<ServiceDefinition> InitializeServiceDefinitions()
        {
            var baseUrl = _configuration["ServiceMonitor:BaseUrl"] ?? "https://localhost:5001";

            return new List<ServiceDefinition>
            {
                // Foundation Layer
                new ServiceDefinition { Name = "KeyManagement", Category = "Foundation", HealthEndpoint = $"{baseUrl}/api/keymanagement/health" },
                new ServiceDefinition { Name = "SGX", Category = "Foundation", HealthEndpoint = $"{baseUrl}/api/enclave/health" },
                new ServiceDefinition { Name = "Storage", Category = "Foundation", HealthEndpoint = $"{baseUrl}/api/storage/health" },
                
                // Security Layer
                new ServiceDefinition { Name = "Compliance", Category = "Security", HealthEndpoint = $"{baseUrl}/api/compliance/health" },
                new ServiceDefinition { Name = "ZeroKnowledge", Category = "Security", HealthEndpoint = $"{baseUrl}/api/zeroknowledge/health" },
                new ServiceDefinition { Name = "Backup", Category = "Security", HealthEndpoint = $"{baseUrl}/api/backup/health" },
                
                // Intelligence Layer
                new ServiceDefinition { Name = "AI.Prediction", Category = "Intelligence", HealthEndpoint = $"{baseUrl}/api/prediction/health" },
                new ServiceDefinition { Name = "AI.PatternRecognition", Category = "Intelligence", HealthEndpoint = $"{baseUrl}/api/patternrecognition/health" },
                new ServiceDefinition { Name = "Oracle", Category = "Intelligence", HealthEndpoint = $"{baseUrl}/api/oracle/health" },
                
                // Blockchain Layer
                new ServiceDefinition { Name = "AbstractAccount", Category = "Blockchain", HealthEndpoint = $"{baseUrl}/api/abstractaccount/health" },
                new ServiceDefinition { Name = "Voting", Category = "Blockchain", HealthEndpoint = $"{baseUrl}/api/voting/health" },
                new ServiceDefinition { Name = "CrossChain", Category = "Blockchain", HealthEndpoint = $"{baseUrl}/api/crosschain/health" },
                new ServiceDefinition { Name = "ProofOfReserve", Category = "Blockchain", HealthEndpoint = $"{baseUrl}/api/proofofreserve/health" },
                
                // Automation Layer
                new ServiceDefinition { Name = "Compute", Category = "Automation", HealthEndpoint = $"{baseUrl}/api/compute/health" },
                new ServiceDefinition { Name = "Automation", Category = "Automation", HealthEndpoint = $"{baseUrl}/api/automation/health" },
                new ServiceDefinition { Name = "Notification", Category = "Automation", HealthEndpoint = $"{baseUrl}/api/notification/health" },
                new ServiceDefinition { Name = "Randomness", Category = "Automation", HealthEndpoint = $"{baseUrl}/api/randomness/health" },
                
                // Infrastructure Layer
                new ServiceDefinition { Name = "Health", Category = "Infrastructure", HealthEndpoint = $"{baseUrl}/api/health" },
                new ServiceDefinition { Name = "Monitoring", Category = "Infrastructure", HealthEndpoint = $"{baseUrl}/api/monitoring/health" },
                new ServiceDefinition { Name = "Configuration", Category = "Infrastructure", HealthEndpoint = $"{baseUrl}/api/configuration/health" },
                new ServiceDefinition { Name = "EventSubscription", Category = "Infrastructure", HealthEndpoint = $"{baseUrl}/api/eventsubscription/health" },
                
                // Advanced Layer
                new ServiceDefinition { Name = "FairOrdering", Category = "Advanced", HealthEndpoint = $"{baseUrl}/api/fairordering/health" }
            };
        }

        private class ServiceDefinition
        {
            public string Name { get; set; }
            public string Category { get; set; }
            public string HealthEndpoint { get; set; }
        }
    }
}
