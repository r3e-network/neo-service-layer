namespace NeoServiceLayer.Services.Notification.Models;

/// <summary>
/// Configuration options for the Notification service
/// </summary>
public class NotificationOptions
{
    /// <summary>
    /// Gets or sets the default sender email address
    /// </summary>
    public string DefaultSenderEmail { get; set; } = "noreply@neo-service-layer.com";

    /// <summary>
    /// Gets or sets the default sender name
    /// </summary>
    public string DefaultSenderName { get; set; } = "Neo Service Layer";

    /// <summary>
    /// Gets or sets the maximum retry attempts for failed notifications
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the batch size for processing notifications
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether to enable email notifications
    /// </summary>
    public bool EnableEmail { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable SMS notifications
    /// </summary>
    public bool EnableSms { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to enable push notifications
    /// </summary>
    public bool EnablePush { get; set; } = false;

    /// <summary>
    /// Gets or sets the SMTP server settings
    /// </summary>
    public SmtpSettings? SmtpSettings { get; set; }

    /// <summary>
    /// Gets or sets the enabled notification channels
    /// </summary>
    public string[] EnabledChannels { get; set; } = { "Email", "Webhook" };

    /// <summary>
    /// Gets or sets the retry attempts for failed notifications
    /// </summary>
    public int RetryAttempts { get; set; } = 3;
}

/// <summary>
/// SMTP server settings
/// </summary>
public class SmtpSettings
{
    /// <summary>
    /// Gets or sets the SMTP server host
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the SMTP server port
    /// </summary>
    public int Port { get; set; } = 25;

    /// <summary>
    /// Gets or sets whether to use SSL
    /// </summary>
    public bool UseSsl { get; set; } = false;

    /// <summary>
    /// Gets or sets the username for authentication
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the password for authentication
    /// </summary>
    public string? Password { get; set; }
}

/// <summary>
/// Request to send batch notifications
/// </summary>
public class BatchNotificationRequest
{
    /// <summary>
    /// Gets or sets the list of notifications to send
    /// </summary>
    public List<SendNotificationRequest> Notifications { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to process in parallel
    /// </summary>
    public bool ProcessInParallel { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum degree of parallelism
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = 10;
}
