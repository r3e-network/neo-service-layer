using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Neo.SecretsManagement.Service.Services;
using System.Security.Claims;

namespace Neo.SecretsManagement.Service.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "SecretAdmin,SystemAdmin")]
public class MonitoringController : ControllerBase
{
    private readonly IMonitoringService _monitoringService;
    private readonly IAuditService _auditService;
    private readonly ILogger<MonitoringController> _logger;

    public MonitoringController(
        IMonitoringService monitoringService,
        IAuditService auditService,
        ILogger<MonitoringController> logger)
    {
        _monitoringService = monitoringService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Get service health status
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous] // Health endpoint should be accessible for load balancers
    public async Task<ActionResult<ServiceHealthStatus>> GetHealthStatus()
    {
        try
        {
            var healthStatus = await _monitoringService.GetHealthStatusAsync();
            
            // Return appropriate HTTP status based on health
            var httpStatusCode = healthStatus.OverallStatus.ToLower() switch
            {
                "healthy" => 200,
                "degraded" => 200, // Still operational but with issues
                "unhealthy" => 503, // Service unavailable
                _ => 503
            };

            return StatusCode(httpStatusCode, healthStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get health status");
            return StatusCode(503, new ServiceHealthStatus
            {
                OverallStatus = "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Error = "Health check failed"
            });
        }
    }

    /// <summary>
    /// Get comprehensive service metrics
    /// </summary>
    [HttpGet("metrics")]
    public async Task<ActionResult<ServiceMetrics>> GetMetrics()
    {
        try
        {
            var userId = GetUserId();
            var metrics = await _monitoringService.GetMetricsAsync();

            await _auditService.LogAsync(
                userId,
                "view",
                "metrics",
                "service_metrics",
                null,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["metrics_type"] = "service",
                    ["timestamp"] = metrics.Timestamp.ToString("O")
                },
                GetClientIp(),
                Request.Headers.UserAgent.FirstOrDefault()
            );

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get service metrics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get performance metrics
    /// </summary>
    [HttpGet("performance")]
    public async Task<ActionResult<PerformanceMetrics>> GetPerformanceMetrics()
    {
        try
        {
            var userId = GetUserId();
            var metrics = await _monitoringService.GetPerformanceMetricsAsync();

            await _auditService.LogAsync(
                userId,
                "view",
                "metrics",
                "performance_metrics",
                null,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["metrics_type"] = "performance",
                    ["cpu_usage"] = metrics.CpuUsage,
                    ["memory_usage_mb"] = metrics.MemoryUsage / 1024 / 1024,
                    ["timestamp"] = metrics.Timestamp.ToString("O")
                },
                GetClientIp(),
                Request.Headers.UserAgent.FirstOrDefault()
            );

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get performance metrics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get security metrics
    /// </summary>
    [HttpGet("security")]
    public async Task<ActionResult<SecurityMetrics>> GetSecurityMetrics()
    {
        try
        {
            var userId = GetUserId();
            var metrics = await _monitoringService.GetSecurityMetricsAsync();

            await _auditService.LogAsync(
                userId,
                "view",
                "metrics",
                "security_metrics",
                null,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["metrics_type"] = "security",
                    ["failed_logins_24h"] = metrics.AuthenticationMetrics.FailedLoginAttempts24h,
                    ["access_violations_24h"] = metrics.AccessViolations24h,
                    ["suspicious_activities"] = metrics.SuspiciousActivities,
                    ["timestamp"] = metrics.Timestamp.ToString("O")
                },
                GetClientIp(),
                Request.Headers.UserAgent.FirstOrDefault()
            );

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get security metrics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get monitoring dashboard data
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<MonitoringDashboard>> GetDashboard([FromQuery] int hoursBack = 24)
    {
        try
        {
            var userId = GetUserId();
            
            // Get all metrics in parallel
            var healthTask = _monitoringService.GetHealthStatusAsync();
            var metricsTask = _monitoringService.GetMetricsAsync();
            var performanceTask = _monitoringService.GetPerformanceMetricsAsync();
            var securityTask = _monitoringService.GetSecurityMetricsAsync();

            await Task.WhenAll(healthTask, metricsTask, performanceTask, securityTask);

            var dashboard = new MonitoringDashboard
            {
                Timestamp = DateTime.UtcNow,
                HoursBack = hoursBack,
                HealthStatus = healthTask.Result,
                ServiceMetrics = metricsTask.Result,
                PerformanceMetrics = performanceTask.Result,
                SecurityMetrics = securityTask.Result,
                Alerts = GenerateAlerts(healthTask.Result, performanceTask.Result, securityTask.Result),
                Summary = GenerateSummary(healthTask.Result, metricsTask.Result, performanceTask.Result, securityTask.Result)
            };

            await _auditService.LogAsync(
                userId,
                "view",
                "dashboard",
                "monitoring_dashboard",
                null,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["hours_back"] = hoursBack,
                    ["overall_status"] = dashboard.HealthStatus.OverallStatus,
                    ["alert_count"] = dashboard.Alerts.Count,
                    ["timestamp"] = dashboard.Timestamp.ToString("O")
                },
                GetClientIp(),
                Request.Headers.UserAgent.FirstOrDefault()
            );

            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get monitoring dashboard");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Record a security event
    /// </summary>
    [HttpPost("security-event")]
    public async Task<IActionResult> RecordSecurityEvent([FromBody] SecurityEventRequest request)
    {
        try
        {
            var userId = GetUserId();
            
            await _monitoringService.RecordSecurityEventAsync(
                request.EventType,
                request.Details,
                request.UserId ?? userId
            );

            await _auditService.LogAsync(
                userId,
                "record",
                "security_event",
                request.EventType,
                null,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["event_type"] = request.EventType,
                    ["target_user"] = request.UserId ?? "self",
                    ["severity"] = request.Severity?.ToString() ?? "info"
                },
                GetClientIp(),
                Request.Headers.UserAgent.FirstOrDefault()
            );

            return Ok(new { message = "Security event recorded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record security event");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get system alerts
    /// </summary>
    [HttpGet("alerts")]
    public async Task<ActionResult<AlertsResponse>> GetAlerts([FromQuery] string? severity = null)
    {
        try
        {
            var userId = GetUserId();
            
            // Get current metrics to generate alerts
            var healthStatus = await _monitoringService.GetHealthStatusAsync();
            var performanceMetrics = await _monitoringService.GetPerformanceMetricsAsync();
            var securityMetrics = await _monitoringService.GetSecurityMetricsAsync();

            var alerts = GenerateAlerts(healthStatus, performanceMetrics, securityMetrics);

            // Filter by severity if requested
            if (!string.IsNullOrEmpty(severity))
            {
                alerts = alerts.Where(a => a.Severity.Equals(severity, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var response = new AlertsResponse
            {
                Alerts = alerts,
                TotalAlerts = alerts.Count,
                CriticalAlerts = alerts.Count(a => a.Severity.Equals("critical", StringComparison.OrdinalIgnoreCase)),
                WarningAlerts = alerts.Count(a => a.Severity.Equals("warning", StringComparison.OrdinalIgnoreCase)),
                InfoAlerts = alerts.Count(a => a.Severity.Equals("info", StringComparison.OrdinalIgnoreCase)),
                Timestamp = DateTime.UtcNow
            };

            await _auditService.LogAsync(
                userId,
                "view",
                "alerts",
                "system_alerts",
                null,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["total_alerts"] = response.TotalAlerts,
                    ["critical_alerts"] = response.CriticalAlerts,
                    ["warning_alerts"] = response.WarningAlerts,
                    ["severity_filter"] = severity ?? "all"
                },
                GetClientIp(),
                Request.Headers.UserAgent.FirstOrDefault()
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get alerts");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Export metrics in Prometheus format
    /// </summary>
    [HttpGet("prometheus")]
    [AllowAnonymous] // Prometheus scraping should be accessible
    public async Task<ActionResult<string>> GetPrometheusMetrics()
    {
        try
        {
            var metrics = await _monitoringService.GetMetricsAsync();
            var performanceMetrics = await _monitoringService.GetPerformanceMetricsAsync();
            
            var prometheusMetrics = ConvertToPrometheusFormat(metrics, performanceMetrics);
            
            return Content(prometheusMetrics, "text/plain; charset=utf-8");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Prometheus metrics");
            return StatusCode(500, "# Error generating metrics\n");
        }
    }

    private List<SystemAlert> GenerateAlerts(ServiceHealthStatus health, PerformanceMetrics performance, SecurityMetrics security)
    {
        var alerts = new List<SystemAlert>();

        // Health alerts
        if (health.OverallStatus == "Unhealthy")
        {
            alerts.Add(new SystemAlert
            {
                Id = Guid.NewGuid().ToString(),
                Severity = "critical",
                Title = "Service Unhealthy",
                Description = "Service is in unhealthy state",
                Timestamp = DateTime.UtcNow,
                Category = "health"
            });
        }

        // Performance alerts
        if (performance.CpuUsage > 80)
        {
            alerts.Add(new SystemAlert
            {
                Id = Guid.NewGuid().ToString(),
                Severity = "warning",
                Title = "High CPU Usage",
                Description = $"CPU usage is {performance.CpuUsage:F1}%",
                Timestamp = DateTime.UtcNow,
                Category = "performance"
            });
        }

        if (performance.MemoryUsage > 1024 * 1024 * 1024) // > 1GB
        {
            alerts.Add(new SystemAlert
            {
                Id = Guid.NewGuid().ToString(),
                Severity = "warning",
                Title = "High Memory Usage",
                Description = $"Memory usage is {performance.MemoryUsage / 1024 / 1024:F0} MB",
                Timestamp = DateTime.UtcNow,
                Category = "performance"
            });
        }

        // Security alerts
        if (security.AuthenticationMetrics.FailedLoginAttempts24h > 10)
        {
            alerts.Add(new SystemAlert
            {
                Id = Guid.NewGuid().ToString(),
                Severity = "warning",
                Title = "High Failed Login Attempts",
                Description = $"{security.AuthenticationMetrics.FailedLoginAttempts24h} failed login attempts in last 24 hours",
                Timestamp = DateTime.UtcNow,
                Category = "security"
            });
        }

        if (security.SuspiciousActivities > 5)
        {
            alerts.Add(new SystemAlert
            {
                Id = Guid.NewGuid().ToString(),
                Severity = "critical",
                Title = "Suspicious Activities Detected",
                Description = $"{security.SuspiciousActivities} suspicious activities detected",
                Timestamp = DateTime.UtcNow,
                Category = "security"
            });
        }

        return alerts;
    }

    private DashboardSummary GenerateSummary(ServiceHealthStatus health, ServiceMetrics metrics, PerformanceMetrics performance, SecurityMetrics security)
    {
        return new DashboardSummary
        {
            OverallStatus = health.OverallStatus,
            TotalSecrets = metrics.SecretMetrics.TotalSecrets,
            ActiveSecrets = metrics.SecretMetrics.ActiveSecrets,
            CpuUsage = performance.CpuUsage,
            MemoryUsageMB = performance.MemoryUsage / 1024 / 1024,
            FailedLogins24h = security.AuthenticationMetrics.FailedLoginAttempts24h,
            SecurityEvents24h = security.SecurityEvents.Values.Sum(),
            BackupCount = metrics.BackupMetrics.TotalBackups,
            LastBackupSize = metrics.BackupMetrics.TotalBackupSize
        };
    }

    private string ConvertToPrometheusFormat(ServiceMetrics metrics, PerformanceMetrics performance)
    {
        var prometheus = new System.Text.StringBuilder();

        // Service metrics
        prometheus.AppendLine("# HELP secrets_total Total number of secrets");
        prometheus.AppendLine("# TYPE secrets_total gauge");
        prometheus.AppendLine($"secrets_total {metrics.SecretMetrics.TotalSecrets}");

        prometheus.AppendLine("# HELP secrets_active Active secrets count");
        prometheus.AppendLine("# TYPE secrets_active gauge");
        prometheus.AppendLine($"secrets_active {metrics.SecretMetrics.ActiveSecrets}");

        // Performance metrics
        prometheus.AppendLine("# HELP cpu_usage_percent CPU usage percentage");
        prometheus.AppendLine("# TYPE cpu_usage_percent gauge");
        prometheus.AppendLine($"cpu_usage_percent {performance.CpuUsage}");

        prometheus.AppendLine("# HELP memory_usage_bytes Memory usage in bytes");
        prometheus.AppendLine("# TYPE memory_usage_bytes gauge");
        prometheus.AppendLine($"memory_usage_bytes {performance.MemoryUsage}");

        // Operation metrics
        foreach (var operation in metrics.OperationMetrics)
        {
            prometheus.AppendLine($"# HELP operation_total_{operation.Operation} Total operations for {operation.Operation}");
            prometheus.AppendLine($"# TYPE operation_total_{operation.Operation} counter");
            prometheus.AppendLine($"operation_total_{{{operation.Operation.Replace("-", "_")}}} {operation.TotalOperations}");

            prometheus.AppendLine($"# HELP operation_duration_ms_{operation.Operation} Average duration for {operation.Operation}");
            prometheus.AppendLine($"# TYPE operation_duration_ms_{operation.Operation} gauge");
            prometheus.AppendLine($"operation_duration_ms_{{{operation.Operation.Replace("-", "_")}}} {operation.AverageResponseTime}");
        }

        return prometheus.ToString();
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) 
            ?? User.FindFirstValue("sub") 
            ?? User.FindFirstValue("user_id") 
            ?? "anonymous";
    }

    private string? GetClientIp()
    {
        return Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim()
            ?? Request.Headers["X-Real-IP"].FirstOrDefault()
            ?? HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}

// Request/Response DTOs
public class SecurityEventRequest
{
    public string EventType { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public SecurityEventSeverity? Severity { get; set; }
}

public enum SecurityEventSeverity
{
    Info,
    Warning,
    Critical
}

public class MonitoringDashboard
{
    public DateTime Timestamp { get; set; }
    public int HoursBack { get; set; }
    public ServiceHealthStatus HealthStatus { get; set; } = new();
    public ServiceMetrics ServiceMetrics { get; set; } = new();
    public PerformanceMetrics PerformanceMetrics { get; set; } = new();
    public SecurityMetrics SecurityMetrics { get; set; } = new();
    public List<SystemAlert> Alerts { get; set; } = new();
    public DashboardSummary Summary { get; set; } = new();
}

public class SystemAlert
{
    public string Id { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class AlertsResponse
{
    public List<SystemAlert> Alerts { get; set; } = new();
    public int TotalAlerts { get; set; }
    public int CriticalAlerts { get; set; }
    public int WarningAlerts { get; set; }
    public int InfoAlerts { get; set; }
    public DateTime Timestamp { get; set; }
}

public class DashboardSummary
{
    public string OverallStatus { get; set; } = string.Empty;
    public int TotalSecrets { get; set; }
    public int ActiveSecrets { get; set; }
    public double CpuUsage { get; set; }
    public long MemoryUsageMB { get; set; }
    public int FailedLogins24h { get; set; }
    public int SecurityEvents24h { get; set; }
    public int BackupCount { get; set; }
    public long LastBackupSize { get; set; }
}