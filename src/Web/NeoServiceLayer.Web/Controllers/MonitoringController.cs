using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Monitoring;
using NeoServiceLayer.Services.Monitoring.Models;

namespace NeoServiceLayer.Web.Controllers;

/// <summary>
/// API controller for system monitoring and metrics operations.
/// </summary>
[Tags("Monitoring")]
public class MonitoringController : BaseApiController
{
    private readonly IMonitoringService _monitoringService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MonitoringController"/> class.
    /// </summary>
    /// <param name="monitoringService">The monitoring service.</param>
    /// <param name="logger">The logger.</param>
    public MonitoringController(
        IMonitoringService monitoringService,
        ILogger<MonitoringController> logger) : base(logger)
    {
        _monitoringService = monitoringService;
    }

    /// <summary>
    /// Gets the current health status of all services.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The system health status.</returns>
    /// <response code="200">System health retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpGet("system-health/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<SystemHealthResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetSystemHealth([FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var healthStatus = await _monitoringService.GetSystemHealthAsync(blockchain);

            Logger.LogInformation("Retrieved system health status on {BlockchainType} by user {UserId}",
                blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(healthStatus, "System health status retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetSystemHealth");
        }
    }

    /// <summary>
    /// Gets performance metrics for a specific service.
    /// </summary>
    /// <param name="request">The metrics request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The service metrics.</returns>
    /// <response code="200">Service metrics retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpPost("service-metrics/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<ServiceMetricsResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetServiceMetrics(
        [FromBody] ServiceMetricsRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var metrics = await _monitoringService.GetServiceMetricsAsync(request, blockchain);

            Logger.LogInformation("Retrieved metrics for service {ServiceName} on {BlockchainType} by user {UserId}",
                request.ServiceName, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(metrics, "Service metrics retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetServiceMetrics");
        }
    }

    /// <summary>
    /// Records a custom metric.
    /// </summary>
    /// <param name="request">The metric recording request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The recording result.</returns>
    /// <response code="200">Metric recorded successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Metric recording failed.</response>
    [HttpPost("record-metric/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<MetricRecordingResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> RecordMetric(
        [FromBody] RecordMetricRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _monitoringService.RecordMetricAsync(request, blockchain);

            Logger.LogInformation("Recorded metric {MetricName} with value {Value} for service {ServiceName} on {BlockchainType} by user {UserId}",
                request.MetricName, request.Value, request.ServiceName, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(result, "Metric recorded successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "RecordMetric");
        }
    }

    /// <summary>
    /// Gets system performance statistics.
    /// </summary>
    /// <param name="request">The performance statistics request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The performance statistics.</returns>
    /// <response code="200">Performance statistics retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpPost("performance-statistics/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<PerformanceStatisticsResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetPerformanceStatistics(
        [FromBody] PerformanceStatisticsRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var statistics = await _monitoringService.GetPerformanceStatisticsAsync(request, blockchain);

            Logger.LogInformation("Retrieved performance statistics on {BlockchainType} by user {UserId}",
                blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(statistics, "Performance statistics retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetPerformanceStatistics");
        }
    }

    /// <summary>
    /// Creates an alert rule for monitoring.
    /// </summary>
    /// <param name="request">The alert rule creation request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The alert rule creation result.</returns>
    /// <response code="200">Alert rule created successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Alert rule creation failed.</response>
    [HttpPost("create-alert-rule/{blockchainType}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<AlertRuleResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> CreateAlertRule(
        [FromBody] CreateAlertRuleRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _monitoringService.CreateAlertRuleAsync(request, blockchain);

            Logger.LogInformation("Created alert rule {RuleName} on {BlockchainType} by user {UserId}",
                request.RuleName, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(result, "Alert rule created successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "CreateAlertRule");
        }
    }

    /// <summary>
    /// Gets active alerts.
    /// </summary>
    /// <param name="request">The alerts request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The active alerts.</returns>
    /// <response code="200">Active alerts retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpPost("active-alerts/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<AlertsResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetActiveAlerts(
        [FromBody] GetAlertsRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var alerts = await _monitoringService.GetActiveAlertsAsync(request, blockchain);

            Logger.LogInformation("Retrieved {AlertCount} active alerts on {BlockchainType} by user {UserId}",
                alerts.Alerts?.Length ?? 0, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(alerts, "Active alerts retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetActiveAlerts");
        }
    }

    /// <summary>
    /// Gets system logs.
    /// </summary>
    /// <param name="request">The logs request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The system logs.</returns>
    /// <response code="200">System logs retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpPost("logs/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<LogsResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetLogs(
        [FromBody] GetLogsRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var logs = await _monitoringService.GetLogsAsync(request, blockchain);

            Logger.LogInformation("Retrieved {LogCount} log entries on {BlockchainType} by user {UserId}",
                logs.LogEntries?.Length ?? 0, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(logs, "System logs retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetLogs");
        }
    }

    /// <summary>
    /// Starts monitoring a service.
    /// </summary>
    /// <param name="request">The monitoring request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The monitoring result.</returns>
    /// <response code="200">Service monitoring started successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Monitoring start failed.</response>
    [HttpPost("start-monitoring/{blockchainType}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<MonitoringResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> StartMonitoring(
        [FromBody] StartMonitoringRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _monitoringService.StartMonitoringAsync(request, blockchain);

            Logger.LogInformation("Started monitoring service {ServiceName} on {BlockchainType} by user {UserId}",
                request.ServiceName, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(result, "Service monitoring started successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "StartMonitoring");
        }
    }

    /// <summary>
    /// Stops monitoring a service.
    /// </summary>
    /// <param name="request">The stop monitoring request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The monitoring result.</returns>
    /// <response code="200">Service monitoring stopped successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Monitoring stop failed.</response>
    [HttpPost("stop-monitoring/{blockchainType}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<MonitoringResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> StopMonitoring(
        [FromBody] StopMonitoringRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _monitoringService.StopMonitoringAsync(request, blockchain);

            Logger.LogInformation("Stopped monitoring service {ServiceName} on {BlockchainType} by user {UserId}",
                request.ServiceName, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(result, "Service monitoring stopped successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "StopMonitoring");
        }
    }
}
