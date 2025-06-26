# Security Logging and Monitoring

## Overview

The Neo Service Layer includes comprehensive security logging and monitoring capabilities to detect and respond to security threats in real-time. This system provides centralized security event tracking, threat detection, and automated alerting.

## Features

### Security Event Logging

- **Authentication Events**: Track all authentication attempts (successful and failed)
- **Authorization Events**: Monitor authorization decisions and access denials
- **Rate Limiting**: Log rate limit violations and potential abuse
- **Suspicious Activities**: Detect and log potential security threats
- **Cryptographic Operations**: Track encryption, signing, and key management operations
- **Enclave Operations**: Monitor TEE/SGX enclave activities
- **Data Access**: Log all data access operations with user context
- **Configuration Changes**: Track all configuration modifications
- **Input Validation**: Log validation failures and potential attack patterns

### Threat Detection

The security monitoring service includes multiple threat detectors:

1. **Brute Force Detector**: Identifies repeated failed authentication attempts
2. **Rate Limit Abuse Detector**: Detects clients exceeding rate limits
3. **Suspicious Pattern Detector**: Identifies SQL injection, XSS, and path traversal attempts
4. **Authentication Anomaly Detector**: Detects unusual authentication patterns
5. **Data Exfiltration Detector**: Monitors for excessive data access patterns

### Real-time Monitoring

- Continuous analysis of security events
- Configurable monitoring intervals and analysis windows
- Automatic threat severity assessment
- Alert cooldown to prevent notification spam

## Configuration

### Basic Setup

Add security logging to your service configuration:

```csharp
services.AddNeoServiceLayer(configuration);
// Security logging is automatically included
```

### Custom Configuration

```csharp
services.AddSecurityLogging(options =>
{
    options.MonitoringInterval = TimeSpan.FromMinutes(1);
    options.AnalysisWindowMinutes = 15;
    options.EnableNotifications = true;
    options.BruteForceThreshold = 5;
    options.RateLimitAbuseThreshold = 10;
});
```

### Configuration File

Add security settings to your `appsettings.json`:

```json
{
  "Security": {
    "Monitoring": {
      "MonitoringInterval": "00:01:00",
      "AnalysisWindowMinutes": 15,
      "AlertCooldownPeriod": "00:30:00",
      "EnableNotifications": true,
      "SecurityNotificationRecipients": ["security@example.com"],
      "BruteForceThreshold": 5,
      "RateLimitAbuseThreshold": 10,
      "AuthenticationFailureRateThreshold": 0.5,
      "DataExfiltrationThreshold": 1000
    }
  }
}
```

## Usage in Services

### Using SecurityAwareServiceBase

Services can inherit from `SecurityAwareServiceBase` to get built-in security logging:

```csharp
public class MyService : SecurityAwareServiceBase, IMyService
{
    public MyService(ILogger<MyService> logger, ISecurityLogger securityLogger) 
        : base(logger, securityLogger)
    {
    }

    public async Task<Result> PerformSecureOperation(string userId, string data)
    {
        // Log authentication
        LogAuthenticationAttempt(userId, true);

        // Validate input with security logging
        if (!ValidateAndLogInput(data, ValidateData, "DataValidation", userId))
        {
            return Result.Fail("Invalid input");
        }

        // Execute with security context
        return await ExecuteWithSecurityLoggingAsync(
            async () => await ProcessDataAsync(data),
            "Process secure data",
            userId: userId
        );
    }

    private (bool isValid, string? error) ValidateData(string data)
    {
        if (string.IsNullOrEmpty(data))
            return (false, "Data cannot be empty");
            
        if (data.Contains("<script>", StringComparison.OrdinalIgnoreCase))
            return (false, "Potential XSS detected");
            
        return (true, null);
    }
}
```

### Direct Security Logger Usage

```csharp
public class CustomService
{
    private readonly ISecurityLogger _securityLogger;

    public CustomService(ISecurityLogger securityLogger)
    {
        _securityLogger = securityLogger;
    }

    public void LogSecurityEvent(string clientId)
    {
        // Log a custom security event
        _securityLogger.RecordSecurityEvent(
            SecurityEventType.DataAccess,
            "User accessed sensitive data",
            new SecurityEventMetadata
            {
                ClientId = clientId,
                IpAddress = "192.168.1.100",
                AdditionalData = new Dictionary<string, object>
                {
                    ["DataType"] = "Financial",
                    ["RecordCount"] = 150
                }
            }
        );

        // Log suspicious activity
        _securityLogger.RecordSuspiciousActivity(
            "Multiple failed login attempts from different IPs",
            SuspiciousActivityType.BruteForceAttempt,
            new SecurityEventMetadata { ClientId = clientId }
        );
    }
}
```

## Security Event Types

### Authentication Events
- Successful and failed login attempts
- Password changes
- Account lockouts
- Multi-factor authentication events

### Authorization Events
- Access granted/denied decisions
- Privilege escalation attempts
- Resource access violations

### Data Security Events
- Data access (read/write/delete)
- Bulk operations
- Export operations
- Sensitive data access

### System Security Events
- Configuration changes
- Security policy updates
- Encryption key operations
- Certificate operations

## Threat Response

### Automated Responses

When threats are detected, the system can:
- Raise alerts through the monitoring service
- Send notifications to security team
- Log detailed evidence for investigation
- Trigger automated response workflows

### Alert Severity Levels

- **Low**: Informational, no immediate action required
- **Medium**: Potential threat, investigation recommended
- **High**: Active threat detected, immediate action required
- **Critical**: Severe security breach, emergency response needed

## Security Statistics and Reporting

### Real-time Statistics

```csharp
// Get security statistics
var stats = securityLogger.GetStatistics(
    DateTime.UtcNow.AddHours(-1),
    DateTime.UtcNow
);

Console.WriteLine($"Failed authentications: {stats.FailedAuthentications}");
Console.WriteLine($"Rate limit violations: {stats.RateLimitViolations}");
Console.WriteLine($"Suspicious activities: {stats.SuspiciousActivities}");
```

### Security Event Query

```csharp
// Get recent security events
var recentEvents = securityLogger.GetRecentEvents(
    count: 100,
    filterByType: SecurityEventType.Authentication
);

// Get security summary
var summary = securityLogger.GetSecurityEventSummary(
    period: TimeSpan.FromDays(1),
    filterByType: SecurityEventType.SuspiciousActivity
);
```

## Best Practices

1. **Enable Security Logging**: Always enable security logging in production
2. **Configure Alerts**: Set up appropriate alert thresholds based on your traffic
3. **Regular Review**: Regularly review security logs and statistics
4. **Incident Response**: Have a clear incident response plan for security alerts
5. **Log Retention**: Configure appropriate retention periods for compliance
6. **Sensitive Data**: Never log sensitive data like passwords or keys
7. **Correlation**: Use correlation IDs to track related security events

## Integration with Monitoring Service

Security events are automatically integrated with the monitoring service:

```csharp
// Security metrics are automatically recorded
// Available metrics include:
// - security.events.total
// - security.auth.success
// - security.auth.failed
// - security.authz.failures
// - security.ratelimit.violations
// - security.suspicious.activities
// - security.threats.detected
```

## Compliance and Auditing

The security logging system supports compliance requirements:

- **Audit Trail**: Complete audit trail of all security events
- **Tamper-proof**: Events are immutable once logged
- **Retention**: Configurable retention periods
- **Export**: Export security logs for compliance reporting
- **Search**: Search and filter events for investigations

## Performance Considerations

- Security logging is designed to be lightweight
- Events are logged asynchronously to minimize impact
- Automatic cleanup of old events based on retention policy
- In-memory caching for recent events and statistics

## Troubleshooting

### Common Issues

1. **Missing Security Events**
   - Verify security logging is enabled
   - Check service inherits from SecurityAwareServiceBase
   - Ensure ISecurityLogger is registered

2. **Too Many Alerts**
   - Adjust threat detection thresholds
   - Increase alert cooldown period
   - Review and tune detection rules

3. **Performance Impact**
   - Reduce monitoring frequency
   - Adjust analysis window size
   - Enable sampling for high-volume events