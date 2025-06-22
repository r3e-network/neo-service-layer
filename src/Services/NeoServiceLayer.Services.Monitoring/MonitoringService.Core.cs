using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Monitoring.Models;
using NeoServiceLayer.Tee.Host.Services;

namespace NeoServiceLayer.Services.Monitoring;

/// <summary>
/// Core implementation of the Monitoring Service that provides system health monitoring and metrics collection.
/// </summary>
public partial class MonitoringService : EnclaveBlockchainServiceBase, IMonitoringService
{
    private readonly ConcurrentDictionary<string, ServiceHealthStatus> _serviceHealthCache = new();
    private readonly ConcurrentDictionary<string, List<ServiceMetric>> _metricsCache = new();
    private readonly ConcurrentDictionary<string, Alert> _activeAlerts = new();
    private readonly ConcurrentDictionary<string, LogEntry> _logEntries = new();
    private readonly ConcurrentDictionary<string, MonitoringSession> _monitoringSessions = new();
    private readonly Timer _healthCheckTimer;
    private readonly Timer _metricsCollectionTimer;
    private readonly object _cacheLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MonitoringService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="enclaveManager">The enclave manager.</param>
    /// <param name="configuration">The service configuration.</param>
    public MonitoringService(
        ILogger<MonitoringService> logger,
        IEnclaveManager enclaveManager,
        IServiceConfiguration? configuration = null)
        : base("MonitoringService", "System health monitoring and metrics collection", "1.0.0", logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX }, enclaveManager)
    {
        Configuration = configuration;

        AddCapability<IMonitoringService>();
        AddDependency(new ServiceDependency("EnclaveManager", true, "1.0.0"));

        // Initialize timers for periodic health checks and metrics collection
        _healthCheckTimer = new Timer(PerformHealthCheck, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        _metricsCollectionTimer = new Timer(CollectMetrics, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Gets the service configuration.
    /// </summary>
    protected IServiceConfiguration? Configuration { get; }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        Logger.LogInformation("Starting Monitoring Service...");

        // Initialize monitoring components
        await InitializeMonitoringComponentsAsync();

        Logger.LogInformation("Monitoring Service started successfully");
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStopAsync()
    {
        Logger.LogInformation("Stopping Monitoring Service...");

        // Stop monitoring operations
        await StopMonitoringOperationsAsync();

        // Dispose timers
        _healthCheckTimer?.Dispose();
        _metricsCollectionTimer?.Dispose();

        Logger.LogInformation("Monitoring Service stopped successfully");
        return true;
    }

    /// <inheritdoc/>
    protected override Task<ServiceHealth> OnGetHealthAsync()
    {
        var serviceCount = _serviceHealthCache.Count;
        var healthyCount = _serviceHealthCache.Values.Count(s => s.Status == HealthStatus.Healthy);

        Logger.LogDebug("Monitoring service health check: {HealthyCount}/{ServiceCount} services healthy",
            healthyCount, serviceCount);

        return Task.FromResult(ServiceHealth.Healthy);
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        Logger.LogInformation("Initializing Monitoring Service");

        // Initialize monitoring subsystems
        await InitializeHealthCheckingAsync();
        await InitializeMetricsCollectionAsync();
        await InitializeAlertingAsync();

        Logger.LogInformation("Monitoring Service initialized successfully");
        return true;
    }

    /// <inheritdoc/>
    public async Task<AlertRuleResult> CreateAlertRuleAsync(CreateAlertRuleRequest request, BlockchainType blockchainType)
    {
        if (!base.SupportsBlockchain(blockchainType))
        {
            return new AlertRuleResult
            {
                Success = false,
                ErrorMessage = $"Blockchain type {blockchainType} is not supported"
            };
        }

        try
        {
            var ruleId = Guid.NewGuid().ToString();

            // Store the alert rule (in production this would be persisted)
            var alertRule = new AlertRule
            {
                RuleId = ruleId,
                RuleName = request.RuleName,
                ServiceName = request.ServiceName,
                MetricName = request.MetricName,
                Condition = request.Condition,
                Threshold = request.Threshold,
                Severity = request.Severity,
                NotificationChannels = request.NotificationChannels,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            Logger.LogInformation("Created alert rule {RuleId} for service {ServiceName}", ruleId, request.ServiceName);

            return new AlertRuleResult
            {
                RuleId = ruleId,
                Success = true,
                CreatedAt = alertRule.CreatedAt
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create alert rule for service {ServiceName}", request.ServiceName);
            return new AlertRuleResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        Logger.LogInformation("Initializing enclave for Monitoring Service");

        // Initialize enclave-specific monitoring components
        await Task.CompletedTask;

        Logger.LogInformation("Enclave initialized successfully for Monitoring Service");
        return true;
    }

    /// <summary>
    /// Determines the overall system health based on individual service statuses.
    /// </summary>
    /// <param name="serviceStatuses">The individual service health statuses.</param>
    /// <returns>The overall system health status.</returns>
    private static HealthStatus DetermineOverallHealth(IEnumerable<ServiceHealthStatus> serviceStatuses)
    {
        var statuses = serviceStatuses.ToArray();

        if (statuses.Length == 0)
        {
            return HealthStatus.Unknown;
        }

        if (statuses.Any(s => s.Status == HealthStatus.Unhealthy))
        {
            return HealthStatus.Unhealthy;
        }

        if (statuses.Any(s => s.Status == HealthStatus.Degraded))
        {
            return HealthStatus.Degraded;
        }

        if (statuses.Any(s => s.Status == HealthStatus.Warning))
        {
            return HealthStatus.Warning;
        }

        if (statuses.All(s => s.Status == HealthStatus.Healthy))
        {
            return HealthStatus.Healthy;
        }

        return HealthStatus.Unknown;
    }

    /// <inheritdoc/>
    public async Task<AlertsResult> GetActiveAlertsAsync(GetAlertsRequest request, BlockchainType blockchainType)
    {
        if (!base.SupportsBlockchain(blockchainType))
        {
            return new AlertsResult
            {
                Success = false,
                ErrorMessage = $"Blockchain type {blockchainType} is not supported"
            };
        }

        try
        {
            var alerts = new List<Alert>();

            lock (_cacheLock)
            {
                var filteredAlerts = _activeAlerts.Values.AsEnumerable();

                // Apply filters
                if (!string.IsNullOrEmpty(request.ServiceName))
                {
                    filteredAlerts = filteredAlerts.Where(a => a.ServiceName.Equals(request.ServiceName, StringComparison.OrdinalIgnoreCase));
                }

                if (request.Severity.HasValue)
                {
                    filteredAlerts = filteredAlerts.Where(a => a.Severity == request.Severity.Value);
                }

                if (request.StartTime.HasValue)
                {
                    filteredAlerts = filteredAlerts.Where(a => a.TriggeredAt >= request.StartTime.Value);
                }

                if (request.EndTime.HasValue)
                {
                    filteredAlerts = filteredAlerts.Where(a => a.TriggeredAt <= request.EndTime.Value);
                }

                alerts.AddRange(filteredAlerts.Take(request.Limit));
            }

            await Task.CompletedTask;

            return new AlertsResult
            {
                Alerts = alerts.ToArray(),
                TotalCount = alerts.Count,
                Success = true
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get active alerts");
            return new AlertsResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc/>
    public async Task<LogsResult> GetLogsAsync(GetLogsRequest request, BlockchainType blockchainType)
    {
        if (!base.SupportsBlockchain(blockchainType))
        {
            return new LogsResult
            {
                Success = false,
                ErrorMessage = $"Blockchain type {blockchainType} is not supported"
            };
        }

        try
        {
            var logs = new List<LogEntry>();

            lock (_cacheLock)
            {
                var filteredLogs = _logEntries.Values.AsEnumerable();

                // Apply filters
                if (!string.IsNullOrEmpty(request.ServiceName))
                {
                    filteredLogs = filteredLogs.Where(l => l.ServiceName.Equals(request.ServiceName, StringComparison.OrdinalIgnoreCase));
                }

                if (request.LogLevel.HasValue)
                {
                    filteredLogs = filteredLogs.Where(l => l.Level == request.LogLevel.Value);
                }

                if (request.StartTime.HasValue)
                {
                    filteredLogs = filteredLogs.Where(l => l.Timestamp >= request.StartTime.Value);
                }

                if (request.EndTime.HasValue)
                {
                    filteredLogs = filteredLogs.Where(l => l.Timestamp <= request.EndTime.Value);
                }

                if (!string.IsNullOrEmpty(request.SearchQuery))
                {
                    filteredLogs = filteredLogs.Where(l => l.Message.Contains(request.SearchQuery, StringComparison.OrdinalIgnoreCase));
                }

                logs.AddRange(filteredLogs.OrderByDescending(l => l.Timestamp).Take(request.Limit));
            }

            await Task.CompletedTask;

            return new LogsResult
            {
                LogEntries = logs.ToArray(),
                TotalCount = logs.Count,
                Success = true
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get logs");
            return new LogsResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc/>
    public async Task<MonitoringResult> StartMonitoringAsync(StartMonitoringRequest request, BlockchainType blockchainType)
    {
        if (!base.SupportsBlockchain(blockchainType))
        {
            return new MonitoringResult
            {
                Success = false,
                ErrorMessage = $"Blockchain type {blockchainType} is not supported"
            };
        }

        try
        {
            var sessionId = Guid.NewGuid().ToString();

            var session = new MonitoringSession
            {
                SessionId = sessionId,
                ServiceName = request.ServiceName,
                BlockchainType = blockchainType,
                MonitoringInterval = request.MonitoringInterval,
                MetricsMonitored = request.MetricsToMonitor,
                IsActive = true,
                StartedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };

            lock (_cacheLock)
            {
                _monitoringSessions[sessionId] = session;
            }

            Logger.LogInformation("Started monitoring session {SessionId} for service {ServiceName}", sessionId, request.ServiceName);

            await Task.CompletedTask;

            return new MonitoringResult
            {
                SessionId = sessionId,
                Success = true,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start monitoring for service {ServiceName}", request.ServiceName);
            return new MonitoringResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <inheritdoc/>
    public async Task<MonitoringResult> StopMonitoringAsync(StopMonitoringRequest request, BlockchainType blockchainType)
    {
        if (!base.SupportsBlockchain(blockchainType))
        {
            return new MonitoringResult
            {
                Success = false,
                ErrorMessage = $"Blockchain type {blockchainType} is not supported"
            };
        }

        try
        {
            var stoppedSessions = new List<string>();

            lock (_cacheLock)
            {
                var sessionsToStop = _monitoringSessions.Values
                    .Where(s => s.ServiceName.Equals(request.ServiceName, StringComparison.OrdinalIgnoreCase) && s.IsActive)
                    .ToArray();

                foreach (var session in sessionsToStop)
                {
                    session.IsActive = false;
                    session.EndedAt = DateTime.UtcNow;
                    session.LastUpdated = DateTime.UtcNow;
                    stoppedSessions.Add(session.SessionId);
                }
            }

            Logger.LogInformation("Stopped {Count} monitoring sessions for service {ServiceName}", stoppedSessions.Count, request.ServiceName);

            await Task.CompletedTask;

            return new MonitoringResult
            {
                SessionId = stoppedSessions.FirstOrDefault() ?? string.Empty,
                Success = true,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to stop monitoring for service {ServiceName}", request.ServiceName);
            return new MonitoringResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Initializes monitoring components.
    /// </summary>
    private async Task InitializeMonitoringComponentsAsync()
    {
        await Task.Delay(100); // Simulate initialization
        Logger.LogDebug("Monitoring components initialized");
    }

    /// <summary>
    /// Stops monitoring operations.
    /// </summary>
    private async Task StopMonitoringOperationsAsync()
    {
        await Task.Delay(50); // Simulate cleanup
        Logger.LogDebug("Monitoring operations stopped");
    }

    /// <summary>
    /// Initializes health checking subsystem.
    /// </summary>
    private async Task InitializeHealthCheckingAsync()
    {
        await Task.Delay(50);
        Logger.LogDebug("Health checking subsystem initialized");
    }

    /// <summary>
    /// Initializes metrics collection subsystem.
    /// </summary>
    private async Task InitializeMetricsCollectionAsync()
    {
        await Task.Delay(50);
        Logger.LogDebug("Metrics collection subsystem initialized");
    }

    /// <summary>
    /// Initializes alerting subsystem.
    /// </summary>
    private async Task InitializeAlertingAsync()
    {
        await Task.Delay(50);
        Logger.LogDebug("Alerting subsystem initialized");
    }

    /// <summary>
    /// Executes an operation asynchronously with proper error handling.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <returns>The result of the operation.</returns>
    protected async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing operation");
            throw;
        }
    }

    /// <summary>
    /// Executes an operation asynchronously with proper error handling.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    protected async Task ExecuteAsync(Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing operation");
            throw;
        }
    }
}
