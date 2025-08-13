using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.Observability.Metrics;
using NeoServiceLayer.Services.Authentication;

namespace NeoServiceLayer.Api.Controllers
{
    /// <summary>
    /// Monitoring endpoints for authentication service
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/monitoring/authentication")]
    [Authorize(Roles = "admin,monitor")]
    public class AuthenticationMonitoringController : BaseApiController
    {
        private readonly IAuthenticationMetricsCollector _metricsCollector;
        private readonly ISecurityLogger _securityLogger;
        private readonly HealthCheckService _healthCheckService;
        private readonly IMetricsService _metricsService;

        public AuthenticationMonitoringController(
            ILogger<AuthenticationMonitoringController> logger,
            IAuthenticationMetricsCollector metricsCollector,
            ISecurityLogger securityLogger,
            HealthCheckService healthCheckService,
            IMetricsService metricsService) : base(logger)
        {
            _metricsCollector = metricsCollector;
            _securityLogger = securityLogger;
            _healthCheckService = healthCheckService;
            _metricsService = metricsService;
        }

        /// <summary>
        /// Get authentication service health status
        /// </summary>
        [HttpGet("health")]
        [AllowAnonymous] // Health checks should be accessible without auth for monitoring tools
        [ProducesResponseType(typeof(HealthCheckResponse), 200)]
        [ProducesResponseType(503)]
        public async Task<IActionResult> GetHealthStatus()
        {
            try
            {
                var report = await _healthCheckService.CheckHealthAsync();

                var response = new HealthCheckResponse
                {
                    Status = report.Status.ToString(),
                    TotalDuration = report.TotalDuration,
                    Entries = report.Entries.Select(e => new HealthCheckEntry
                    {
                        Name = e.Key,
                        Status = e.Value.Status.ToString(),
                        Description = e.Value.Description,
                        Duration = e.Value.Duration,
                        Data = e.Value.Data,
                        Tags = e.Value.Tags?.ToList()
                    }).ToList()
                };

                var statusCode = report.Status switch
                {
                    HealthStatus.Healthy => 200,
                    HealthStatus.Degraded => 200,
                    HealthStatus.Unhealthy => 503,
                    _ => 503
                };

                return StatusCode(statusCode, response);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get health status");
                return StatusCode(503, new HealthCheckResponse
                {
                    Status = "Unhealthy",
                    TotalDuration = TimeSpan.Zero,
                    Entries = new List<HealthCheckEntry>
                    {
                        new HealthCheckEntry
                        {
                            Name = "Exception",
                            Status = "Unhealthy",
                            Description = ex.Message
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Get authentication metrics
        /// </summary>
        [HttpGet("metrics")]
        [ProducesResponseType(typeof(ApiResponse<AuthenticationMetrics>), 200)]
        public async Task<IActionResult> GetMetrics(
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null)
        {
            try
            {
                var metrics = await _metricsCollector.GetMetricsAsync();

                await _securityLogger.LogSecurityEventAsync("MetricsAccessed", GetUserId(),
                    new Dictionary<string, object>
                    {
                        ["IpAddress"] = GetClientIpAddress(),
                        ["StartTime"] = startTime,
                        ["EndTime"] = endTime
                    });

                return Ok(CreateResponse(metrics, "Metrics retrieved successfully"));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GetMetrics");
            }
        }

        /// <summary>
        /// Get login statistics
        /// </summary>
        [HttpGet("metrics/logins")]
        [ProducesResponseType(typeof(ApiResponse<LoginStatistics>), 200)]
        public async Task<IActionResult> GetLoginStatistics(
            [FromQuery] int hours = 24)
        {
            try
            {
                var metrics = await _metricsCollector.GetMetricsAsync();

                var stats = new LoginStatistics
                {
                    Period = $"Last {hours} hours",
                    TotalAttempts = metrics.LoginMetrics.TotalAttempts,
                    SuccessfulLogins = metrics.LoginMetrics.SuccessfulLogins,
                    FailedLogins = metrics.LoginMetrics.FailedLogins,
                    SuccessRate = metrics.LoginMetrics.TotalAttempts > 0
                        ? (double)metrics.LoginMetrics.SuccessfulLogins / metrics.LoginMetrics.TotalAttempts
                        : 0,
                    AverageLoginDuration = metrics.LoginMetrics.AverageLoginDuration,
                    LoginsByMethod = metrics.LoginMetrics.LoginsByMethod,
                    PeakLoginTime = DateTime.UtcNow.AddHours(-2), // Would be calculated from time-series data
                    UniqueUsers = 0 // Would be calculated from actual data
                };

                return Ok(CreateResponse(stats, "Login statistics retrieved successfully"));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GetLoginStatistics");
            }
        }

        /// <summary>
        /// Get security events
        /// </summary>
        [HttpGet("metrics/security")]
        [ProducesResponseType(typeof(ApiResponse<SecurityEventSummary>), 200)]
        public async Task<IActionResult> GetSecurityEvents(
            [FromQuery] int hours = 24)
        {
            try
            {
                var metrics = await _metricsCollector.GetMetricsAsync();

                var summary = new SecurityEventSummary
                {
                    Period = $"Last {hours} hours",
                    AccountLockouts = metrics.SecurityMetrics.AccountLockouts,
                    RateLimitHits = metrics.SecurityMetrics.RateLimitHits,
                    PasswordResets = metrics.SecurityMetrics.PasswordResets,
                    EmailVerifications = metrics.SecurityMetrics.EmailVerifications,
                    SecurityEventsByType = metrics.SecurityMetrics.SecurityEventsByType,
                    TotalSecurityEvents = metrics.SecurityMetrics.SecurityEventsByType.Values.Sum(),
                    CriticalEvents = metrics.SecurityMetrics.SecurityEventsByType
                        .Where(e => e.Key.Contains("critical") || e.Key.Contains("theft"))
                        .Sum(e => e.Value)
                };

                return Ok(CreateResponse(summary, "Security events retrieved successfully"));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GetSecurityEvents");
            }
        }

        /// <summary>
        /// Get token statistics
        /// </summary>
        [HttpGet("metrics/tokens")]
        [ProducesResponseType(typeof(ApiResponse<TokenStatistics>), 200)]
        public async Task<IActionResult> GetTokenStatistics()
        {
            try
            {
                var metrics = await _metricsCollector.GetMetricsAsync();

                var stats = new TokenStatistics
                {
                    AccessTokensGenerated = metrics.TokenMetrics.AccessTokensGenerated,
                    RefreshTokensGenerated = metrics.TokenMetrics.RefreshTokensGenerated,
                    TokenValidations = metrics.TokenMetrics.TokenValidations,
                    TokenValidationFailures = metrics.TokenMetrics.TokenValidationFailures,
                    ValidationFailureRate = metrics.TokenMetrics.TokenValidations > 0
                        ? (double)metrics.TokenMetrics.TokenValidationFailures / metrics.TokenMetrics.TokenValidations
                        : 0,
                    AverageGenerationTime = metrics.TokenMetrics.AverageGenerationTime,
                    AverageValidationTime = metrics.TokenMetrics.AverageValidationTime,
                    ActiveSessions = 0, // Would be calculated from session store
                    RevokedTokens = 0 // Would be calculated from blacklist
                };

                return Ok(CreateResponse(stats, "Token statistics retrieved successfully"));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GetTokenStatistics");
            }
        }

        /// <summary>
        /// Get MFA statistics
        /// </summary>
        [HttpGet("metrics/mfa")]
        [ProducesResponseType(typeof(ApiResponse<MfaStatistics>), 200)]
        public async Task<IActionResult> GetMfaStatistics()
        {
            try
            {
                var metrics = await _metricsCollector.GetMetricsAsync();

                var stats = new MfaStatistics
                {
                    TotalAttempts = metrics.MfaMetrics.TotalAttempts,
                    SuccessfulVerifications = metrics.MfaMetrics.SuccessfulVerifications,
                    FailedVerifications = metrics.MfaMetrics.FailedVerifications,
                    SuccessRate = metrics.MfaMetrics.TotalAttempts > 0
                        ? (double)metrics.MfaMetrics.SuccessfulVerifications / metrics.MfaMetrics.TotalAttempts
                        : 0,
                    VerificationsByMethod = metrics.MfaMetrics.VerificationsByMethod,
                    MostUsedMethod = metrics.MfaMetrics.VerificationsByMethod?
                        .OrderByDescending(m => m.Value)
                        .FirstOrDefault().Key ?? "none",
                    EnabledUsers = 0 // Would be calculated from user data
                };

                return Ok(CreateResponse(stats, "MFA statistics retrieved successfully"));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GetMfaStatistics");
            }
        }

        /// <summary>
        /// Get real-time dashboard data
        /// </summary>
        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(ApiResponse<DashboardData>), 200)]
        public async Task<IActionResult> GetDashboardData()
        {
            try
            {
                var metrics = await _metricsCollector.GetMetricsAsync();
                var health = await _healthCheckService.CheckHealthAsync();

                var dashboard = new DashboardData
                {
                    Timestamp = DateTime.UtcNow,
                    HealthStatus = health.Status.ToString(),
                    ActiveUsers = 0, // Would be calculated from session data
                    ActiveSessions = 0, // Would be calculated from session store
                    LoginActivity = new LoginActivity
                    {
                        Last5Minutes = 0, // Would be calculated from time-series data
                        Last15Minutes = 0,
                        LastHour = metrics.LoginMetrics.TotalAttempts,
                        FailureRate = metrics.LoginMetrics.TotalAttempts > 0
                            ? (double)metrics.LoginMetrics.FailedLogins / metrics.LoginMetrics.TotalAttempts
                            : 0
                    },
                    SecurityAlerts = new SecurityAlerts
                    {
                        Critical = 0, // Would be calculated from recent events
                        High = 0,
                        Medium = 0,
                        Low = 0
                    },
                    SystemPerformance = new SystemPerformance
                    {
                        AverageResponseTime = metrics.LoginMetrics.AverageLoginDuration,
                        TokenGenerationTime = metrics.TokenMetrics.AverageGenerationTime,
                        TokenValidationTime = metrics.TokenMetrics.AverageValidationTime,
                        CacheHitRate = 0, // Would be calculated from cache metrics
                        ErrorRate = 0 // Would be calculated from error logs
                    }
                };

                return Ok(CreateResponse(dashboard, "Dashboard data retrieved successfully"));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GetDashboardData");
            }
        }

        /// <summary>
        /// Export metrics in Prometheus format
        /// </summary>
        [HttpGet("metrics/export")]
        [Produces("text/plain")]
        [AllowAnonymous] // Allow Prometheus to scrape without auth
        public async Task<IActionResult> ExportMetrics()
        {
            try
            {
                var metrics = await _metricsCollector.GetMetricsAsync();
                var prometheusFormat = new System.Text.StringBuilder();

                // Login metrics
                prometheusFormat.AppendLine("# HELP auth_login_total Total number of login attempts");
                prometheusFormat.AppendLine("# TYPE auth_login_total counter");
                prometheusFormat.AppendLine($"auth_login_total{{status=\"success\"}} {metrics.LoginMetrics.SuccessfulLogins}");
                prometheusFormat.AppendLine($"auth_login_total{{status=\"failure\"}} {metrics.LoginMetrics.FailedLogins}");

                // Token metrics
                prometheusFormat.AppendLine("# HELP auth_token_generated_total Total number of tokens generated");
                prometheusFormat.AppendLine("# TYPE auth_token_generated_total counter");
                prometheusFormat.AppendLine($"auth_token_generated_total{{type=\"access\"}} {metrics.TokenMetrics.AccessTokensGenerated}");
                prometheusFormat.AppendLine($"auth_token_generated_total{{type=\"refresh\"}} {metrics.TokenMetrics.RefreshTokensGenerated}");

                // Security metrics
                prometheusFormat.AppendLine("# HELP auth_security_events_total Total number of security events");
                prometheusFormat.AppendLine("# TYPE auth_security_events_total counter");
                prometheusFormat.AppendLine($"auth_security_events_total{{type=\"lockout\"}} {metrics.SecurityMetrics.AccountLockouts}");
                prometheusFormat.AppendLine($"auth_security_events_total{{type=\"ratelimit\"}} {metrics.SecurityMetrics.RateLimitHits}");

                // Response time metrics
                prometheusFormat.AppendLine("# HELP auth_operation_duration_seconds Operation duration in seconds");
                prometheusFormat.AppendLine("# TYPE auth_operation_duration_seconds histogram");
                prometheusFormat.AppendLine($"auth_operation_duration_seconds{{operation=\"login\"}} {metrics.LoginMetrics.AverageLoginDuration / 1000.0}");
                prometheusFormat.AppendLine($"auth_operation_duration_seconds{{operation=\"token_generation\"}} {metrics.TokenMetrics.AverageGenerationTime / 1000.0}");
                prometheusFormat.AppendLine($"auth_operation_duration_seconds{{operation=\"token_validation\"}} {metrics.TokenMetrics.AverageValidationTime / 1000.0}");

                return Content(prometheusFormat.ToString(), "text/plain");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to export metrics");
                return StatusCode(500, "Failed to export metrics");
            }
        }
    }

    // Response DTOs
    public class HealthCheckResponse
    {
        public string Status { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public List<HealthCheckEntry> Entries { get; set; }
    }

    public class HealthCheckEntry
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public TimeSpan Duration { get; set; }
        public IReadOnlyDictionary<string, object> Data { get; set; }
        public List<string> Tags { get; set; }
    }

    public class LoginStatistics
    {
        public string Period { get; set; }
        public long TotalAttempts { get; set; }
        public long SuccessfulLogins { get; set; }
        public long FailedLogins { get; set; }
        public double SuccessRate { get; set; }
        public double AverageLoginDuration { get; set; }
        public Dictionary<string, long> LoginsByMethod { get; set; }
        public DateTime PeakLoginTime { get; set; }
        public long UniqueUsers { get; set; }
    }

    public class SecurityEventSummary
    {
        public string Period { get; set; }
        public long AccountLockouts { get; set; }
        public long RateLimitHits { get; set; }
        public long PasswordResets { get; set; }
        public long EmailVerifications { get; set; }
        public Dictionary<string, long> SecurityEventsByType { get; set; }
        public long TotalSecurityEvents { get; set; }
        public long CriticalEvents { get; set; }
    }

    public class TokenStatistics
    {
        public long AccessTokensGenerated { get; set; }
        public long RefreshTokensGenerated { get; set; }
        public long TokenValidations { get; set; }
        public long TokenValidationFailures { get; set; }
        public double ValidationFailureRate { get; set; }
        public double AverageGenerationTime { get; set; }
        public double AverageValidationTime { get; set; }
        public long ActiveSessions { get; set; }
        public long RevokedTokens { get; set; }
    }

    public class MfaStatistics
    {
        public long TotalAttempts { get; set; }
        public long SuccessfulVerifications { get; set; }
        public long FailedVerifications { get; set; }
        public double SuccessRate { get; set; }
        public Dictionary<string, long> VerificationsByMethod { get; set; }
        public string MostUsedMethod { get; set; }
        public long EnabledUsers { get; set; }
    }

    public class DashboardData
    {
        public DateTime Timestamp { get; set; }
        public string HealthStatus { get; set; }
        public long ActiveUsers { get; set; }
        public long ActiveSessions { get; set; }
        public LoginActivity LoginActivity { get; set; }
        public SecurityAlerts SecurityAlerts { get; set; }
        public SystemPerformance SystemPerformance { get; set; }
    }

    public class LoginActivity
    {
        public long Last5Minutes { get; set; }
        public long Last15Minutes { get; set; }
        public long LastHour { get; set; }
        public double FailureRate { get; set; }
    }

    public class SecurityAlerts
    {
        public int Critical { get; set; }
        public int High { get; set; }
        public int Medium { get; set; }
        public int Low { get; set; }
    }

    public class SystemPerformance
    {
        public double AverageResponseTime { get; set; }
        public double TokenGenerationTime { get; set; }
        public double TokenValidationTime { get; set; }
        public double CacheHitRate { get; set; }
        public double ErrorRate { get; set; }
    }
}
