using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Oracle.Models;

/// <summary>
/// Represents a date range for filtering.
/// </summary>
public class DateRange
{
    /// <summary>
    /// Gets or sets the start date.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Gets whether the range is valid.
    /// </summary>
    public bool IsValid => StartDate <= EndDate;
}

// Note: DataSource and OracleSubscription are defined in their respective files

/// <summary>
/// Represents authentication configuration for data sources.
/// </summary>
public class AuthenticationConfig
{
    /// <summary>
    /// Gets or sets the authentication type.
    /// </summary>
    public AuthenticationType Type { get; set; }

    /// <summary>
    /// Gets or sets the API key (for API key authentication).
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the API key header name.
    /// </summary>
    public string? ApiKeyHeader { get; set; } = "X-API-Key";

    /// <summary>
    /// Gets or sets the username (for basic authentication).
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the password (for basic authentication).
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the bearer token (for bearer token authentication).
    /// </summary>
    public string? BearerToken { get; set; }

    /// <summary>
    /// Gets or sets OAuth configuration.
    /// </summary>
    public OAuthConfig? OAuth { get; set; }

    /// <summary>
    /// Gets or sets custom authentication parameters.
    /// </summary>
    public Dictionary<string, string> CustomParameters { get; set; } = new();
}

/// <summary>
/// Represents OAuth configuration.
/// </summary>
public class OAuthConfig
{
    /// <summary>
    /// Gets or sets the OAuth client ID.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OAuth client secret.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token endpoint URL.
    /// </summary>
    public string TokenEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OAuth scope.
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// Gets or sets the grant type.
    /// </summary>
    public string GrantType { get; set; } = "client_credentials";
}

/// <summary>
/// Represents a validation rule for data sources.
/// </summary>
public class ValidationRule
{
    /// <summary>
    /// Gets or sets the rule identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rule name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rule type.
    /// </summary>
    public ValidationRuleType Type { get; set; }

    /// <summary>
    /// Gets or sets the field to validate.
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the validation criteria.
    /// </summary>
    public Dictionary<string, string> Criteria { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the rule is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the error message for validation failures.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Represents subscription metrics.
/// </summary>
public class SubscriptionMetrics
{
    /// <summary>
    /// Gets or sets the total number of updates.
    /// </summary>
    public int TotalUpdates { get; set; }

    /// <summary>
    /// Gets or sets the successful update count.
    /// </summary>
    public int SuccessfulUpdates { get; set; }

    /// <summary>
    /// Gets or sets the failed update count.
    /// </summary>
    public int FailedUpdates { get; set; }

    /// <summary>
    /// Gets or sets the average response time in milliseconds.
    /// </summary>
    public double AverageResponseTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the average latency in milliseconds.
    /// </summary>
    public double AverageLatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the success rate as a decimal between 0 and 1.
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime? LastUpdate { get; set; }

    /// <summary>
    /// Gets or sets the last successful update timestamp.
    /// </summary>
    public DateTime? LastSuccessAt { get; set; }

    /// <summary>
    /// Gets or sets the last failure timestamp.
    /// </summary>
    public DateTime? LastFailureAt { get; set; }

    /// <summary>
    /// Gets or sets the data quality score.
    /// </summary>
    public double DataQualityScore { get; set; }

    /// <summary>
    /// Gets or sets uptime percentage.
    /// </summary>
    public double UptimePercentage { get; set; }
}

/// <summary>
/// Represents subscription activity.
/// </summary>
public class SubscriptionActivity
{
    /// <summary>
    /// Gets or sets the activity identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the activity type.
    /// </summary>
    public ActivityType Type { get; set; }

    /// <summary>
    /// Gets or sets the activity timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the activity description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the activity was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the response time in milliseconds.
    /// </summary>
    public int? ResponseTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the error message if applicable.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional activity data.
    /// </summary>
    public Dictionary<string, string> Data { get; set; } = new();
}

/// <summary>
/// Represents data source health information.
/// </summary>
public class DataSourceHealth
{
    /// <summary>
    /// Gets or sets the health status.
    /// </summary>
    public HealthStatus Status { get; set; } = HealthStatus.Unknown;

    /// <summary>
    /// Gets or sets the last health check timestamp.
    /// </summary>
    public DateTime? LastHealthCheck { get; set; }

    /// <summary>
    /// Gets or sets the response time in milliseconds.
    /// </summary>
    public int? ResponseTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the uptime percentage.
    /// </summary>
    public double UptimePercentage { get; set; }

    /// <summary>
    /// Gets or sets health check error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the consecutive failure count.
    /// </summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// Gets or sets additional health metrics.
    /// </summary>
    public Dictionary<string, double> Metrics { get; set; } = new();
}

// Enums

/// <summary>
/// Data source type enumeration.
/// </summary>
public enum DataSourceType
{
    /// <summary>
    /// REST API data source.
    /// </summary>
    RestApi,

    /// <summary>
    /// WebSocket data source.
    /// </summary>
    WebSocket,

    /// <summary>
    /// GraphQL data source.
    /// </summary>
    GraphQL,

    /// <summary>
    /// RSS/Atom feed data source.
    /// </summary>
    RssFeed,

    /// <summary>
    /// Database data source.
    /// </summary>
    Database,

    /// <summary>
    /// File-based data source.
    /// </summary>
    File,

    /// <summary>
    /// External blockchain data source.
    /// </summary>
    Blockchain,

    /// <summary>
    /// Custom data source type.
    /// </summary>
    Custom
}

/// <summary>
/// Authentication type enumeration.
/// </summary>
public enum AuthenticationType
{
    /// <summary>
    /// No authentication required.
    /// </summary>
    None,

    /// <summary>
    /// API key authentication.
    /// </summary>
    ApiKey,

    /// <summary>
    /// Basic HTTP authentication.
    /// </summary>
    Basic,

    /// <summary>
    /// Bearer token authentication.
    /// </summary>
    Bearer,

    /// <summary>
    /// OAuth 2.0 authentication.
    /// </summary>
    OAuth2,

    /// <summary>
    /// Custom authentication method.
    /// </summary>
    Custom
}

/// <summary>
/// Subscription status enumeration.
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>
    /// Subscription is active and receiving updates.
    /// </summary>
    Active,

    /// <summary>
    /// Subscription is paused.
    /// </summary>
    Paused,

    /// <summary>
    /// Subscription has been cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Subscription has expired.
    /// </summary>
    Expired,

    /// <summary>
    /// Subscription has failed due to errors.
    /// </summary>
    Failed,

    /// <summary>
    /// Subscription is pending activation.
    /// </summary>
    Pending
}

/// <summary>
/// Validation rule type enumeration.
/// </summary>
public enum ValidationRuleType
{
    /// <summary>
    /// Required field validation.
    /// </summary>
    Required,

    /// <summary>
    /// Data type validation.
    /// </summary>
    DataType,

    /// <summary>
    /// Range validation.
    /// </summary>
    Range,

    /// <summary>
    /// Regular expression validation.
    /// </summary>
    Regex,

    /// <summary>
    /// Format validation.
    /// </summary>
    Format,

    /// <summary>
    /// Length validation.
    /// </summary>
    Length,

    /// <summary>
    /// Custom validation logic.
    /// </summary>
    Custom
}

/// <summary>
/// Activity type enumeration.
/// </summary>
public enum ActivityType
{
    /// <summary>
    /// Data update activity.
    /// </summary>
    DataUpdate,

    /// <summary>
    /// Subscription created.
    /// </summary>
    SubscriptionCreated,

    /// <summary>
    /// Subscription modified.
    /// </summary>
    SubscriptionModified,

    /// <summary>
    /// Subscription paused.
    /// </summary>
    SubscriptionPaused,

    /// <summary>
    /// Subscription resumed.
    /// </summary>
    SubscriptionResumed,

    /// <summary>
    /// Subscription cancelled.
    /// </summary>
    SubscriptionCancelled,

    /// <summary>
    /// Error occurred.
    /// </summary>
    Error,

    /// <summary>
    /// Webhook notification sent.
    /// </summary>
    WebhookNotification,

    /// <summary>
    /// Health check performed.
    /// </summary>
    HealthCheck
}

/// <summary>
/// Health status enumeration.
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// Health status is unknown.
    /// </summary>
    Unknown,

    /// <summary>
    /// Data source is healthy.
    /// </summary>
    Healthy,

    /// <summary>
    /// Data source is degraded but functional.
    /// </summary>
    Degraded,

    /// <summary>
    /// Data source is unhealthy.
    /// </summary>
    Unhealthy,

    /// <summary>
    /// Data source is unreachable.
    /// </summary>
    Unreachable
}

/// <summary>
/// Represents filter operators.
/// </summary>
public enum FilterOperator
{
    /// <summary>
    /// Equals comparison.
    /// </summary>
    Equals,

    /// <summary>
    /// Not equals comparison.
    /// </summary>
    NotEquals,

    /// <summary>
    /// Greater than comparison.
    /// </summary>
    GreaterThan,

    /// <summary>
    /// Greater than or equal comparison.
    /// </summary>
    GreaterThanOrEqual,

    /// <summary>
    /// Less than comparison.
    /// </summary>
    LessThan,

    /// <summary>
    /// Less than or equal comparison.
    /// </summary>
    LessThanOrEqual,

    /// <summary>
    /// Contains text.
    /// </summary>
    Contains,

    /// <summary>
    /// Starts with text.
    /// </summary>
    StartsWith,

    /// <summary>
    /// Ends with text.
    /// </summary>
    EndsWith,

    /// <summary>
    /// Regular expression match.
    /// </summary>
    Regex,

    /// <summary>
    /// In list of values.
    /// </summary>
    In,

    /// <summary>
    /// Not in list of values.
    /// </summary>
    NotIn
}

/// <summary>
/// Represents data formats.
/// </summary>
public enum DataFormat
{
    /// <summary>
    /// JSON format.
    /// </summary>
    Json,

    /// <summary>
    /// XML format.
    /// </summary>
    Xml,

    /// <summary>
    /// CSV format.
    /// </summary>
    Csv,

    /// <summary>
    /// Binary format.
    /// </summary>
    Binary,

    /// <summary>
    /// Protocol Buffers format.
    /// </summary>
    Protobuf,

    /// <summary>
    /// MessagePack format.
    /// </summary>
    MessagePack
}
