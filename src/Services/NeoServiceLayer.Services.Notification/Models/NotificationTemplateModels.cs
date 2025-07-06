namespace NeoServiceLayer.Services.Notification.Models;

/// <summary>
/// Notification template.
/// </summary>
public class NotificationTemplate
{
    /// <summary>
    /// Gets or sets the template ID.
    /// </summary>
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template name.
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template subject.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template body.
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template variables.
    /// </summary>
    public string[] Variables { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the supported channels.
    /// </summary>
    public NotificationChannel[] SupportedChannels { get; set; } = Array.Empty<NotificationChannel>();

    /// <summary>
    /// Gets or sets the template category.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the template is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last updated timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Create template request.
/// </summary>
public class CreateTemplateRequest
{
    /// <summary>
    /// Gets or sets the template name.
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template subject.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template body.
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template variables.
    /// </summary>
    public string[] Variables { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the supported channels.
    /// </summary>
    public NotificationChannel[] SupportedChannels { get; set; } = Array.Empty<NotificationChannel>();

    /// <summary>
    /// Gets or sets the template category.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Template result.
/// </summary>
public class TemplateResult
{
    /// <summary>
    /// Gets or sets the template ID.
    /// </summary>
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Send template notification request.
/// </summary>
public class SendTemplateNotificationRequest
{
    /// <summary>
    /// Gets or sets the template ID.
    /// </summary>
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification channel.
    /// </summary>
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// Gets or sets the recipient.
    /// </summary>
    public string Recipient { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template variables.
    /// </summary>
    public Dictionary<string, object> Variables { get; set; } = new();

    /// <summary>
    /// Gets or sets the notification priority.
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Notification history request.
/// </summary>
public class NotificationHistoryRequest
{
    /// <summary>
    /// Gets or sets the recipient filter.
    /// </summary>
    public string? Recipient { get; set; }

    /// <summary>
    /// Gets or sets the channel filter.
    /// </summary>
    public NotificationChannel? Channel { get; set; }

    /// <summary>
    /// Gets or sets the status filter.
    /// </summary>
    public DeliveryStatus? Status { get; set; }

    /// <summary>
    /// Gets or sets the start time filter.
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time filter.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of results.
    /// </summary>
    public int Limit { get; set; } = 100;

    /// <summary>
    /// Gets or sets the offset for pagination.
    /// </summary>
    public int Offset { get; set; } = 0;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Notification history result.
/// </summary>
public class NotificationHistoryResult
{
    /// <summary>
    /// Gets or sets the notification history entries.
    /// </summary>
    public NotificationHistoryEntry[] Entries { get; set; } = Array.Empty<NotificationHistoryEntry>();

    /// <summary>
    /// Gets or sets the total count of entries.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets whether there are more entries.
    /// </summary>
    public bool HasMore { get; set; }

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Update template request.
/// </summary>
public class UpdateTemplateRequest
{
    /// <summary>
    /// Gets or sets the template name.
    /// </summary>
    public string? TemplateName { get; set; }

    /// <summary>
    /// Gets or sets the template subject.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the template body.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Gets or sets the template variables.
    /// </summary>
    public string[]? Variables { get; set; }

    /// <summary>
    /// Gets or sets the supported channels.
    /// </summary>
    public NotificationChannel[]? SupportedChannels { get; set; }

    /// <summary>
    /// Gets or sets the template category.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets whether the template is active.
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Get history request.
/// </summary>
public class GetHistoryRequest : NotificationHistoryRequest
{
}

/// <summary>
/// Notification history.
/// </summary>
public class NotificationHistory : NotificationHistoryResult
{
}

/// <summary>
/// Broadcast request.
/// </summary>
public class BroadcastRequest
{
    /// <summary>
    /// Gets or sets the notification content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subject.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target channels.
    /// </summary>
    public NotificationChannel[] Channels { get; set; } = Array.Empty<NotificationChannel>();

    /// <summary>
    /// Gets or sets the target categories.
    /// </summary>
    public string[] Categories { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the priority.
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Broadcast result.
/// </summary>
public class BroadcastResult
{
    /// <summary>
    /// Gets or sets the broadcast ID.
    /// </summary>
    public string BroadcastId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the broadcast was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the number of notifications sent.
    /// </summary>
    public int NotificationsSent { get; set; }

    /// <summary>
    /// Gets or sets the error message if the broadcast failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the broadcast timestamp.
    /// </summary>
    public DateTime BroadcastAt { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Notification history entry.
/// </summary>
public class NotificationHistoryEntry
{
    /// <summary>
    /// Gets or sets the notification ID.
    /// </summary>
    public string NotificationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the channel used.
    /// </summary>
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// Gets or sets the recipient.
    /// </summary>
    public string Recipient { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subject.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the delivery status.
    /// </summary>
    public DeliveryStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the sent timestamp.
    /// </summary>
    public DateTime SentAt { get; set; }

    /// <summary>
    /// Gets or sets the delivered timestamp.
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>
    /// Gets or sets the priority.
    /// </summary>
    public NotificationPriority Priority { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
