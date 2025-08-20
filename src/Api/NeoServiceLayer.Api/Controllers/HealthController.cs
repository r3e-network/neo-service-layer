using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Health;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Api.Controllers;

/// <summary>
/// Controller for health checks and system monitoring.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/health")]
[Tags("Health")]
public class HealthController : BaseApiController
{
    private readonly HealthCheckService _healthCheckService;
    private readonly IHealthService? _healthService;
    private readonly ILogger<HealthController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthController"/> class.
    /// </summary>
    /// <param name="healthCheckService">The health check service.</param>
    /// <param name="healthService">The health service (optional).</param>
    /// <param name="logger">The logger.</param>
    public HealthController(
        HealthCheckService healthCheckService,
        IHealthService? healthService,
        ILogger<HealthController> logger) : base(logger)
    {
        _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
        _healthService = healthService;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the basic health status of the API.
    /// </summary>
    /// <returns>The health status.</returns>
    /// <response code="200">Service is healthy.</response>
    /// <response code="503">Service is unhealthy.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 503)]
    public async Task<IActionResult> GetHealth()
    {
        var result = await _healthCheckService.CheckHealthAsync();

        var response = new
        {
            status = result.Status.ToString(),
            timestamp = DateTimeOffset.UtcNow,
            duration = result.TotalDuration.TotalMilliseconds
        };

        return result.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy
            ? Ok(response)
            : StatusCode(503, response);
    }

    /// <summary>
    /// Gets the detailed health status including all health checks.
    /// </summary>
    /// <returns>The detailed health status.</returns>
    /// <response code="200">Health check report generated.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="503">Service is unhealthy.</response>
    [HttpGet("detailed")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(object), 503)]
    public async Task<IActionResult> GetDetailedHealth()
    {
        var result = await _healthCheckService.CheckHealthAsync();

        var healthChecks = result.Entries.Select(entry => new
        {
            name = entry.Key,
            status = entry.Value.Status.ToString(),
            duration = entry.Value.Duration.TotalMilliseconds,
            description = entry.Value.Description,
            data = entry.Value.Data,
            exception = entry.Value.Exception?.Message
        });

        var response = new
        {
            status = result.Status.ToString(),
            timestamp = DateTimeOffset.UtcNow,
            totalDuration = result.TotalDuration.TotalMilliseconds,
            checks = healthChecks
        };

        return result.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy
            ? Ok(response)
            : StatusCode(503, response);
    }

    /// <summary>
    /// Gets the liveness probe for Kubernetes.
    /// </summary>
    /// <returns>The liveness status.</returns>
    /// <response code="200">Service is alive.</response>
    /// <response code="503">Service is not alive.</response>
    [HttpGet("live")]
    [AllowAnonymous]
    [ProducesResponseType(200)]
    [ProducesResponseType(503)]
    public IActionResult GetLiveness()
    {
        // Simple liveness check - if the API can respond, it's alive
        return Ok(new { status = "alive", timestamp = DateTimeOffset.UtcNow });
    }

    /// <summary>
    /// Gets the readiness probe for Kubernetes.
    /// </summary>
    /// <returns>The readiness status.</returns>
    /// <response code="200">Service is ready.</response>
    /// <response code="503">Service is not ready.</response>
    [HttpGet("ready")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 503)]
    public async Task<IActionResult> GetReadiness()
    {
        var result = await _healthCheckService.CheckHealthAsync(
            predicate: check => check.Tags.Contains("ready"));

        var response = new
        {
            status = result.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy ? "ready" : "not_ready",
            timestamp = DateTimeOffset.UtcNow
        };

        return result.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy
            ? Ok(response)
            : StatusCode(503, response);
    }

    /// <summary>
    /// Gets the system information.
    /// </summary>
    /// <returns>The system information.</returns>
    /// <response code="200">System information retrieved.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpGet("info")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public IActionResult GetSystemInfo()
    {
        var info = new
        {
            version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            sgxMode = Environment.GetEnvironmentVariable("SGX_MODE") ?? "Unknown",
            timestamp = DateTimeOffset.UtcNow,
            uptime = TimeSpan.FromMilliseconds(Environment.TickCount64),
            runtime = new
            {
                framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                os = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                architecture = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(),
                processArchitecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString()
            },
            memory = new
            {
                workingSet = Environment.WorkingSet,
                gc = new
                {
                    gen0 = GC.CollectionCount(0),
                    gen1 = GC.CollectionCount(1),
                    gen2 = GC.CollectionCount(2),
                    totalMemory = GC.GetTotalMemory(false)
                }
            }
        };

        return Ok(info);
    }
}
