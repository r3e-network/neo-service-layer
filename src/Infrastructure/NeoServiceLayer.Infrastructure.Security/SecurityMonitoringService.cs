using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;


namespace NeoServiceLayer.Infrastructure.Security;

/// <summary>
/// Background service that monitors security events and triggers alerts.
/// </summary>
public class SecurityMonitoringService : BackgroundService
{
    private readonly ISecurityLogger _securityLogger;
    //private readonly IMonitoringService _monitoringService;
    //private readonly INotificationService _notificationService;
    private readonly ILogger<SecurityMonitoringService> Logger;
    private readonly SecurityMonitoringConfiguration _configuration;
    private readonly ConcurrentDictionary<string, SecurityThreatDetector> _threatDetectors;
    private readonly ConcurrentDictionary<string, DateTime> _alertCooldowns;

    public SecurityMonitoringService(
        ISecurityLogger securityLogger,
        // IMonitoringService monitoringService,
        // INotificationService notificationService,
        ILogger<SecurityMonitoringService> logger,
        SecurityMonitoringConfiguration configuration)
    {
        _securityLogger = securityLogger ?? throw new ArgumentNullException(nameof(securityLogger));
        // _monitoringService = monitoringService ?? throw new ArgumentNullException(nameof(monitoringService));
        // _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _threatDetectors = new ConcurrentDictionary<string, SecurityThreatDetector>();
        _alertCooldowns = new ConcurrentDictionary<string, DateTime>();

        InitializeThreatDetectors();
    }

    /// <summary>
    /// Executes the security monitoring background service.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token to stop the service.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.LogInformation("Security monitoring service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await AnalyzeSecurityEventsAsync();
                await Task.Delay(_configuration.MonitoringInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in security monitoring service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        Logger.LogInformation("Security monitoring service stopped");
    }

    /// <summary>
    /// Analyzes security events within the configured time window and detects potential threats.
    /// </summary>
    /// <returns>A task representing the asynchronous analysis operation.</returns>
    private async Task AnalyzeSecurityEventsAsync()
    {
        var endTime = DateTime.UtcNow;
        var startTime = endTime.AddMinutes(-_configuration.AnalysisWindowMinutes);

        var statistics = _securityLogger.GetStatistics(startTime, endTime);
        var recentEvents = _securityLogger.GetRecentEvents(1000).ToList();

        // Run threat detection
        var threats = new List<SecurityThreat>();
        foreach (var detector in _threatDetectors.Values)
        {
            var detectedThreats = await detector.DetectThreatsAsync(statistics, recentEvents);
            threats.AddRange(detectedThreats);
        }

        // Process detected threats
        foreach (var threat in threats)
        {
            await ProcessThreatAsync(threat);
        }

        // Log monitoring metrics
        LogMonitoringMetrics(statistics, threats);
    }

    /// <summary>
    /// Processes a detected security threat by logging, alerting, and notifying as appropriate.
    /// </summary>
    /// <param name="threat">The security threat to process.</param>
    /// <returns>A task representing the asynchronous processing operation.</returns>
    private async Task ProcessThreatAsync(SecurityThreat threat)
    {
        Logger.LogWarning("Security threat detected: {ThreatType} - {Description} (Severity: {Severity})",
            threat.ThreatType, threat.Description, threat.Severity);

        // Check cooldown
        var cooldownKey = $"{threat.ThreatType}:{threat.AffectedResource ?? "global"}";
        if (_alertCooldowns.TryGetValue(cooldownKey, out var lastAlertTime))
        {
            if (DateTime.UtcNow - lastAlertTime < _configuration.AlertCooldownPeriod)
            {
                Logger.LogDebug("Skipping alert for {ThreatType} due to cooldown", threat.ThreatType);
                return;
            }
        }

        // Create monitoring alert (temporarily disabled due to missing Alert type)
        // var alert = new Alert
        // {
        //     Id = Guid.NewGuid().ToString(),
        //     Name = $"Security Alert: {threat.ThreatType}",
        //     Description = threat.Description,
        //     Severity = MapThreatSeverityToAlertSeverity(threat.Severity),
        //     Source = "SecurityMonitoringService",
        //     Timestamp = DateTime.UtcNow,
        //     Tags = new Dictionary<string, string>
        //     {
        //         ["ThreatType"] = threat.ThreatType.ToString(),
        //         ["AffectedResource"] = threat.AffectedResource ?? "Unknown",
        //         ["ClientId"] = threat.ClientId ?? "Unknown"
        //     }
        // };

        // await _monitoringService.RaiseAlertAsync(alert, BlockchainType.NeoX);

        // Send notification if critical
        if (threat.Severity >= ThreatSeverity.High && _configuration.EnableNotifications)
        {
            await SendSecurityNotificationAsync(threat);
        }

        // Update cooldown
        _alertCooldowns.AddOrUpdate(cooldownKey, DateTime.UtcNow, (_, _) => DateTime.UtcNow);

        // Record the threat detection
        _securityLogger.RecordSecurityEvent(
            SecurityEventType.SecurityViolation,
            $"Security threat detected: {threat.ThreatType}",
            new SecurityEventMetadata
            {
                ClientId = threat.ClientId,
                IpAddress = threat.IpAddress,
                AdditionalData = new Dictionary<string, object>
                {
                    ["ThreatType"] = threat.ThreatType.ToString(),
                    ["Severity"] = threat.Severity.ToString(),
                    ["Evidence"] = threat.Evidence
                }
            });
    }

    private async Task SendSecurityNotificationAsync(SecurityThreat threat)
    {
        // Temporarily disabled due to missing NotificationType and INotificationService dependencies
        // var notification = new Notification
        // {
        //     Type = NotificationType.Critical,
        //     Subject = $"Security Alert: {threat.ThreatType}",
        //     Body = $"A {threat.Severity} severity security threat has been detected.\n\n" +
        //            $"Type: {threat.ThreatType}\n" +
        //            $"Description: {threat.Description}\n" +
        //            $"Affected Resource: {threat.AffectedResource ?? "Unknown"}\n" +
        //            $"Client: {threat.ClientId ?? "Unknown"}\n" +
        //            $"IP Address: {threat.IpAddress ?? "Unknown"}\n\n" +
        //            $"Evidence: {string.Join(", ", threat.Evidence)}",
        //     Recipients = _configuration.SecurityNotificationRecipients,
        //     Metadata = new Dictionary<string, object>
        //     {
        //         ["ThreatType"] = threat.ThreatType.ToString(),
        //         ["Severity"] = threat.Severity.ToString(),
        //         ["Timestamp"] = DateTime.UtcNow
        //     }
        // };

        // await _notificationService.SendNotificationAsync(notification);
        await Task.CompletedTask; // Placeholder
    }

    private void LogMonitoringMetrics(SecurityEventStatistics statistics, List<SecurityThreat> threats)
    {
        var metrics = new Dictionary<string, double>
        {
            ["security.events.total"] = statistics.TotalEvents,
            ["security.auth.success"] = statistics.SuccessfulAuthentications,
            ["security.auth.failed"] = statistics.FailedAuthentications,
            ["security.authz.failures"] = statistics.AuthorizationFailures,
            ["security.ratelimit.violations"] = statistics.RateLimitViolations,
            ["security.suspicious.activities"] = statistics.SuspiciousActivities,
            ["security.threats.detected"] = threats.Count
        };

        // Record security metrics using APM system
        try
        {
            foreach (var metric in metrics)
            {
                // Use the APM system to record security metrics
                // TODO: Implement APM integration
                // // ApplicationPerformanceMonitoring // TODO: Fix this reference.RecordRequest(
                //     "SECURITY_METRIC",
                //     metric.Key,
                //     0.001, // Minimal duration for metrics
                //     200);

                Logger.LogDebug("Security metric {MetricName}: {MetricValue}", metric.Key, metric.Value);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to record security metrics to APM system");
        }
    }

    private void InitializeThreatDetectors()
    {
        // Brute force detector
        _threatDetectors.TryAdd("BruteForce", new BruteForceDetector(_configuration));

        // Rate limit abuse detector
        _threatDetectors.TryAdd("RateLimitAbuse", new RateLimitAbuseDetector(_configuration));

        // Suspicious pattern detector
        _threatDetectors.TryAdd("SuspiciousPattern", new SuspiciousPatternDetector(_configuration));

        // Authentication anomaly detector
        _threatDetectors.TryAdd("AuthAnomaly", new AuthenticationAnomalyDetector(_configuration));

        // Data exfiltration detector
        _threatDetectors.TryAdd("DataExfiltration", new DataExfiltrationDetector(_configuration));
    }

    private AlertSeverity MapThreatSeverityToAlertSeverity(ThreatSeverity threatSeverity)
    {
        return threatSeverity switch
        {
            ThreatSeverity.Low => AlertSeverity.Low,
            ThreatSeverity.Medium => AlertSeverity.Warning,
            ThreatSeverity.High => AlertSeverity.High,
            ThreatSeverity.Critical => AlertSeverity.Critical,
            _ => AlertSeverity.Warning
        };
    }
}

/// <summary>
/// Configuration for security monitoring.
/// </summary>
public class SecurityMonitoringConfiguration
{
    public TimeSpan MonitoringInterval { get; set; } = TimeSpan.FromMinutes(1);
    public int AnalysisWindowMinutes { get; set; } = 15;
    public TimeSpan AlertCooldownPeriod { get; set; } = TimeSpan.FromMinutes(30);
    public bool EnableNotifications { get; set; } = true;
    public string[] SecurityNotificationRecipients { get; set; } = Array.Empty<string>();

    // Threat detection thresholds
    public int BruteForceThreshold { get; set; } = 5;
    public int RateLimitAbuseThreshold { get; set; } = 10;
    public double AuthenticationFailureRateThreshold { get; set; } = 0.5;
    public int DataExfiltrationThreshold { get; set; } = 1000;
}

/// <summary>
/// Represents a detected security threat.
/// </summary>
public class SecurityThreat
{
    public ThreatType ThreatType { get; set; }
    public ThreatSeverity Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ClientId { get; set; }
    public string? IpAddress { get; set; }
    public string? AffectedResource { get; set; }
    public List<string> Evidence { get; set; } = new();
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Types of security threats.
/// </summary>
public enum ThreatType
{
    BruteForce,
    RateLimitAbuse,
    SuspiciousPattern,
    AuthenticationAnomaly,
    DataExfiltration,
    PrivilegeEscalation,
    UnauthorizedAccess,
    MaliciousPayload
}

/// <summary>
/// Severity levels for threats.
/// </summary>
public enum ThreatSeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Severity levels for alerts.
/// </summary>
public enum AlertSeverity
{
    Low,
    Warning,
    High,
    Critical
}

/// <summary>
/// Base class for threat detectors.
/// </summary>
public abstract class SecurityThreatDetector
{
    protected readonly SecurityMonitoringConfiguration Configuration;

    protected SecurityThreatDetector(SecurityMonitoringConfiguration configuration)
    {
        Configuration = configuration;
    }

    public abstract Task<List<SecurityThreat>> DetectThreatsAsync(
        SecurityEventStatistics statistics,
        List<SecurityEvent> recentEvents);
}

/// <summary>
/// Detects brute force attacks.
/// </summary>
public class BruteForceDetector : SecurityThreatDetector
{
    public BruteForceDetector(SecurityMonitoringConfiguration configuration) : base(configuration) { }

    public override Task<List<SecurityThreat>> DetectThreatsAsync(
        SecurityEventStatistics statistics,
        List<SecurityEvent> recentEvents)
    {
        var threats = new List<SecurityThreat>();

        // Group failed authentications by client/IP
        var failedAuthGroups = recentEvents
            .Where(e => e.EventType == SecurityEventType.Authentication &&
                       e.Metadata?.AdditionalData?.ContainsKey("Success") == true &&
                       !(bool)(e.Metadata.AdditionalData["Success"] ?? true))
            .GroupBy(e => e.Metadata?.ClientId ?? e.Metadata?.IpAddress ?? "Unknown")
            .Where(g => g.Count() >= Configuration.BruteForceThreshold);

        foreach (var group in failedAuthGroups)
        {
            var threat = new SecurityThreat
            {
                ThreatType = ThreatType.BruteForce,
                Severity = group.Count() >= Configuration.BruteForceThreshold * 2
                    ? ThreatSeverity.High
                    : ThreatSeverity.Medium,
                Description = $"Possible brute force attack detected with {group.Count()} failed authentication attempts",
                ClientId = group.First().Metadata?.ClientId,
                IpAddress = group.First().Metadata?.IpAddress,
                Evidence = group.Select(e => $"Failed auth at {e.Timestamp:HH:mm:ss}").ToList()
            };

            threats.Add(threat);
        }

        return Task.FromResult(threats);
    }
}

/// <summary>
/// Detects rate limit abuse.
/// </summary>
public class RateLimitAbuseDetector : SecurityThreatDetector
{
    public RateLimitAbuseDetector(SecurityMonitoringConfiguration configuration) : base(configuration) { }

    public override Task<List<SecurityThreat>> DetectThreatsAsync(
        SecurityEventStatistics statistics,
        List<SecurityEvent> recentEvents)
    {
        var threats = new List<SecurityThreat>();

        // Check for excessive rate limit violations
        if (statistics.RateLimitViolations >= Configuration.RateLimitAbuseThreshold)
        {
            var violationEvents = recentEvents
                .Where(e => e.EventType == SecurityEventType.RateLimit)
                .ToList();

            var topOffenders = violationEvents
                .GroupBy(e => e.Metadata?.ClientId ?? e.Metadata?.IpAddress ?? "Unknown")
                .OrderByDescending(g => g.Count())
                .Take(3);

            foreach (var offender in topOffenders)
            {
                var threat = new SecurityThreat
                {
                    ThreatType = ThreatType.RateLimitAbuse,
                    Severity = ThreatSeverity.Medium,
                    Description = $"Client exceeded rate limits {offender.Count()} times",
                    ClientId = offender.First().Metadata?.ClientId,
                    IpAddress = offender.First().Metadata?.IpAddress,
                    Evidence = offender
                        .Select(e => $"Rate limit violation at {e.Timestamp:HH:mm:ss}")
                        .Take(5)
                        .ToList()
                };

                threats.Add(threat);
            }
        }

        return Task.FromResult(threats);
    }
}

/// <summary>
/// Detects suspicious patterns.
/// </summary>
public class SuspiciousPatternDetector : SecurityThreatDetector
{
    public SuspiciousPatternDetector(SecurityMonitoringConfiguration configuration) : base(configuration) { }

    public override Task<List<SecurityThreat>> DetectThreatsAsync(
        SecurityEventStatistics statistics,
        List<SecurityEvent> recentEvents)
    {
        var threats = new List<SecurityThreat>();

        // Look for SQL injection attempts
        var sqlInjectionEvents = recentEvents
            .Where(e => e.EventType == SecurityEventType.SuspiciousActivity &&
                       e.Metadata?.AdditionalData?.ContainsKey("ActivityType") == true &&
                       e.Metadata.AdditionalData["ActivityType"]?.ToString() == "SqlInjection")
            .ToList();

        if (sqlInjectionEvents.Any())
        {
            var threat = new SecurityThreat
            {
                ThreatType = ThreatType.MaliciousPayload,
                Severity = ThreatSeverity.High,
                Description = $"SQL injection attempts detected ({sqlInjectionEvents.Count} occurrences)",
                Evidence = sqlInjectionEvents
                    .Select(e => e.Message)
                    .Take(3)
                    .ToList()
            };

            threats.Add(threat);
        }

        // Look for path traversal attempts
        var pathTraversalEvents = recentEvents
            .Where(e => e.EventType == SecurityEventType.ValidationFailure &&
                       e.Message.Contains("path", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (pathTraversalEvents.Count >= 3)
        {
            var threat = new SecurityThreat
            {
                ThreatType = ThreatType.UnauthorizedAccess,
                Severity = ThreatSeverity.Medium,
                Description = "Multiple path traversal attempts detected",
                Evidence = pathTraversalEvents
                    .Select(e => e.Message)
                    .Take(3)
                    .ToList()
            };

            threats.Add(threat);
        }

        return Task.FromResult(threats);
    }
}

/// <summary>
/// Detects authentication anomalies.
/// </summary>
public class AuthenticationAnomalyDetector : SecurityThreatDetector
{
    public AuthenticationAnomalyDetector(SecurityMonitoringConfiguration configuration) : base(configuration) { }

    public override Task<List<SecurityThreat>> DetectThreatsAsync(
        SecurityEventStatistics statistics,
        List<SecurityEvent> recentEvents)
    {
        var threats = new List<SecurityThreat>();

        // Check for high authentication failure rate
        var totalAuth = statistics.SuccessfulAuthentications + statistics.FailedAuthentications;
        if (totalAuth > 0)
        {
            var failureRate = (double)statistics.FailedAuthentications / totalAuth;
            if (failureRate > Configuration.AuthenticationFailureRateThreshold)
            {
                var threat = new SecurityThreat
                {
                    ThreatType = ThreatType.AuthenticationAnomaly,
                    Severity = failureRate > 0.8 ? ThreatSeverity.High : ThreatSeverity.Medium,
                    Description = $"High authentication failure rate detected: {failureRate:P}",
                    Evidence = new List<string>
                    {
                        $"Failed: {statistics.FailedAuthentications}",
                        $"Successful: {statistics.SuccessfulAuthentications}",
                        $"Failure rate: {failureRate:P}"
                    }
                };

                threats.Add(threat);
            }
        }

        // Check for IP address changes during sessions
        var ipChangeEvents = recentEvents
            .Where(e => e.EventType == SecurityEventType.SuspiciousActivity &&
                       e.Message.Contains("IP address change", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (ipChangeEvents.Count >= 2)
        {
            var threat = new SecurityThreat
            {
                ThreatType = ThreatType.AuthenticationAnomaly,
                Severity = ThreatSeverity.Medium,
                Description = "Multiple IP address changes detected during active sessions",
                Evidence = ipChangeEvents
                    .Select(e => e.Message)
                    .Take(3)
                    .ToList()
            };

            threats.Add(threat);
        }

        return Task.FromResult(threats);
    }
}

/// <summary>
/// Detects potential data exfiltration.
/// </summary>
public class DataExfiltrationDetector : SecurityThreatDetector
{
    public DataExfiltrationDetector(SecurityMonitoringConfiguration configuration) : base(configuration) { }

    public override Task<List<SecurityThreat>> DetectThreatsAsync(
        SecurityEventStatistics statistics,
        List<SecurityEvent> recentEvents)
    {
        var threats = new List<SecurityThreat>();

        // Look for excessive data access
        var dataAccessGroups = recentEvents
            .Where(e => e.EventType == SecurityEventType.DataAccess &&
                       e.Metadata?.AdditionalData?.ContainsKey("AccessType") == true &&
                       (e.Metadata.AdditionalData["AccessType"]?.ToString() == "Read" ||
                        e.Metadata.AdditionalData["AccessType"]?.ToString() == "Export"))
            .GroupBy(e => e.Metadata?.ClientId ?? e.Metadata?.AdditionalData?["UserId"]?.ToString() ?? "Unknown")
            .Where(g => g.Count() >= Configuration.DataExfiltrationThreshold);

        foreach (var group in dataAccessGroups)
        {
            var threat = new SecurityThreat
            {
                ThreatType = ThreatType.DataExfiltration,
                Severity = group.Count() >= Configuration.DataExfiltrationThreshold * 2
                    ? ThreatSeverity.High
                    : ThreatSeverity.Medium,
                Description = $"Excessive data access detected: {group.Count()} operations",
                ClientId = group.First().Metadata?.ClientId,
                AffectedResource = "Data Storage",
                Evidence = group
                    .Select(e => $"Access at {e.Timestamp:HH:mm:ss}: {e.Metadata?.AdditionalData?["Resource"]}")
                    .Take(5)
                    .ToList()
            };

            threats.Add(threat);
        }

        return Task.FromResult(threats);
    }
}
