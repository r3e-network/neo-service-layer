using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Web.Hubs;
using NeoServiceLayer.Web.Models;
using NeoServiceLayer.Web.Services;

namespace NeoServiceLayer.Web.Pages
{
    /// <summary>
    /// Page model for the real-time monitoring dashboard with WebSocket support.
    /// </summary>
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly ILogger<DashboardModel> _logger;
        private readonly IServiceMonitor _serviceMonitor;
        private readonly IMemoryCache _cache;
        private readonly IHubContext<DashboardHub> _hubContext;
        private readonly IMetricsCollector _metricsCollector;

        public DashboardModel(
            ILogger<DashboardModel> logger,
            IServiceMonitor serviceMonitor,
            IMemoryCache cache,
            IHubContext<DashboardHub> hubContext,
            IMetricsCollector metricsCollector)
        {
            _logger = logger;
            _serviceMonitor = serviceMonitor;
            _cache = cache;
            _hubContext = hubContext;
            _metricsCollector = metricsCollector;
        }

        public DashboardViewModel Dashboard { get; set; }
        public List<ServiceStatus> ServiceStatuses { get; set; }
        public SystemMetrics SystemMetrics { get; set; }
        public List<Alert> RecentAlerts { get; set; }
        public List<ActivityLog> RecentActivity { get; set; }

        /// <summary>
        /// Handles GET requests to the dashboard page with caching.
        /// </summary>
        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Try to get cached dashboard data
                if (!_cache.TryGetValue("dashboard_data", out DashboardViewModel cachedDashboard))
                {
                    // Load fresh data
                    Dashboard = await LoadDashboardDataAsync();
                    
                    // Cache for 30 seconds
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromSeconds(30));
                    
                    _cache.Set("dashboard_data", Dashboard, cacheOptions);
                }
                else
                {
                    Dashboard = cachedDashboard;
                }

                // Load real-time data
                ServiceStatuses = await _serviceMonitor.GetAllServiceStatusesAsync();
                SystemMetrics = await _metricsCollector.GetSystemMetricsAsync();
                RecentAlerts = await _serviceMonitor.GetRecentAlertsAsync(10);
                RecentActivity = await _serviceMonitor.GetRecentActivityAsync(20);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                // Initialize with empty data
                Dashboard = new DashboardViewModel();
                ServiceStatuses = new List<ServiceStatus>();
                SystemMetrics = new SystemMetrics();
                RecentAlerts = new List<Alert>();
                RecentActivity = new List<ActivityLog>();
                return Page();
            }
        }

        /// <summary>
        /// Gets the status of a specific service.
        /// </summary>
        public async Task<IActionResult> OnGetServiceStatusAsync(string serviceName)
        {
            try
            {
                var status = await _serviceMonitor.GetServiceStatusAsync(serviceName);
                return new JsonResult(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service status for {ServiceName}", serviceName);
                return new JsonResult(new { error = "Failed to get service status" });
            }
        }

        /// <summary>
        /// Gets current system metrics.
        /// </summary>
        public async Task<IActionResult> OnGetMetricsAsync()
        {
            try
            {
                var metrics = await _metricsCollector.GetSystemMetricsAsync();
                return new JsonResult(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system metrics");
                return new JsonResult(new { error = "Failed to get metrics" });
            }
        }

        /// <summary>
        /// Exports dashboard data in various formats.
        /// </summary>
        public async Task<IActionResult> OnGetExportDataAsync(string format = "json")
        {
            try
            {
                var exportData = new
                {
                    Timestamp = DateTime.UtcNow,
                    ServiceStatuses = await _serviceMonitor.GetAllServiceStatusesAsync(),
                    SystemMetrics = await _metricsCollector.GetSystemMetricsAsync(),
                    Alerts = await _serviceMonitor.GetRecentAlertsAsync(100),
                    Activity = await _serviceMonitor.GetRecentActivityAsync(100)
                };

                switch (format.ToLower())
                {
                    case "csv":
                        return await ExportAsCsvAsync(exportData);
                    case "pdf":
                        return await ExportAsPdfAsync(exportData);
                    default:
                        return new JsonResult(exportData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting dashboard data");
                return BadRequest(new { error = "Failed to export data" });
            }
        }

        /// <summary>
        /// Refreshes the status of a specific service.
        /// </summary>
        public async Task<IActionResult> OnPostRefreshServiceAsync(string serviceName)
        {
            try
            {
                var status = await _serviceMonitor.RefreshServiceStatusAsync(serviceName);
                
                // Notify all connected clients via SignalR
                await _hubContext.Clients.All.SendAsync("ServiceStatusUpdated", serviceName, status);
                
                return new JsonResult(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing service {ServiceName}", serviceName);
                return BadRequest(new { error = "Failed to refresh service" });
            }
        }

        /// <summary>
        /// Restarts a service (admin only).
        /// </summary>
        public async Task<IActionResult> OnPostRestartServiceAsync(string serviceName)
        {
            try
            {
                // Check if user has permission to restart services
                if (!User.IsInRole("Administrator"))
                {
                    return Forbid();
                }

                var result = await _serviceMonitor.RestartServiceAsync(serviceName);
                
                // Notify all connected clients
                await _hubContext.Clients.All.SendAsync("ServiceRestarted", serviceName);
                
                return new JsonResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restarting service {ServiceName}", serviceName);
                return BadRequest(new { error = "Failed to restart service" });
            }
        }

        private async Task<DashboardViewModel> LoadDashboardDataAsync()
        {
            var dashboard = new DashboardViewModel
            {
                TotalServices = 26,
                LastUpdated = DateTime.UtcNow
            };

            try
            {
                // Get service health summary
                var statuses = await _serviceMonitor.GetAllServiceStatusesAsync();
                dashboard.HealthyServices = statuses.Count(s => s.IsHealthy);
                dashboard.UnhealthyServices = statuses.Count(s => !s.IsHealthy);
                dashboard.AverageResponseTime = statuses.Any() 
                    ? statuses.Average(s => s.ResponseTime) 
                    : 0;

                // Get system metrics
                var metrics = await _metricsCollector.GetSystemMetricsAsync();
                dashboard.RequestsPerSecond = metrics.RequestsPerSecond;
                dashboard.CpuUsage = metrics.CpuUsage;
                dashboard.MemoryUsage = metrics.MemoryUsage;
                dashboard.ActiveConnections = metrics.ActiveConnections;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error loading dashboard data, using defaults");
            }

            return dashboard;
        }

        private async Task<IActionResult> ExportAsCsvAsync(object data)
        {
            // Implementation for CSV export
            var csv = GenerateCsv(data);
            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", $"dashboard_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }

        private async Task<IActionResult> ExportAsPdfAsync(object data)
        {
            // Implementation for PDF export would require a PDF library
            // For now, return JSON
            return new JsonResult(data);
        }

        private string GenerateCsv(object data)
        {
            // Simple CSV generation
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Service,Status,ResponseTime,ErrorRate,LastCheck");
            
            // Add service data
            if (data is dynamic exportData && exportData.ServiceStatuses != null)
            {
                foreach (var service in exportData.ServiceStatuses)
                {
                    csv.AppendLine($"{service.Name},{service.Status},{service.ResponseTime},{service.ErrorRate},{service.LastCheck}");
                }
            }
            
            return csv.ToString();
        }
    }
}
