using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Web.Models;
using NeoServiceLayer.Web.Services;

namespace NeoServiceLayer.Web.Hubs
{
    /// <summary>
    /// SignalR hub for real-time dashboard updates.
    /// </summary>
    [Authorize]
    public class DashboardHub : Hub
    {
        private readonly ILogger<DashboardHub> _logger;
        private readonly IServiceMonitor _serviceMonitor;
        private readonly IMetricsCollector _metricsCollector;
        private static readonly Dictionary<string, string> _userConnections = new Dictionary<string, string>();

        public DashboardHub(
            ILogger<DashboardHub> logger,
            IServiceMonitor serviceMonitor,
            IMetricsCollector metricsCollector)
        {
            _logger = logger;
            _serviceMonitor = serviceMonitor;
            _metricsCollector = metricsCollector;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier ?? Context.ConnectionId;
            _userConnections[Context.ConnectionId] = userId;

            _logger.LogInformation("User {UserId} connected to dashboard hub", userId);

            // Send initial data to the connected client
            await SendInitialData();

            // Add to dashboard group
            await Groups.AddToGroupAsync(Context.ConnectionId, "dashboard");

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (_userConnections.ContainsKey(Context.ConnectionId))
            {
                var userId = _userConnections[Context.ConnectionId];
                _userConnections.Remove(Context.ConnectionId);
                _logger.LogInformation("User {UserId} disconnected from dashboard hub", userId);
            }

            // Remove from dashboard group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "dashboard");

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Subscribe to specific service updates.
        /// </summary>
        public async Task SubscribeToService(string serviceName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"service_{serviceName}");
            _logger.LogDebug("Client {ConnectionId} subscribed to {ServiceName}", Context.ConnectionId, serviceName);

            // Send current status of the service
            var status = await _serviceMonitor.GetServiceStatusAsync(serviceName);
            await Clients.Caller.SendAsync("ServiceStatusUpdate", serviceName, status);
        }

        /// <summary>
        /// Unsubscribe from service updates.
        /// </summary>
        public async Task UnsubscribeFromService(string serviceName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"service_{serviceName}");
            _logger.LogDebug("Client {ConnectionId} unsubscribed from {ServiceName}", Context.ConnectionId, serviceName);
        }

        /// <summary>
        /// Request refresh of all service statuses.
        /// </summary>
        public async Task RefreshAllServices()
        {
            try
            {
                var statuses = await _serviceMonitor.GetAllServiceStatusesAsync();
                await Clients.Caller.SendAsync("AllServicesUpdate", statuses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing all services");
                await Clients.Caller.SendAsync("Error", "Failed to refresh services");
            }
        }

        /// <summary>
        /// Request refresh of a specific service.
        /// </summary>
        public async Task RefreshService(string serviceName)
        {
            try
            {
                var status = await _serviceMonitor.RefreshServiceStatusAsync(serviceName);

                // Notify all clients subscribed to this service
                await Clients.Group($"service_{serviceName}").SendAsync("ServiceStatusUpdate", serviceName, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing service {ServiceName}", serviceName);
                await Clients.Caller.SendAsync("Error", $"Failed to refresh service {serviceName}");
            }
        }

        /// <summary>
        /// Request current system metrics.
        /// </summary>
        public async Task GetSystemMetrics()
        {
            try
            {
                var metrics = await _metricsCollector.GetSystemMetricsAsync();
                await Clients.Caller.SendAsync("SystemMetricsUpdate", metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system metrics");
                await Clients.Caller.SendAsync("Error", "Failed to get system metrics");
            }
        }

        /// <summary>
        /// Request performance trend data.
        /// </summary>
        public async Task GetPerformanceTrend(int periodMinutes = 60)
        {
            try
            {
                var trend = await _metricsCollector.GetPerformanceTrendAsync(TimeSpan.FromMinutes(periodMinutes));
                await Clients.Caller.SendAsync("PerformanceTrendUpdate", trend);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting performance trend");
                await Clients.Caller.SendAsync("Error", "Failed to get performance trend");
            }
        }

        /// <summary>
        /// Request recent alerts.
        /// </summary>
        public async Task GetRecentAlerts(int count = 10)
        {
            try
            {
                var alerts = await _serviceMonitor.GetRecentAlertsAsync(count);
                await Clients.Caller.SendAsync("AlertsUpdate", alerts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent alerts");
                await Clients.Caller.SendAsync("Error", "Failed to get alerts");
            }
        }

        /// <summary>
        /// Acknowledge an alert.
        /// </summary>
        public async Task AcknowledgeAlert(string alertId)
        {
            try
            {
                var userId = Context.UserIdentifier ?? "Unknown";
                await _serviceMonitor.AcknowledgeAlertAsync(alertId, userId);

                // Notify all dashboard clients about the acknowledgment
                await Clients.Group("dashboard").SendAsync("AlertAcknowledged", alertId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error acknowledging alert {AlertId}", alertId);
                await Clients.Caller.SendAsync("Error", "Failed to acknowledge alert");
            }
        }

        /// <summary>
        /// Request recent activity logs.
        /// </summary>
        public async Task GetRecentActivity(int count = 20)
        {
            try
            {
                var activity = await _serviceMonitor.GetRecentActivityAsync(count);
                await Clients.Caller.SendAsync("ActivityUpdate", activity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent activity");
                await Clients.Caller.SendAsync("Error", "Failed to get activity");
            }
        }

        /// <summary>
        /// Broadcast a system-wide notification.
        /// </summary>
        public async Task BroadcastNotification(string message, string severity = "info")
        {
            // Check if user has permission to broadcast
            if (!Context.User.IsInRole("Administrator"))
            {
                await Clients.Caller.SendAsync("Error", "Insufficient permissions");
                return;
            }

            await Clients.Group("dashboard").SendAsync("SystemNotification", new
            {
                Message = message,
                Severity = severity,
                Timestamp = DateTime.UtcNow,
                User = Context.UserIdentifier
            });
        }

        private async Task SendInitialData()
        {
            try
            {
                // Send initial dashboard data to the newly connected client
                var tasks = new List<Task>
                {
                    Task.Run(async () =>
                    {
                        var statuses = await _serviceMonitor.GetAllServiceStatusesAsync();
                        await Clients.Caller.SendAsync("AllServicesUpdate", statuses);
                    }),
                    Task.Run(async () =>
                    {
                        var metrics = await _metricsCollector.GetSystemMetricsAsync();
                        await Clients.Caller.SendAsync("SystemMetricsUpdate", metrics);
                    }),
                    Task.Run(async () =>
                    {
                        var alerts = await _serviceMonitor.GetRecentAlertsAsync(5);
                        await Clients.Caller.SendAsync("AlertsUpdate", alerts);
                    }),
                    Task.Run(async () =>
                    {
                        var activity = await _serviceMonitor.GetRecentActivityAsync(10);
                        await Clients.Caller.SendAsync("ActivityUpdate", activity);
                    })
                };

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending initial data to client");
            }
        }
    }

    /// <summary>
    /// Background service for broadcasting real-time updates.
    /// </summary>
    public class DashboardUpdateService : BackgroundService
    {
        private readonly IHubContext<DashboardHub> _hubContext;
        private readonly IServiceMonitor _serviceMonitor;
        private readonly IMetricsCollector _metricsCollector;
        private readonly ILogger<DashboardUpdateService> _logger;

        public DashboardUpdateService(
            IHubContext<DashboardHub> hubContext,
            IServiceMonitor serviceMonitor,
            IMetricsCollector metricsCollector,
            ILogger<DashboardUpdateService> logger)
        {
            _hubContext = hubContext;
            _serviceMonitor = serviceMonitor;
            _metricsCollector = metricsCollector;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Broadcast system metrics every 5 seconds
                    var metrics = await _metricsCollector.GetSystemMetricsAsync();
                    await _hubContext.Clients.Group("dashboard").SendAsync("SystemMetricsUpdate", metrics, stoppingToken);

                    // Check for new alerts
                    var alerts = await _serviceMonitor.GetActiveAlertsAsync();
                    if (alerts.Any())
                    {
                        await _hubContext.Clients.Group("dashboard").SendAsync("NewAlerts", alerts, stoppingToken);
                    }

                    await Task.Delay(5000, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in dashboard update service");
                    await Task.Delay(10000, stoppingToken); // Wait longer on error
                }
            }
        }
    }
}
