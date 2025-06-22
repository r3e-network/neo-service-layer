using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NeoServiceLayer.Infrastructure.Security;

/// <summary>
/// Extension methods for configuring security logging and monitoring.
/// </summary>
public static class SecurityLoggingExtensions
{
    /// <summary>
    /// Adds security logging services to the service collection.
    /// </summary>
    public static IServiceCollection AddSecurityLogging(this IServiceCollection services, IConfiguration configuration)
    {
        // Register security logger
        services.AddSingleton<ISecurityLogger, SecurityLogger>();

        // Register security monitoring configuration
        var monitoringConfig = configuration.GetSection("Security:Monitoring").Get<SecurityMonitoringConfiguration>()
            ?? new SecurityMonitoringConfiguration();
        services.AddSingleton(monitoringConfig);

        // Register security monitoring service
        services.AddHostedService<SecurityMonitoringService>();

        return services;
    }

    /// <summary>
    /// Adds security logging with custom configuration.
    /// </summary>
    public static IServiceCollection AddSecurityLogging(this IServiceCollection services,
        Action<SecurityMonitoringConfiguration> configureOptions)
    {
        // Register security logger
        services.AddSingleton<ISecurityLogger, SecurityLogger>();

        // Configure security monitoring
        var config = new SecurityMonitoringConfiguration();
        configureOptions(config);
        services.AddSingleton(config);

        // Register security monitoring service
        services.AddHostedService<SecurityMonitoringService>();

        return services;
    }

    /// <summary>
    /// Records a security event using the current request context.
    /// </summary>
    public static void RecordSecurityEventWithContext(this ISecurityLogger securityLogger,
        SecurityEventType eventType,
        string message,
        string? clientId = null,
        string? ipAddress = null,
        string? sessionId = null,
        Dictionary<string, object>? additionalData = null)
    {
        var metadata = new SecurityEventMetadata
        {
            ClientId = clientId,
            IpAddress = ipAddress,
            SessionId = sessionId,
            AdditionalData = additionalData
        };

        securityLogger.RecordSecurityEvent(eventType, message, metadata);
    }

    /// <summary>
    /// Records a failed validation with security context.
    /// </summary>
    public static void RecordValidationFailureWithContext(this ISecurityLogger securityLogger,
        string validationType,
        string details,
        string? clientId = null,
        string? ipAddress = null,
        Dictionary<string, object>? validationErrors = null)
    {
        var metadata = new SecurityEventMetadata
        {
            ClientId = clientId,
            IpAddress = ipAddress,
            AdditionalData = validationErrors ?? new Dictionary<string, object>()
        };

        securityLogger.RecordValidationFailure(validationType, details, metadata);
    }

    /// <summary>
    /// Records a suspicious activity with enhanced details.
    /// </summary>
    public static void RecordSuspiciousActivityWithDetails(this ISecurityLogger securityLogger,
        string description,
        SuspiciousActivityType activityType,
        string? clientId = null,
        string? ipAddress = null,
        string? userAgent = null,
        Dictionary<string, object>? evidence = null)
    {
        var metadata = new SecurityEventMetadata
        {
            ClientId = clientId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            AdditionalData = evidence ?? new Dictionary<string, object>()
        };

        securityLogger.RecordSuspiciousActivity(description, activityType, metadata);
    }

    /// <summary>
    /// Records a batch of security events.
    /// </summary>
    public static void RecordSecurityEventBatch(this ISecurityLogger securityLogger,
        IEnumerable<(SecurityEventType eventType, string message, SecurityEventMetadata? metadata)> events)
    {
        foreach (var (eventType, message, metadata) in events)
        {
            securityLogger.RecordSecurityEvent(eventType, message, metadata);
        }
    }

    /// <summary>
    /// Gets a security event summary for reporting.
    /// </summary>
    public static SecurityEventSummary GetSecurityEventSummary(this ISecurityLogger securityLogger,
        TimeSpan period,
        SecurityEventType? filterByType = null)
    {
        var endTime = DateTime.UtcNow;
        var startTime = endTime - period;
        var stats = securityLogger.GetStatistics(startTime, endTime);
        var recentEvents = securityLogger.GetRecentEvents(100, filterByType);

        return new SecurityEventSummary
        {
            Period = period,
            Statistics = stats,
            TopEvents = recentEvents.Take(10).ToList(),
            GeneratedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Summary of security events for reporting.
/// </summary>
public class SecurityEventSummary
{
    public TimeSpan Period { get; set; }
    public SecurityEventStatistics Statistics { get; set; } = new();
    public List<SecurityEvent> TopEvents { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}
