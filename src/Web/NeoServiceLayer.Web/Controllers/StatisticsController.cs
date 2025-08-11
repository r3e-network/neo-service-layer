using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Statistics;

namespace NeoServiceLayer.Web.Controllers;

/// <summary>
/// Controller for statistics and monitoring endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;
    private readonly ILogger<StatisticsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatisticsController"/> class.
    /// </summary>
    public StatisticsController(IStatisticsService statisticsService, ILogger<StatisticsController> logger)
    {
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets overall system statistics.
    /// </summary>
    /// <returns>System statistics.</returns>
    [HttpGet("system")]
    public async Task<IActionResult> GetSystemStatistics()
    {
        try
        {
            var stats = await _statisticsService.GetSystemStatisticsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system statistics");
            return StatusCode(500, new { error = "Failed to retrieve system statistics" });
        }
    }

    /// <summary>
    /// Gets statistics for all services.
    /// </summary>
    /// <returns>Service statistics.</returns>
    [HttpGet("services")]
    public async Task<IActionResult> GetAllServiceStatistics()
    {
        try
        {
            var stats = await _statisticsService.GetAllServiceStatisticsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service statistics");
            return StatusCode(500, new { error = "Failed to retrieve service statistics" });
        }
    }

    /// <summary>
    /// Gets statistics for a specific service.
    /// </summary>
    /// <param name="serviceName">Service name.</param>
    /// <returns>Service statistics.</returns>
    [HttpGet("services/{serviceName}")]
    public async Task<IActionResult> GetServiceStatistics(string serviceName)
    {
        try
        {
            var stats = await _statisticsService.GetServiceStatisticsAsync(serviceName);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics for service {ServiceName}", serviceName);
            return StatusCode(500, new { error = $"Failed to retrieve statistics for service {serviceName}" });
        }
    }

    /// <summary>
    /// Gets blockchain statistics.
    /// </summary>
    /// <param name="blockchain">Blockchain type (NeoN3 or NeoX).</param>
    /// <returns>Blockchain statistics.</returns>
    [HttpGet("blockchain/{blockchain}")]
    public async Task<IActionResult> GetBlockchainStatistics(string blockchain)
    {
        try
        {
            if (!Enum.TryParse<BlockchainType>(blockchain, true, out var blockchainType))
            {
                return BadRequest(new { error = $"Invalid blockchain type: {blockchain}" });
            }

            var stats = await _statisticsService.GetBlockchainStatisticsAsync(blockchainType);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blockchain statistics for {Blockchain}", blockchain);
            return StatusCode(500, new { error = $"Failed to retrieve blockchain statistics for {blockchain}" });
        }
    }

    /// <summary>
    /// Gets performance metrics for a time range.
    /// </summary>
    /// <param name="startTime">Start time (ISO 8601 format).</param>
    /// <param name="endTime">End time (ISO 8601 format).</param>
    /// <returns>Performance metrics.</returns>
    [HttpGet("performance")]
    public async Task<IActionResult> GetPerformanceMetrics(
        [FromQuery] DateTime? startTime = null, 
        [FromQuery] DateTime? endTime = null)
    {
        try
        {
            var start = startTime ?? DateTime.UtcNow.AddHours(-1);
            var end = endTime ?? DateTime.UtcNow;

            if (start > end)
            {
                return BadRequest(new { error = "Start time must be before end time" });
            }

            var metrics = await _statisticsService.GetPerformanceMetricsAsync(start, end);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance metrics");
            return StatusCode(500, new { error = "Failed to retrieve performance metrics" });
        }
    }

    /// <summary>
    /// Records an operation metric.
    /// </summary>
    /// <param name="request">Operation record request.</param>
    /// <returns>Success response.</returns>
    [HttpPost("record/operation")]
    public async Task<IActionResult> RecordOperation([FromBody] RecordOperationRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrEmpty(request.ServiceName) || string.IsNullOrEmpty(request.Operation))
            {
                return BadRequest(new { error = "Invalid request. ServiceName and Operation are required." });
            }

            await _statisticsService.RecordOperationAsync(
                request.ServiceName, 
                request.Operation, 
                request.Success, 
                request.Duration);

            return Ok(new { message = "Operation recorded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording operation");
            return StatusCode(500, new { error = "Failed to record operation" });
        }
    }

    /// <summary>
    /// Records a blockchain transaction metric.
    /// </summary>
    /// <param name="request">Transaction record request.</param>
    /// <returns>Success response.</returns>
    [HttpPost("record/transaction")]
    public async Task<IActionResult> RecordTransaction([FromBody] RecordTransactionRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrEmpty(request.TransactionType))
            {
                return BadRequest(new { error = "Invalid request. TransactionType is required." });
            }

            if (!Enum.TryParse<BlockchainType>(request.Blockchain, true, out var blockchainType))
            {
                return BadRequest(new { error = $"Invalid blockchain type: {request.Blockchain}" });
            }

            await _statisticsService.RecordTransactionAsync(
                blockchainType, 
                request.TransactionType, 
                request.Success);

            return Ok(new { message = "Transaction recorded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording transaction");
            return StatusCode(500, new { error = "Failed to record transaction" });
        }
    }

    /// <summary>
    /// Exports statistics for a time range.
    /// </summary>
    /// <param name="startTime">Start time (ISO 8601 format).</param>
    /// <param name="endTime">End time (ISO 8601 format).</param>
    /// <param name="format">Export format (json, csv, prometheus).</param>
    /// <returns>Exported data.</returns>
    [HttpGet("export")]
    public async Task<IActionResult> ExportStatistics(
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null,
        [FromQuery] string format = "json")
    {
        try
        {
            var start = startTime ?? DateTime.UtcNow.AddDays(-1);
            var end = endTime ?? DateTime.UtcNow;

            if (start > end)
            {
                return BadRequest(new { error = "Start time must be before end time" });
            }

            var supportedFormats = new[] { "json", "csv", "prometheus" };
            if (!supportedFormats.Contains(format.ToLower()))
            {
                return BadRequest(new { error = $"Unsupported format. Supported formats: {string.Join(", ", supportedFormats)}" });
            }

            var data = await _statisticsService.ExportStatisticsAsync(start, end, format);

            var contentType = format.ToLower() switch
            {
                "json" => "application/json",
                "csv" => "text/csv",
                "prometheus" => "text/plain",
                _ => "application/octet-stream"
            };

            var fileName = $"neo-service-layer-stats-{start:yyyyMMdd}-{end:yyyyMMdd}.{format}";
            
            return File(data, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting statistics");
            return StatusCode(500, new { error = "Failed to export statistics" });
        }
    }

    /// <summary>
    /// Gets real-time statistics updates via Server-Sent Events.
    /// </summary>
    /// <returns>SSE stream of statistics updates.</returns>
    [HttpGet("realtime")]
    public async Task GetRealTimeStatistics()
    {
        Response.Headers.Add("Content-Type", "text/event-stream");
        Response.Headers.Add("Cache-Control", "no-cache");
        Response.Headers.Add("Connection", "keep-alive");

        try
        {
            await foreach (var update in _statisticsService.GetRealTimeStatisticsAsync(HttpContext.RequestAborted))
            {
                var data = System.Text.Json.JsonSerializer.Serialize(update);
                await Response.WriteAsync($"data: {data}\n\n");
                await Response.Body.FlushAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming real-time statistics");
        }
    }

    /// <summary>
    /// Request model for recording operations.
    /// </summary>
    public class RecordOperationRequest
    {
        /// <summary>
        /// Service name.
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Operation name.
        /// </summary>
        public string Operation { get; set; } = string.Empty;

        /// <summary>
        /// Whether the operation succeeded.
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Operation duration in milliseconds.
        /// </summary>
        public long Duration { get; set; }
    }

    /// <summary>
    /// Request model for recording transactions.
    /// </summary>
    public class RecordTransactionRequest
    {
        /// <summary>
        /// Blockchain type (NeoN3 or NeoX).
        /// </summary>
        public string Blockchain { get; set; } = "NeoN3";

        /// <summary>
        /// Transaction type.
        /// </summary>
        public string TransactionType { get; set; } = string.Empty;

        /// <summary>
        /// Whether the transaction succeeded.
        /// </summary>
        public bool Success { get; set; } = true;
    }
}